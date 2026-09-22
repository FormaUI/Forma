#!/usr/bin/env dotnet
#:package SkiaSharp@3.119.0
#:include GlyphParser.cs

// Builds Forma's glyph asset: validates the stroke drawings, derives each filled variant from its
// stroke drawing, and writes the compact JSON the library embeds.
//
//   usage: dotnet run scripts/cs/BuildGlyphPack.cs -- <enum.cs> <glyphs folder> <output.json> [--format]
//          dotnet run scripts/cs/BuildGlyphPack.cs -- draft <glyphs folder> <from grid> <to grid>
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
    Console.Error.WriteLine("usage: dotnet run scripts/cs/BuildGlyphPack.cs -- <enum.cs> <glyphs folder> <output.json> [--format]");
    Console.Error.WriteLine("       dotnet run scripts/cs/BuildGlyphPack.cs -- draft <glyphs folder> <from grid> <to grid>");
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

// Every drawing that reads but would not draw as written, gathered so one run names them all.
var problems = new List<string>();

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

        var stroke = Drawing.Read(strokePath, size, expectStroke: true, problems);

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

if (problems.Count > 0)
{
    throw new InvalidOperationException($"{problems.Count} drawing(s) need redrawing:\n{string.Join('\n', problems)}");
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
    var written = 0;

    foreach (var file in Directory.GetFiles(source, "*.svg"))
    {
        var destination = Path.Combine(target, Path.GetFileName(file)!);
        if (File.Exists(destination))
        {
            continue;
        }

        File.WriteAllText(destination, GlyphDraft.Scale(File.ReadAllText(file), from, to));
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
    using var paint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = StrokeWidth, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
    var bodyArea = ReferenceBodyArea * (grid / ReferenceGrid) * (grid / ReferenceGrid);

    // Every path Skia returns is a native handle, and a shape operation makes a new one rather than writing
    // into the old, so the one it replaces would otherwise be dropped. They are collected as they are made
    // and released together, which keeps each step from having to know whether what it replaced is still read.
    var owned = new List<SKPath>();

    try
    {
        var parts = stroke.Paths.Select(path =>
        {
            var skia = SKPath.ParseSvgPathData(path.Data) ?? throw new InvalidOperationException($"Unparseable path data: {path.Data}");
            owned.Add(skia);
            return new { path.Opacity, path.Filled, Skia = skia };
        }).ToArray();

        var candidates = parts
            .Where(part => !part.Filled && part.Opacity >= 1)
            .Select(part =>
            {
                var region = ClosedRegion(part.Skia);
                if (region is not null)
                {
                    owned.Add(region);
                }

                return (Part: part, Region: region);
            })
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
        owned.Add(body);
        foreach (var region in bodies)
        {
            body = Combine(body, region, SKPathOp.Union);
        }

        var result = new SKPath(body);
        owned.Add(result);
        var faded = new List<(SKPath Path, double Opacity)>();

        foreach (var part in parts)
        {
            var outline = part.Skia;
            if (!part.Filled)
            {
                outline = paint.GetFillPath(part.Skia) ?? new SKPath();
                owned.Add(outline);
            }

            var isBody = bodyParts.Contains(part);

            if (part.Opacity < 1)
            {
                faded.Add((outline, part.Opacity));
                continue;
            }

            if (isBody)
            {
                result = Combine(result, outline, SKPathOp.Union);
                continue;
            }

            var inside = outline.Op(body, SKPathOp.Intersect) ?? new SKPath();
            owned.Add(inside);
            var outside = outline.Op(body, SKPathOp.Difference) ?? new SKPath();
            owned.Add(outside);
            result = Combine(result, inside, SKPathOp.Difference);
            result = Combine(result, outside, SKPathOp.Union);
        }

        var entries = new List<Dictionary<string, object>> { new() { ["d"] = Data(result) } };
        entries.AddRange(faded.Select(item => new Dictionary<string, object> { ["d"] = Data(item.Path), ["o"] = item.Opacity }));

        return new Dictionary<string, object> { ["stroke"] = 0, ["paths"] = entries.ToArray() };

        // Keeps the path the operation could not produce, as the operation itself does, and takes ownership
        // of the one it did so that neither the old nor the new is released while the other is still read.
        SKPath Combine(SKPath path, SKPath other, SKPathOp operation)
        {
            var combined = path.Op(other, operation);
            if (combined is null)
            {
                return path;
            }

            owned.Add(combined);
            return combined;
        }
    }
    finally
    {
        foreach (var path in owned)
        {
            path.Dispose();
        }
    }
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
                // Flush has already copied whatever this contour contributed, so the handle can go.
                Flush();
                current.Dispose();
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
    current.Dispose();

    if (closed.IsEmpty)
    {
        closed.Dispose();
        return null;
    }

    return closed;

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
    using var simplified = path.Simplify();
    return Regex.Replace((simplified ?? path).ToSvgPathData(), @"-?\d+\.\d+", match => float.Parse(match.Value, CultureInfo.InvariantCulture).ToString("0.##", CultureInfo.InvariantCulture));
}

