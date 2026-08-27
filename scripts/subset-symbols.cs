// Subsets one JuliaMono weight to Forma's mapped characters and renames it off the
// reserved font name, as the OFL requires of a modified version.
//
//   usage: dotnet run subset-symbols.cs -- <source.ttf> <characters.txt> <subfamily> <output.woff>

using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

const string Family = "Forma Unicode Symbols";
const int TextPresentation = 0xFE0E;

if (args.Length != 4)
{
    Console.Error.WriteLine("usage: dotnet run subset-symbols.cs -- <source.ttf> <characters.txt> <subfamily> <output.woff>");
    return 2;
}

var (sourcePath, charactersPath, subfamily, outputPath) = (args[0], args[1], args[2], args[3]);

var required = File.ReadAllText(charactersPath)
    .EnumerateRunes()
    .Select(rune => rune.Value)
    .Where(value => !Rune.IsWhiteSpace(new Rune(value)) && value != TextPresentation)
    .Distinct()
    .Order()
    .ToArray();

var font = new SfntReader(File.ReadAllBytes(sourcePath));

// --- resolve the characters to glyphs, then close over composite components -------------------
var cmap = font.ReadCmap();
var missing = required.Where(value => !cmap.ContainsKey(value)).ToArray();

if (missing.Length > 0)
{
    Console.Error.WriteLine("missing from source font: " + string.Join(' ', missing.Select(value => $"U+{value:X4}")));
    return 1;
}

var glyf = font.Table("glyf");
var loca = font.ReadLoca();
var closure = new SortedSet<int> { 0 };

foreach (var value in required)
{
    Close(cmap[value]);
}

void Close(int glyph)
{
    if (!closure.Add(glyph))
    {
        return;
    }

    foreach (var component in SfntReader.Components(GlyphData(glyph)))
    {
        Close(component);
    }
}

ReadOnlySpan<byte> GlyphData(int glyph)
    => glyf.AsSpan(loca[glyph], loca[glyph + 1] - loca[glyph]);

// --- rebuild the glyph-dependent tables over the new, dense glyph order -----------------------
var kept = closure.ToArray();
var toNew = kept.Select((old, index) => (old, index)).ToDictionary(pair => pair.old, pair => pair.index);

var newGlyf = new MemoryStream();
var newLoca = new uint[kept.Length + 1];

for (var index = 0; index < kept.Length; index++)
{
    newLoca[index] = (uint)newGlyf.Length;
    var data = GlyphData(kept[index]).ToArray();
    SfntReader.RemapComponents(data, toNew);
    newGlyf.Write(data);

    if (newGlyf.Length % 4 != 0)
    {
        newGlyf.Write(new byte[4 - newGlyf.Length % 4]);
    }
}

newLoca[kept.Length] = (uint)newGlyf.Length;

var writer = new SfntWriter();
writer.Add("glyf", newGlyf.ToArray());
writer.Add("loca", SfntWriter.LongLoca(newLoca));
writer.Add("cmap", SfntWriter.CmapFormat4(required.ToDictionary(value => value, value => toNew[cmap[value]])));
writer.Add("hmtx", font.SubsetHmtx(kept));
writer.Add("hhea", font.PatchedHhea(kept.Length));
writer.Add("maxp", font.PatchedMaxp(kept.Length));
writer.Add("head", font.PatchedHead());
writer.Add("OS/2", font.Table("OS/2"));
writer.Add("post", font.PostFormat3());
writer.Add("name", SfntWriter.NameTable(
    (0, font.NameRecord(0) ?? ""),
    (1, Family),
    (2, subfamily),
    (3, $"Forma;{Family.Replace(" ", "", StringComparison.Ordinal)}-{subfamily}"),
    (4, $"{Family} {subfamily}"),
    (6, $"{Family.Replace(" ", "", StringComparison.Ordinal)}-{subfamily}"),
    (13, font.NameRecord(13) ?? ""),
    (14, font.NameRecord(14) ?? "")));

var sfnt = writer.Build();
File.WriteAllBytes(outputPath, SfntWriter.Woff(sfnt));

// --- prove the output answers for every required character ------------------------------------
var check = new SfntReader(sfnt).ReadCmap();
var absent = required.Where(value => !check.ContainsKey(value)).ToArray();

if (absent.Length > 0)
{
    Console.Error.WriteLine("missing from subset: " + string.Join(' ', absent.Select(value => $"U+{value:X4}")));
    return 1;
}

Console.WriteLine($"{outputPath}: {required.Length} characters, {kept.Length} glyphs, {new FileInfo(outputPath).Length / 1024.0:F1} KB");
return 0;

/// <summary>Reads the tables of one TrueType font.</summary>
internal sealed class SfntReader(byte[] data)
{
    private readonly Dictionary<string, (int Offset, int Length)> _directory = ReadDirectory(data);

    public byte[] Table(string tag)
    {
        var (offset, length) = _directory[tag];
        return data.AsSpan(offset, length).ToArray();
    }

    public Dictionary<int, int> ReadCmap()
    {
        var table = Table("cmap");
        var best = -1;

        for (int index = 0, count = ReadU16(table, 2); index < count; index++)
        {
            var platform = ReadU16(table, 4 + (index * 8));
            var encoding = ReadU16(table, 6 + (index * 8));
            var offset = (int)ReadU32(table, 8 + (index * 8));

            // A Windows BMP or full-repertoire subtable answers for everything the map needs.
            if (platform == 3 && encoding is 1 or 10)
            {
                best = offset;
            }
        }

        if (best < 0)
        {
            throw new InvalidDataException("no Windows cmap subtable");
        }

        var result = new Dictionary<int, int>();
        var format = ReadU16(table, best);

        if (format == 4)
        {
            var segments = ReadU16(table, best + 6) / 2;

            for (var segment = 0; segment < segments; segment++)
            {
                int end = ReadU16(table, best + 14 + (segment * 2));
                int start = ReadU16(table, best + 16 + (segments * 2) + (segment * 2));
                int delta = ReadU16(table, best + 16 + (segments * 4) + (segment * 2));
                var rangeAt = best + 16 + (segments * 6) + (segment * 2);
                int range = ReadU16(table, rangeAt);

                for (var code = start; code <= end && code != 0xFFFF; code++)
                {
                    var glyph = range == 0
                        ? (code + delta) & 0xFFFF
                        : ReadU16(table, rangeAt + range + ((code - start) * 2)) is var indirect && indirect != 0 ? (indirect + delta) & 0xFFFF : 0;

                    if (glyph != 0)
                    {
                        result[code] = glyph;
                    }
                }
            }
        }
        else if (format == 12)
        {
            for (int group = 0, groups = (int)ReadU32(table, best + 12); group < groups; group++)
            {
                var start = ReadU32(table, best + 16 + (group * 12));
                var end = ReadU32(table, best + 20 + (group * 12));
                var glyph = ReadU32(table, best + 24 + (group * 12));

                for (var code = start; code <= end; code++)
                {
                    result[(int)code] = (int)(glyph + code - start);
                }
            }
        }
        else
        {
            throw new InvalidDataException($"unsupported cmap format {format}");
        }

        return result;
    }

    public int[] ReadLoca()
    {
        var head = Table("head");
        var maxp = Table("maxp");
        var glyphs = ReadU16(maxp, 4);
        var longFormat = ReadU16(head, 50) == 1;
        var loca = Table("loca");
        var offsets = new int[glyphs + 1];

        for (var index = 0; index <= glyphs; index++)
        {
            offsets[index] = longFormat ? (int)ReadU32(loca, index * 4) : ReadU16(loca, index * 2) * 2;
        }

        return offsets;
    }

    public byte[] SubsetHmtx(int[] kept)
    {
        var hmtx = Table("hmtx");
        var metrics = ReadU16(Table("hhea"), 34);
        var result = new byte[kept.Length * 4];

        for (var index = 0; index < kept.Length; index++)
        {
            var glyph = kept[index];
            var advance = glyph < metrics ? ReadU16(hmtx, glyph * 4) : ReadU16(hmtx, (metrics - 1) * 4);
            var bearing = glyph < metrics
                ? ReadU16(hmtx, (glyph * 4) + 2)
                : ReadU16(hmtx, (metrics * 4) + ((glyph - metrics) * 2));

            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(index * 4), advance);
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan((index * 4) + 2), bearing);
        }

        return result;
    }

    public byte[] PatchedHhea(int glyphs)
    {
        var table = Table("hhea");
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(34), (ushort)glyphs);
        return table;
    }

    public byte[] PatchedMaxp(int glyphs)
    {
        var table = Table("maxp");
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(4), (ushort)glyphs);
        return table;
    }

    public byte[] PatchedHead()
    {
        var table = Table("head");
        BinaryPrimitives.WriteUInt32BigEndian(table.AsSpan(8), 0);   // checkSumAdjustment, recomputed on write
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(50), 1);  // long loca offsets
        return table;
    }

    public byte[] PostFormat3()
    {
        // The header carries the metrics a renderer reads; format 3 declares no glyph names follow.
        var table = Table("post").AsSpan(0, 32).ToArray();
        BinaryPrimitives.WriteUInt32BigEndian(table, 0x00030000);
        return table;
    }

    public string? NameRecord(ushort id)
    {
        var table = Table("name");
        var count = ReadU16(table, 2);
        var strings = ReadU16(table, 4);

        for (var index = 0; index < count; index++)
        {
            var at = 6 + (index * 12);

            if (ReadU16(table, at) == 3 && ReadU16(table, at + 2) == 1 && ReadU16(table, at + 6) == id)
            {
                var length = ReadU16(table, at + 8);
                var offset = strings + ReadU16(table, at + 10);
                return Encoding.BigEndianUnicode.GetString(table, offset, length);
            }
        }

        return null;
    }

    public static IEnumerable<int> Components(ReadOnlySpan<byte> glyph)
    {
        if (glyph.Length == 0 || BinaryPrimitives.ReadInt16BigEndian(glyph) >= 0)
        {
            return [];
        }

        var components = new List<int>();
        var at = 10;

        while (true)
        {
            var flags = BinaryPrimitives.ReadUInt16BigEndian(glyph[at..]);
            components.Add(BinaryPrimitives.ReadUInt16BigEndian(glyph[(at + 2)..]));
            at += 4 + ArgumentBytes(flags);

            if ((flags & 0x0020) == 0)
            {
                return components;
            }
        }
    }

    public static void RemapComponents(byte[] glyph, Dictionary<int, int> toNew)
    {
        if (glyph.Length == 0 || BinaryPrimitives.ReadInt16BigEndian(glyph) >= 0)
        {
            return;
        }

        var at = 10;

        while (true)
        {
            var flags = BinaryPrimitives.ReadUInt16BigEndian(glyph.AsSpan(at));
            var component = BinaryPrimitives.ReadUInt16BigEndian(glyph.AsSpan(at + 2));
            BinaryPrimitives.WriteUInt16BigEndian(glyph.AsSpan(at + 2), (ushort)toNew[component]);
            at += 4 + ArgumentBytes(flags);

            if ((flags & 0x0020) == 0)
            {
                return;
            }
        }
    }

    private static int ArgumentBytes(ushort flags)
        => ((flags & 0x0001) != 0 ? 4 : 2)
            + ((flags & 0x0008) != 0 ? 2 : 0)
            + ((flags & 0x0040) != 0 ? 4 : 0)
            + ((flags & 0x0080) != 0 ? 8 : 0);

    private static Dictionary<string, (int, int)> ReadDirectory(byte[] data)
    {
        var directory = new Dictionary<string, (int, int)>();

        for (int index = 0, count = ReadU16(data, 4); index < count; index++)
        {
            var at = 12 + (index * 16);
            var tag = Encoding.ASCII.GetString(data, at, 4);
            directory[tag] = ((int)ReadU32(data, at + 8), (int)ReadU32(data, at + 12));
        }

        return directory;
    }

    private static ushort ReadU16(byte[] data, int offset)
        => BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset));

    private static uint ReadU32(byte[] data, int offset)
        => BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
}

/// <summary>Assembles TrueType tables into a font file, with the checksums the format requires.</summary>
internal sealed class SfntWriter
{
    private readonly SortedDictionary<string, byte[]> _tables = [];

    public void Add(string tag, byte[] table)
        => _tables[tag] = table;

    public static byte[] LongLoca(uint[] offsets)
    {
        var result = new byte[offsets.Length * 4];

        for (var index = 0; index < offsets.Length; index++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(index * 4), offsets[index]);
        }

        return result;
    }

    public static byte[] CmapFormat4(Dictionary<int, int> map)
    {
        var codes = map.Keys.Order().ToArray();
        var segments = new List<(int Start, int End)>();

        foreach (var code in codes)
        {
            if (segments.Count > 0 && segments[^1].End == code - 1)
            {
                segments[^1] = (segments[^1].Start, code);
            }
            else
            {
                segments.Add((code, code));
            }
        }

        segments.Add((0xFFFF, 0xFFFF));

        var count = segments.Count;
        var glyphIds = new List<ushort>();
        var subtable = new MemoryStream();
        using var writer = new BinaryWriter(subtable);

        void U16(int value)
        {
            writer.Write(BinaryPrimitives.ReverseEndianness((ushort)value));
        }

        var search = 1;
        while (search * 2 <= count)
        {
            search *= 2;
        }

        U16(4);
        U16(16 + (count * 8) + (map.Count * 2)); // length, glyph ids follow the four segment arrays
        U16(0);
        U16(count * 2);
        U16(search * 2);
        U16(int.Log2(search));
        U16((count - search) * 2);

        foreach (var (_, end) in segments)
        {
            U16(end);
        }

        U16(0);

        foreach (var (start, _) in segments)
        {
            U16(start);
        }

        for (var index = 0; index < count; index++)
        {
            U16(index == count - 1 ? 1 : 0); // the final segment maps 0xFFFF to glyph 0 through its delta
        }

        for (var index = 0; index < count; index++)
        {
            if (index == count - 1)
            {
                U16(0);
                continue;
            }

            U16((glyphIds.Count + count - index) * 2);

            for (var code = segments[index].Start; code <= segments[index].End; code++)
            {
                glyphIds.Add((ushort)map[code]);
            }
        }

        foreach (var glyph in glyphIds)
        {
            U16(glyph);
        }

        var body = subtable.ToArray();
        var table = new byte[12 + body.Length];
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(2), 1);  // one subtable
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(4), 3);  // Windows
        BinaryPrimitives.WriteUInt16BigEndian(table.AsSpan(6), 1);  // BMP
        BinaryPrimitives.WriteUInt32BigEndian(table.AsSpan(8), 12);
        body.CopyTo(table, 12);
        return table;
    }

    public static byte[] NameTable(params (ushort Id, string Text)[] records)
    {
        var kept = records.Where(record => record.Text.Length > 0).OrderBy(record => record.Id).ToArray();
        var strings = new MemoryStream();
        var table = new MemoryStream();
        using var writer = new BinaryWriter(table);

        void U16(int value)
        {
            writer.Write(BinaryPrimitives.ReverseEndianness((ushort)value));
        }

        U16(0);
        U16(kept.Length);
        U16(6 + (kept.Length * 12));

        foreach (var (id, text) in kept)
        {
            var encoded = Encoding.BigEndianUnicode.GetBytes(text);
            U16(3);
            U16(1);
            U16(0x0409);
            U16(id);
            U16(encoded.Length);
            U16((int)strings.Length);
            strings.Write(encoded);
        }

        strings.WriteTo(table);
        return table.ToArray();
    }

    /// <summary>Wraps a built font in the WOFF container, compressing each table with zlib.</summary>
    public static byte[] Woff(byte[] sfnt)
    {
        var count = BinaryPrimitives.ReadUInt16BigEndian(sfnt.AsSpan(4));
        var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        void U32(uint value)
        {
            writer.Write(BinaryPrimitives.ReverseEndianness(value));
        }

        var tables = new List<(byte[] Entry, byte[] Data)>();
        var offset = 44 + (count * 20);

        for (var index = 0; index < count; index++)
        {
            var at = 12 + (index * 16);
            var origin = (int)BinaryPrimitives.ReadUInt32BigEndian(sfnt.AsSpan(at + 8));
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(sfnt.AsSpan(at + 12));

            using var compressed = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                zlib.Write(sfnt, origin, length);
            }

            // The spec spells an incompressible table as one stored raw, marked by equal lengths.
            var data = compressed.Length < length ? compressed.ToArray() : sfnt.AsSpan(origin, length).ToArray();

            var entry = new byte[20];
            sfnt.AsSpan(at, 4).CopyTo(entry);
            BinaryPrimitives.WriteUInt32BigEndian(entry.AsSpan(4), (uint)offset);
            BinaryPrimitives.WriteUInt32BigEndian(entry.AsSpan(8), (uint)data.Length);
            BinaryPrimitives.WriteUInt32BigEndian(entry.AsSpan(12), (uint)length);
            sfnt.AsSpan(at + 4, 4).CopyTo(entry.AsSpan(16));

            tables.Add((entry, data));
            offset += (data.Length + 3) & ~3;
        }

        U32(0x774F4646); // 'wOFF'
        U32(0x00010000);
        U32((uint)offset);
        writer.Write(BinaryPrimitives.ReverseEndianness(count));
        writer.Write((ushort)0);
        U32((uint)((sfnt.Length + 3) & ~3)); // totalSfntSize, table data already padded
        U32(0); // major and minor version
        U32(0); // metaOffset
        U32(0); // metaLength
        U32(0); // metaOrigLength
        U32(0); // privOffset
        U32(0); // privLength

        foreach (var (entry, _) in tables)
        {
            writer.Write(entry);
        }

        foreach (var (_, data) in tables)
        {
            writer.Write(data);
            writer.Write(new byte[((data.Length + 3) & ~3) - data.Length]);
        }

        return output.ToArray();
    }

    public byte[] Build()
    {
        var count = _tables.Count;
        var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        var search = 1;
        while (search * 2 <= count)
        {
            search *= 2;
        }

        writer.Write(BinaryPrimitives.ReverseEndianness(0x00010000u));
        writer.Write(BinaryPrimitives.ReverseEndianness((ushort)count));
        writer.Write(BinaryPrimitives.ReverseEndianness((ushort)(search * 16)));
        writer.Write(BinaryPrimitives.ReverseEndianness((ushort)int.Log2(search)));
        writer.Write(BinaryPrimitives.ReverseEndianness((ushort)((count - search) * 16)));

        var offset = 12 + (count * 16);

        foreach (var (tag, table) in _tables)
        {
            writer.Write(Encoding.ASCII.GetBytes(tag));
            writer.Write(BinaryPrimitives.ReverseEndianness(Checksum(table)));
            writer.Write(BinaryPrimitives.ReverseEndianness((uint)offset));
            writer.Write(BinaryPrimitives.ReverseEndianness((uint)table.Length));
            offset += (table.Length + 3) & ~3;
        }

        foreach (var table in _tables.Values)
        {
            writer.Write(table);
            writer.Write(new byte[((table.Length + 3) & ~3) - table.Length]);
        }

        var built = output.ToArray();
        var headAt = 12 + (count * 16);

        foreach (var (tag, table) in _tables)
        {
            if (tag == "head")
            {
                BinaryPrimitives.WriteUInt32BigEndian(built.AsSpan(headAt + 8), 0xB1B0AFBAu - Checksum(built));
                break;
            }

            headAt += (table.Length + 3) & ~3;
        }

        return built;
    }

    private static uint Checksum(byte[] data)
    {
        var sum = 0u;

        for (var at = 0; at < data.Length; at += 4)
        {
            var word = 0u;

            for (var part = 0; part < 4; part++)
            {
                word = (word << 8) | (at + part < data.Length ? data[at + part] : 0u);
            }

            sum += word;
        }

        return sum;
    }
}
