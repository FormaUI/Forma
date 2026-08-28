#:package SkiaSharp@3.119.0

// Builds Forma's glyph asset: validates the stroke drawings, derives each filled variant from its
// stroke drawing, and writes the compact JSON the library embeds.
//
//   usage: dotnet run scripts/GlyphPack.cs -- <enum.cs> <glyphs folder> <output.json> [--format]
//          dotnet run scripts/GlyphPack.cs -- draft <glyphs folder> <from grid> <to grid>
//
// The glyphs folder holds one subfolder per grid, named by its size (32, 24), each with a stroke
// drawing per glyph on that grid. At least one grid must draw every glyph; the others may be partial.
//
// The draft mode writes, for every glyph the target grid lacks, a starting point scaled from the
// source grid with every coordinate snapped to the target grid. A draft is not a drawing: it is
// reviewed and corrected before it counts.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using SkiaSharp;

const float StrokeWidth = 2;
const float HeavyStrokeWidth = 3;
const float ReferenceGrid = 32;
const float ReferenceBodyArea = 30;

if (args.Length == 4 && args[0] == "draft")
{
    return Draft(args[1], int.Parse(args[2], CultureInfo.InvariantCulture), int.Parse(args[3], CultureInfo.InvariantCulture));
}

if (args.Length < 3)
{
    Console.Error.WriteLine("usage: dotnet run scripts/GlyphPack.cs -- <enum.cs> <glyphs folder> <output.json> [--format]");
    Console.Error.WriteLine("       dotnet run scripts/GlyphPack.cs -- draft <glyphs folder> <from grid> <to grid>");
    return 2;
}

var (enumPath, glyphsFolder, outputPath) = (args[0], args[1], args[2]);
var format = args.Contains("--format");

var declared = Regex.Matches(File.ReadAllText(enumPath), @"(?m)^\s{4}(\w+)(?:\s*=\s*\d+)?,\s*$")
    .Select(match => match.Groups[1].Value)
    .Where(name => name != "None")
    .ToArray();

var grids = Directory.GetDirectories(glyphsFolder)
    .Select(folder => (Folder: folder, Size: int.TryParse(Path.GetFileName(folder), NumberStyles.None, CultureInfo.InvariantCulture, out var size) ? size : 0))
    .Where(grid => grid.Size > 0)
    .OrderByDescending(grid => grid.Size)
    .ToArray();

if (grids.Length == 0)
{
    throw new InvalidOperationException($"No grid folders (such as 32 or 24) under {glyphsFolder}");
}

var stray = Directory.GetFiles(glyphsFolder).Select(file => Path.GetFileName(file)!).ToArray();
if (stray.Length > 0)
{
    throw new InvalidOperationException($"Files outside a grid folder ({stray.Length}): {string.Join(", ", stray)}");
}

var expected = declared.Select(name => $"{name}.svg").ToHashSet(StringComparer.Ordinal);

// glyph name -> grid size -> style name -> style
var glyphs = new SortedDictionary<string, SortedDictionary<int, Dictionary<string, object>>>(StringComparer.Ordinal);
var complete = false;
var report = new StringBuilder();

foreach (var (folder, size) in grids)
{
    var files = Directory.GetFiles(folder).Select(file => Path.GetFileName(file)!).ToHashSet(StringComparer.Ordinal);

    var extra = files.Where(file => !expected.Contains(file)).ToArray();
    if (extra.Length > 0)
    {
        throw new InvalidOperationException($"Unexpected files in the {size} grid folder ({extra.Length}): {string.Join(", ", extra)}");
    }

    var drawn = 0;

    foreach (var name in declared)
    {
        var strokePath = Path.Combine(folder, $"{name}.svg");
        if (!File.Exists(strokePath))
        {
            continue;
        }

        var stroke = Drawing.Read(strokePath, size, expectStroke: true);

        if (!glyphs.TryGetValue(name, out var byGrid))
        {
            glyphs[name] = byGrid = [];
        }

        byGrid[size] = new Dictionary<string, object> { ["regular"] = stroke.ToStyle(), ["filled"] = Derive(stroke, size) };
        drawn++;
    }

    complete |= drawn == declared.Length;
    report.Append(CultureInfo.InvariantCulture, $" {size}: {drawn}/{declared.Length} drawn;");
}

if (!complete)
{
    throw new InvalidOperationException("No grid draws every glyph; one grid must be complete");
}

if (Drawing.Problems.Count > 0)
{
    throw new InvalidOperationException($"{Drawing.Problems.Count} drawing(s) need redrawing:\n{string.Join('\n', Drawing.Problems)}");
}

using (var output = File.Create(outputPath))
{
    using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = format });
    writer.WriteStartObject();
    writer.WritePropertyName("glyphs");
    writer.WriteStartObject();
    foreach (var (name, byGrid) in glyphs)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        foreach (var (size, styles) in byGrid)
        {
            writer.WritePropertyName(size.ToString(CultureInfo.InvariantCulture));
            writer.WriteStartObject();
            foreach (var (styleName, style) in styles)
            {
                writer.WritePropertyName(styleName);
                WriteStyle(writer, (Dictionary<string, object>)style);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    writer.WriteEndObject();
    writer.WriteEndObject();
    writer.Flush();
    output.WriteByte((byte)'\n');
}
Console.WriteLine($"Validated {declared.Length} glyphs on {grids.Length} grid(s):{report} wrote {outputPath}.");
return 0;

// Scales every drawing the target grid lacks from the source grid,
// snapping each number in the path data to an integer, and writes it as a draft. Arc radii and
// coordinates scale alike; arc flags are left as they are.
static int Draft(string glyphsFolder, int from, int to)
{
    var source = Path.Combine(glyphsFolder, from.ToString(CultureInfo.InvariantCulture));
    var target = Path.Combine(glyphsFolder, to.ToString(CultureInfo.InvariantCulture));
    Directory.CreateDirectory(target);
    var scale = (double)to / from;
    var written = 0;

    foreach (var file in Directory.GetFiles(source, "*.svg"))
    {
        var destination = Path.Combine(target, Path.GetFileName(file)!);
        if (File.Exists(destination))
        {
            continue;
        }

        var content = File.ReadAllText(file)
            .Replace($"viewBox=\"0 0 {from} {from}\"", $"viewBox=\"0 0 {to} {to}\"", StringComparison.Ordinal);

        content = Regex.Replace(content, @"d=""([^""]+)""", match =>
        {
            var data = Regex.Replace(match.Groups[1].Value, @"([A-Za-z])\s*((?:-?\d+(?:\.\d+)?\s*)+)", segment =>
            {
                var command = segment.Groups[1].Value;
                var numbers = segment.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var scaled = numbers.Select((text, index) =>
                {
                    var value = double.Parse(text, CultureInfo.InvariantCulture);
                    var isArcFlag = command is "A" or "a" && index % 7 is 3 or 4;
                    var isArcRotation = command is "A" or "a" && index % 7 is 2;
                    return isArcFlag || isArcRotation
                        ? text
                        : Math.Round(value * scale, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
                });
                return $"{command} {string.Join(' ', scaled)} ";
            });
            return $"d=\"{data.Trim()}\"";
        });

        File.WriteAllText(destination, content);
        written++;
    }

    Console.WriteLine($"Drafted {written} glyph(s) on the {to} grid from the {from} grid into {target}.");
    return 0;
}

static void WriteStyle(Utf8JsonWriter writer, Dictionary<string, object> style)
{
    writer.WriteStartObject();
    writer.WriteNumber("stroke", Convert.ToDouble(style["stroke"], CultureInfo.InvariantCulture));
    writer.WritePropertyName("paths");
    writer.WriteStartArray();
    foreach (var entry in (Dictionary<string, object>[])style["paths"])
    {
        writer.WriteStartObject();
        writer.WriteString("d", (string)entry["d"]);
        if (entry.TryGetValue("o", out var opacity))
        {
            writer.WriteNumber("o", Convert.ToDouble(opacity, CultureInfo.InvariantCulture));
        }

        if (entry.ContainsKey("f"))
        {
            writer.WriteBoolean("f", true);
        }

        writer.WriteEndObject();
    }

    writer.WriteEndArray();
    writer.WriteEndObject();
}

// The filled variant is the stroke drawing's silhouette: every body (a closed contour of some
// size) is filled together with its own outline, every other mark keeps its stroked outline, and
// whatever of those marks lies inside a body is cut out of it. A drawing with no body has nothing
// to fill and draws its strokes heavier instead.
static object Derive(Drawing stroke, int grid)
{
    var paint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
    var bodyArea = ReferenceBodyArea * (grid / ReferenceGrid) * (grid / ReferenceGrid);

    var parts = stroke.Paths.Select(path => new
    {
        path.Opacity,
        path.Filled,
        Skia = SKPath.ParseSvgPathData(path.Data) ?? throw new InvalidOperationException($"Unparseable path data: {path.Data}"),
    }).ToArray();

    var candidates = parts
        .Where(part => !part.Filled && part.Opacity >= 1)
        .Select(part => (Part: part, Region: ClosedRegion(part.Skia)))
        .Where(item => item.Region is not null && Math.Abs(Area(item.Region)) >= bodyArea)
        .ToArray();

    // A closed shape lying inside another is a detail of that body, not a body of its own.
    var bodyParts = new HashSet<object>();
    var bodies = new List<SKPath>();
    foreach (var (part, region) in candidates)
    {
        var enclosed = candidates.Any(other => other.Part != part && other.Region!.Contains(region!.TightBounds.Left, region.TightBounds.Top) && other.Region.Contains(region.TightBounds.Right, region.TightBounds.Bottom) && other.Region.Contains(region.TightBounds.MidX, region.TightBounds.MidY));
        if (!enclosed)
        {
            bodies.Add(region!);
            bodyParts.Add(part);
        }
    }

    if (bodies.Count == 0)
    {
        return new Dictionary<string, object>
        {
            ["stroke"] = HeavyStrokeWidth,
            ["paths"] = stroke.Paths.Select(path => path.ToEntry()).ToArray(),
        };
    }

    var body = new SKPath();
    foreach (var region in bodies)
    {
        body = body.Op(region, SKPathOp.Union) ?? body;
    }

    var result = new SKPath(body);
    var faded = new List<(SKPath Path, double Opacity)>();

    foreach (var part in parts)
    {
        var outline = part.Filled ? part.Skia : paint.GetFillPath(part.Skia) ?? new SKPath();
        var isBody = bodyParts.Contains(part);

        if (part.Opacity < 1)
        {
            faded.Add((outline, part.Opacity));
            continue;
        }

        if (isBody)
        {
            result = result.Op(outline, SKPathOp.Union) ?? result;
            continue;
        }

        var inside = outline.Op(body, SKPathOp.Intersect) ?? new SKPath();
        var outside = outline.Op(body, SKPathOp.Difference) ?? new SKPath();
        result = result.Op(inside, SKPathOp.Difference) ?? result;
        result = result.Op(outside, SKPathOp.Union) ?? result;
    }

    var entries = new List<Dictionary<string, object>> { new() { ["d"] = Data(result) } };
    entries.AddRange(faded.Select(item => new Dictionary<string, object> { ["d"] = Data(item.Path), ["o"] = item.Opacity }));

    return new Dictionary<string, object> { ["stroke"] = 0, ["paths"] = entries.ToArray() };
}

// The region a path's closed contours enclose, or null when it has none.
static SKPath? ClosedRegion(SKPath path)
{
    var closed = new SKPath { FillType = SKPathFillType.Winding };
    using var iterator = path.CreateRawIterator();
    var points = new SKPoint[4];
    var current = new SKPath();
    var hasClose = false;

    SKPathVerb verb;
    while ((verb = iterator.Next(points)) != SKPathVerb.Done)
    {
        switch (verb)
        {
            case SKPathVerb.Move:
                Flush();
                current = new SKPath();
                current.MoveTo(points[0]);
                break;
            case SKPathVerb.Line: current.LineTo(points[1]); break;
            case SKPathVerb.Quad: current.QuadTo(points[1], points[2]); break;
            case SKPathVerb.Conic: current.ConicTo(points[1], points[2], iterator.ConicWeight()); break;
            case SKPathVerb.Cubic: current.CubicTo(points[1], points[2], points[3]); break;
            case SKPathVerb.Close: hasClose = true; current.Close(); break;
        }
    }

    Flush();
    return closed.IsEmpty ? null : closed;

    void Flush()
    {
        if (hasClose)
        {
            closed.AddPath(current);
        }

        hasClose = false;
    }
}

static float Area(SKPath path)
{
    var bounds = path.TightBounds;
    return bounds.Width * bounds.Height;
}

static string Data(SKPath path)
{
    var simplified = path.Simplify() ?? path;
    return Regex.Replace(simplified.ToSvgPathData(), @"-?\d+\.\d+", match => float.Parse(match.Value, CultureInfo.InvariantCulture).ToString("0.##", CultureInfo.InvariantCulture));
}

/// <summary>One authored SVG file: its paths and, for a stroke file, its stroke width.</summary>
internal sealed record Drawing(double Stroke, IReadOnlyList<DrawingPath> Paths)
{
    /// <summary>Every rule a drawing broke that is worth reporting together rather than one at a time.</summary>
    public static List<string> Problems { get; } = [];

    public static Drawing Read(string path, int grid, bool expectStroke)
    {
        var content = File.ReadAllText(path);
        var file = Path.GetFileName(path);

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
        double stroke;
        var strokeRoot = Regex.Match(attributes, @"fill=""none""\s+stroke=""currentColor""\s+stroke-width=""(2|3)""\s+stroke-linecap=""round""\s+stroke-linejoin=""round""");
        if (strokeRoot.Success)
        {
            stroke = double.Parse(strokeRoot.Groups[1].Value, CultureInfo.InvariantCulture);
        }
        else if (Regex.IsMatch(attributes, @"fill=""currentColor""\s+fill-rule=""evenodd""\s+stroke=""none"""))
        {
            stroke = 0;
        }
        else
        {
            throw new InvalidOperationException($"Root attributes in {file} are neither the stroke form nor the filled form");
        }

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

            CheckArcs(data, file);
            CheckCircles(data, file);

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

    public object ToStyle()
        => new Dictionary<string, object> { ["stroke"] = Stroke, ["paths"] = Paths.Select(path => path.ToEntry()).ToArray() };

    // A circle drawn as two half-turn arcs is round only when its two points sit a diameter apart;
    // any other spacing draws a lens.
    private static void CheckCircles(string data, string file)
    {
        foreach (Match circle in Regex.Matches(data, @"M (-?\d+) (-?\d+) A (\d+) \3 0 1 ([01]) (-?\d+) (-?\d+) A \3 \3 0 1 \4 \1 \2 Z"))
        {
            var radius = double.Parse(circle.Groups[3].Value, CultureInfo.InvariantCulture);
            var dx = double.Parse(circle.Groups[5].Value, CultureInfo.InvariantCulture) - double.Parse(circle.Groups[1].Value, CultureInfo.InvariantCulture);
            var dy = double.Parse(circle.Groups[6].Value, CultureInfo.InvariantCulture) - double.Parse(circle.Groups[2].Value, CultureInfo.InvariantCulture);
            if (Math.Abs(Math.Sqrt(dx * dx + dy * dy) - 2 * radius) > 1e-9)
            {
                Problems.Add($"A circle of radius {radius} in {file} spans {Math.Sqrt(dx * dx + dy * dy):0.##} between its two points instead of {2 * radius}: {circle.Value}");
            }
        }
    }

    // An arc whose radius is under half its chord is silently enlarged by the renderer, so the drawn
    // curve is not the one written. A 45° rounded end on the integer grid overshoots by a few percent
    // and is fine; a real distortion is rejected. Absolute commands only, which is all a drawing uses.
    private static void CheckArcs(string data, string file)
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
                            Problems.Add($"An arc of radius {rx} in {file} spans a chord of {chord:0.##}, so the renderer would enlarge it: {data}");
                        }

                        (x, y) = (endX, endY);
                    }

                    break;
            }
        }
    }
}

/// <summary>One path of a drawing: its data, whether it is a solid mark inside a stroke drawing, and its opacity.</summary>
internal sealed record DrawingPath(string Data, bool Filled, double Opacity)
{
    public Dictionary<string, object> ToEntry()
    {
        var entry = new Dictionary<string, object> { ["d"] = Data };
        if (Opacity < 1)
        {
            entry["o"] = Opacity;
        }

        if (Filled)
        {
            entry["f"] = true;
        }

        return entry;
    }
}
