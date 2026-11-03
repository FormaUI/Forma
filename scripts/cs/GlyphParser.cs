// What a glyph drawing is and what makes one valid, apart from the pack that compiles them.
//
// BuildGlyphPack.cs includes this file so the rules live in one place, and GlyphParserTests.cs includes it so they
// can be exercised without a folder of drawings or a run of the pack.

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>One authored SVG file: its paths and, for a stroke file, its stroke width.</summary>
internal sealed record Drawing(double Stroke, IReadOnlyList<DrawingPath> Paths)
{
    /// <summary>Reads a drawing from disk, reporting what it cannot judge outright into <paramref name="problems" />.</summary>
    public static Drawing Read(string path, int grid, bool expectStroke, ICollection<string> problems)
        => Parse(File.ReadAllText(path), Path.GetFileName(path), grid, expectStroke, problems);

    /// <summary>
    /// Reads a drawing from its markup. A rule the drawing cannot be read past at all throws; a shape that
    /// reads but would not draw as written is added to <paramref name="problems" />, so a pack reports every
    /// drawing needing work together rather than one run at a time.
    /// </summary>
    public static Drawing Parse(string content, string file, int grid, bool expectStroke, ICollection<string> problems)
    {
        ArgumentNullException.ThrowIfNull(problems);

        if (content.Contains("<?xml", StringComparison.Ordinal) || content.Contains("<!--", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"XML prolog or comments are forbidden in {file}");
        }

        foreach (Match element in Regex.Matches(content, @"<(\w+)[\s>/]"))
        {
            if (element.Groups[1].Value is not ("svg" or "path"))
            {
                throw new InvalidOperationException($"Forbidden element <{element.Groups[1].Value}> in {file}; only <path> is permitted");
            }
        }

        if (Regex.IsMatch(content, @"\b(id|class|style|transform)\s*="))
        {
            throw new InvalidOperationException($"Forbidden attribute in {file}");
        }

        var root = Regex.Match(content, @"(?s)^<svg\s+([^>]+)>(.*)</svg>\s*$");
        if (!root.Success || !root.Groups[1].Value.Contains($"viewBox=\"0 0 {grid} {grid}\"", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Invalid SVG structure in {file}, or a viewBox other than 0 0 {grid} {grid}");
        }

        var attributes = root.Groups[1].Value;
        var strokeRoot = Regex.Match(attributes, @"fill=""none""\s+stroke=""currentColor""\s+stroke-width=""(2|3)""\s+stroke-linecap=""round""\s+stroke-linejoin=""round""");
        var stroke = strokeRoot.Success
            ? double.Parse(strokeRoot.Groups[1].Value, CultureInfo.InvariantCulture)
            : Regex.IsMatch(attributes, @"fill=""currentColor""\s+fill-rule=""evenodd""\s+stroke=""none""")
                ? 0
                : throw new InvalidOperationException($"Root attributes in {file} are neither the stroke form nor the filled form");

        if (expectStroke && stroke != 2)
        {
            throw new InvalidOperationException($"{file} must be the stroke form at stroke 2");
        }

        var paths = new List<DrawingPath>();
        foreach (Match element in Regex.Matches(root.Groups[2].Value, @"<path\s+([^>]+?)\s*/>"))
        {
            var attributeText = element.Groups[1].Value;
            var filled = attributeText.Contains("fill=\"currentColor\"", StringComparison.Ordinal);
            var solid = filled && attributeText.Contains("stroke=\"none\"", StringComparison.Ordinal);
            foreach (Match attribute in Regex.Matches(attributeText, @"([\w-]+)=""[^""]*"""))
            {
                var allowed = attribute.Groups[1].Value is "d" or "fill" or "stroke" or "opacity" || (solid && attribute.Groups[1].Value == "fill-rule");
                if (!allowed)
                {
                    throw new InvalidOperationException($"Forbidden attribute '{attribute.Groups[1].Value}' on a path in {file}");
                }
            }

            var data = Regex.Match(attributeText, @"d=""([^""]+)""").Groups[1].Value;
            if (data.Length == 0)
            {
                throw new InvalidOperationException($"A path in {file} has no data");
            }

            // A solid silhouette's edge is anti-aliased whatever the grid, so on the 32 grid it may sit on half units.
            var decimals = solid && grid == 32 ? @"\d+\.(?!5\b)\d+" : @"\d+\.\d+";
            if (Regex.IsMatch(data, decimals))
            {
                throw new InvalidOperationException($"Coordinates off the grid are forbidden in {file}: {data}");
            }

            CheckArcs(data, file, problems);
            CheckCircles(data, file, problems);

            var opacityText = Regex.Match(attributeText, @"opacity=""([^""]+)""").Groups[1].Value;
            var opacity = opacityText.Length == 0 ? 1 : double.Parse(opacityText, CultureInfo.InvariantCulture);
            paths.Add(new DrawingPath(data, filled && stroke > 0, opacity));
        }

        if (paths.Count == 0)
        {
            throw new InvalidOperationException($"No <path> elements in {file}");
        }

        return new Drawing(stroke, paths);
    }

    public GlyphStyle ToStyle()
        => new(Stroke, Paths);

    // A circle drawn as two half-turn arcs is round only when its two points sit a diameter apart;
    // any other spacing draws a lens.
    private static void CheckCircles(string data, string file, ICollection<string> problems)
    {
        foreach (Match circle in Regex.Matches(data, @"M (-?\d+) (-?\d+) A (\d+) \3 0 1 ([01]) (-?\d+) (-?\d+) A \3 \3 0 1 \4 \1 \2 Z"))
        {
            var radius = double.Parse(circle.Groups[3].Value, CultureInfo.InvariantCulture);
            var dx = double.Parse(circle.Groups[5].Value, CultureInfo.InvariantCulture) - double.Parse(circle.Groups[1].Value, CultureInfo.InvariantCulture);
            var dy = double.Parse(circle.Groups[6].Value, CultureInfo.InvariantCulture) - double.Parse(circle.Groups[2].Value, CultureInfo.InvariantCulture);
            if (Math.Abs(Math.Sqrt(dx * dx + dy * dy) - 2 * radius) > 1e-9)
            {
                problems.Add($"A circle of radius {radius} in {file} spans {Math.Sqrt(dx * dx + dy * dy):0.##} between its two points instead of {2 * radius}: {circle.Value}");
            }
        }
    }

    // An arc whose radius is under half its chord is silently enlarged by the renderer, so the drawn
    // curve is not the one written. A 45° rounded end on the integer grid overshoots by a few percent
    // and is fine; a real distortion is rejected. Absolute commands only, which is all a drawing uses.
    private static void CheckArcs(string data, string file, ICollection<string> problems)
    {
        var x = 0.0;
        var y = 0.0;
        var startX = 0.0;
        var startY = 0.0;

        foreach (Match segment in Regex.Matches(data, @"([MLHVACQZ])\s*((?:-?\d+(?:\.\d+)?\s*)*)"))
        {
            var command = segment.Groups[1].Value;
            var numbers = segment.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(text => double.Parse(text, CultureInfo.InvariantCulture)).ToArray();

            switch (command)
            {
                case "M": (x, y) = (numbers[0], numbers[1]); (startX, startY) = (x, y); for (var i = 2; i + 1 < numbers.Length; i += 2) { (x, y) = (numbers[i], numbers[i + 1]); } break;
                case "L": for (var i = 0; i + 1 < numbers.Length; i += 2) { (x, y) = (numbers[i], numbers[i + 1]); } break;
                case "H": x = numbers[^1]; break;
                case "V": y = numbers[^1]; break;
                case "C": for (var i = 0; i + 5 < numbers.Length; i += 6) { (x, y) = (numbers[i + 4], numbers[i + 5]); } break;
                case "Q": for (var i = 0; i + 3 < numbers.Length; i += 4) { (x, y) = (numbers[i + 2], numbers[i + 3]); } break;
                case "Z": (x, y) = (startX, startY); break;
                case "A":
                    for (var i = 0; i + 6 < numbers.Length; i += 7)
                    {
                        var (rx, ry, endX, endY) = (numbers[i], numbers[i + 1], numbers[i + 5], numbers[i + 6]);
                        var chord = Math.Sqrt((endX - x) * (endX - x) + (endY - y) * (endY - y));
                        if (rx == ry && chord > 2 * rx * 1.15)
                        {
                            problems.Add($"An arc of radius {rx} in {file} spans a chord of {chord:0.##}, so the renderer would enlarge it: {data}");
                        }

                        (x, y) = (endX, endY);
                    }

                    break;
            }
        }
    }
}

/// <summary>One path of a drawing: its data, whether it is a solid mark inside a stroke drawing, and its opacity.</summary>
internal sealed record DrawingPath(string Data, bool Filled, double Opacity);

/// <summary>One style of a glyph as the asset carries it: the width its paths are stroked at, and the paths.</summary>
/// <remarks>A stroke of zero is the filled form, where each path is a silhouette rather than a line.</remarks>
internal sealed record GlyphStyle(double Stroke, IReadOnlyList<DrawingPath> Paths);

/// <summary>Scales a drawing from one grid to another as a starting point for one the target grid lacks.</summary>
internal static class GlyphDraft
{
    /// <summary>
    /// Rewrites a drawing's markup for the target grid, scaling the viewBox and every coordinate and arc
    /// radius, and snapping each to an integer. Arc flags and the rotation carry over untouched, since
    /// neither is a length.
    /// </summary>
    public static string Scale(string content, int from, int to)
    {
        var scale = (double)to / from;

        var scaled = content.Replace($"viewBox=\"0 0 {from} {from}\"", $"viewBox=\"0 0 {to} {to}\"", StringComparison.Ordinal);

        return Regex.Replace(scaled, @"d=""([^""]+)""", match =>
        {
            var data = Regex.Replace(match.Groups[1].Value, @"([A-Za-z])\s*((?:-?\d+(?:\.\d+)?\s*)+)", segment =>
            {
                var command = segment.Groups[1].Value;
                var numbers = segment.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var parts = numbers.Select((text, index) =>
                {
                    var value = double.Parse(text, CultureInfo.InvariantCulture);
                    var isArcFlag = command is "A" or "a" && index % 7 is 3 or 4;
                    var isArcRotation = command is "A" or "a" && index % 7 is 2;
                    return isArcFlag || isArcRotation
                        ? text
                        : Math.Round(value * scale, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
                });
                return $"{command} {string.Join(' ', parts)} ";
            });
            return $"d=\"{data.Trim()}\"";
        });
    }
}
