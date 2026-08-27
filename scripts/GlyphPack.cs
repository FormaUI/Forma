#:package SkiaSharp@3.119.0

// Builds Forma's glyph asset: validates the stroke drawings, derives each filled variant from its
// stroke drawing, and writes the compact JSON the library embeds.
//
//   usage: dotnet run scripts/GlyphPack.cs -- <enum.cs> <glyphs folder> <output.json> [--format]

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using SkiaSharp;

const float StrokeWidth = 2;
const float HeavyStrokeWidth = 3;
const float BodyArea = 30;
const string ViewBox = "0 0 32 32";

if (args.Length < 3)
{
    Console.Error.WriteLine("usage: dotnet run scripts/GlyphPack.cs -- <enum.cs> <glyphs folder> <output.json> [--format]");
    return 2;
}

var (enumPath, glyphsFolder, outputPath) = (args[0], args[1], args[2]);
var format = args.Contains("--format");

var declared = Regex.Matches(File.ReadAllText(enumPath), @"(?m)^\s{4}(\w+)(?:\s*=\s*\d+)?,\s*$")
    .Select(match => match.Groups[1].Value)
    .Where(name => name != "None")
    .ToArray();

var files = Directory.GetFiles(glyphsFolder).Select(file => Path.GetFileName(file)!).ToHashSet(StringComparer.Ordinal);
var expected = declared.Select(name => $"{name}.svg").ToHashSet(StringComparer.Ordinal);
var overrides = declared.Select(name => $"{name}.alt.svg").ToHashSet(StringComparer.Ordinal);

var missing = expected.Where(file => !files.Contains(file)).ToArray();
if (missing.Length > 0)
{
    throw new InvalidOperationException($"Missing glyph files ({missing.Length}): {string.Join(", ", missing)}");
}

var extra = files.Where(file => !expected.Contains(file) && !overrides.Contains(file)).ToArray();
if (extra.Length > 0)
{
    throw new InvalidOperationException($"Unexpected files in the glyphs folder ({extra.Length}): {string.Join(", ", extra)}");
}

var glyphs = new SortedDictionary<string, object>(StringComparer.Ordinal);
var derived = 0;
var overridden = 0;

foreach (var name in declared)
{
    var stroke = Drawing.Read(Path.Combine(glyphsFolder, $"{name}.svg"), expectStroke: true);
    var overridePath = Path.Combine(glyphsFolder, $"{name}.alt.svg");

    object filled;
    if (File.Exists(overridePath))
    {
        filled = Drawing.Read(overridePath, expectStroke: false).ToStyle();
        overridden++;
    }
    else
    {
        filled = Derive(stroke);
        derived++;
    }

    glyphs[name] = new Dictionary<string, object> { ["regular"] = stroke.ToStyle(), ["filled"] = filled };
}

using (var output = File.Create(outputPath))
{
    using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = format });
    writer.WriteStartObject();
    writer.WriteString("viewBox", ViewBox);
    writer.WritePropertyName("glyphs");
    writer.WriteStartObject();
    foreach (var (name, styles) in glyphs)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        foreach (var (styleName, style) in (Dictionary<string, object>)styles)
        {
            writer.WritePropertyName(styleName);
            WriteStyle(writer, (Dictionary<string, object>)style);
        }

        writer.WriteEndObject();
    }

    writer.WriteEndObject();
    writer.WriteEndObject();
    writer.Flush();
    output.WriteByte((byte)'\n');
}
Console.WriteLine($"Validated {declared.Length} glyphs; derived {derived} filled variants and kept {overridden} drawn ones; wrote {outputPath}.");
return 0;

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
static object Derive(Drawing stroke)
{
    var paint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };

    var parts = stroke.Paths.Select(path => new
    {
        path.Opacity,
        path.Filled,
        Skia = SKPath.ParseSvgPathData(path.Data) ?? throw new InvalidOperationException($"Unparseable path data: {path.Data}"),
    }).ToArray();

    var candidates = parts
        .Where(part => !part.Filled && part.Opacity >= 1)
        .Select(part => (Part: part, Region: ClosedRegion(part.Skia)))
        .Where(item => item.Region is not null && Math.Abs(Area(item.Region)) >= BodyArea)
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
    public static Drawing Read(string path, bool expectStroke)
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
        if (!root.Success || !root.Groups[1].Value.Contains("viewBox=\"0 0 32 32\"", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Invalid SVG structure or viewBox in {file}");
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
            foreach (Match attribute in Regex.Matches(attributeText, @"([\w-]+)=""[^""]*"""))
            {
                if (attribute.Groups[1].Value is not ("d" or "fill" or "stroke" or "opacity"))
                {
                    throw new InvalidOperationException($"Forbidden attribute '{attribute.Groups[1].Value}' on a path in {file}");
                }
            }

            var data = Regex.Match(attributeText, @"d=""([^""]+)""").Groups[1].Value;
            if (data.Length == 0)
            {
                throw new InvalidOperationException($"A path in {file} has no data");
            }

            if (Regex.IsMatch(data, @"\d+\.\d+"))
            {
                throw new InvalidOperationException($"Decimal coordinates are forbidden in {file}: {data}");
            }

            var filled = attributeText.Contains("fill=\"currentColor\"", StringComparison.Ordinal);
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
