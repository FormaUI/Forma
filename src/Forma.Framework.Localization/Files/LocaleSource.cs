namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;

/// <summary>Where translation files are read from, walked from the file named for the assembly down to the file named for the type, each file read once.</summary>
/// <param name="format">The format the files are written in.</param>
public abstract class LocaleSource(ILocaleFileFormat format)
{
    private readonly ConcurrentDictionary<string, LocaleFile?> _files = new(StringComparer.Ordinal);

    /// <summary>The format the files are written in.</summary>
    protected ILocaleFileFormat Format { get; } = format ?? throw new ArgumentNullException(nameof(format));

    /// <summary>Whether a file may complete a type's name from a root it declares rather than from the assembly's name.</summary>
    protected abstract bool ReadsDeclaredRoots { get; }

    /// <summary>The translations for a type in one culture, root by root and within a root from the file named for the assembly down to the file named for the type, so a later entry overrides an earlier one.</summary>
    /// <param name="assemblyName">The name of the assembly the type lives in.</param>
    /// <param name="typeName">The type's full name.</param>
    /// <param name="cultureName">The culture the files are named for.</param>
    public IReadOnlyList<LocaleSourceEntry> Read(string assemblyName, string typeName, string cultureName)
    {
        ArgumentException.ThrowIfNullOrEmpty(assemblyName);
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentException.ThrowIfNullOrEmpty(cultureName);

        var entries = new List<LocaleSourceEntry>();

        var segments = typeName.Split('.');
        var assemblySegments = assemblyName.Split('.');
        var leadsWithAssembly = segments.Length > assemblySegments.Length && segments.AsSpan(0, assemblySegments.Length).SequenceEqual(assemblySegments);

        // The assembly's name is the root a file need not declare, and the root of a type outside any namespace; a type rooted elsewhere is laid out by its full name.
        ReadRoot(assemblyName, assemblyName, GetLeadingSegments(assemblyName, leadsWithAssembly || segments.Length == 1), leadsWithAssembly ? segments[assemblySegments.Length..] : segments, cultureName, entries);

        if (ReadsDeclaredRoots)
        {
            for (var length = 1; length < segments.Length; length++)
            {
                var rootNamespace = string.Join('.', segments[..length]);
                if (rootNamespace != assemblyName)
                {
                    ReadRoot(assemblyName, rootNamespace, GetLeadingSegments(assemblyName, rootedAtAssembly: true), segments[length..], cultureName, entries);
                }
            }
        }

        return entries;
    }

    /// <summary>The segments every file name starts with, before the part of the type's name the file spells.</summary>
    /// <param name="assemblyName">The name of the assembly the type lives in.</param>
    /// <param name="rootedAtAssembly">Whether the type's name is completed from the assembly's, which a name starting with it or carrying no namespace is.</param>
    protected abstract IReadOnlyList<string> GetLeadingSegments(string assemblyName, bool rootedAtAssembly);

    /// <summary>The name a file has in this source, named in anything thrown.</summary>
    /// <param name="segments">The segments of the file's name, without the culture and extension.</param>
    /// <param name="cultureName">The culture the file is named for.</param>
    protected abstract string GetFileName(IReadOnlyList<string> segments, string cultureName);

    /// <summary>Reads the file the segments name, or null when this source holds none; opens it for the format to read unless a derived class already holds it read.</summary>
    /// <param name="segments">The segments of the file's name, without the culture and extension.</param>
    /// <param name="cultureName">The culture the file is named for.</param>
    protected virtual LocaleFile? ReadFile(IReadOnlyList<string> segments, string cultureName)
    {
        if (OpenFile(segments, cultureName) is not { } stream)
        {
            return null;
        }

        using (stream)
        {
            return Format.Read(stream, GetFileName(segments, cultureName), cultureName);
        }
    }

    /// <summary>Opens the file the segments name, or null when this source holds none; a source that reads its files itself opens none.</summary>
    /// <param name="segments">The segments of the file's name, without the culture and extension.</param>
    /// <param name="cultureName">The culture the file is named for.</param>
    protected virtual Stream? OpenFile(IReadOnlyList<string> segments, string cultureName)
        => null;

    private void ReadRoot(string assemblyName, string rootNamespace, IReadOnlyList<string> leadingSegments, string[] segments, string cultureName, List<LocaleSourceEntry> entries)
    {
        for (var depth = leadingSegments.Count == 0 ? 1 : 0; depth <= segments.Length; depth++)
        {
            var name = new List<string>(leadingSegments);
            name.AddRange(segments[..depth]);

            // A file named for a folder sits beside it or inside it under the folder's own name, the assembly's file inside the assembly's folder; the type's own file has no folder.
            ReadEntries(name, assemblyName, rootNamespace, segments.AsSpan(depth), cultureName, entries);
            if (depth < segments.Length)
            {
                name.Add(depth == 0 ? assemblyName : segments[depth - 1]);
                ReadEntries(name, assemblyName, rootNamespace, segments.AsSpan(depth), cultureName, entries);
            }
        }
    }

    private void ReadEntries(IReadOnlyList<string> segments, string assemblyName, string rootNamespace, ReadOnlySpan<string> remainingSegments, string cultureName, List<LocaleSourceEntry> entries)
    {
        var fileName = GetFileName(segments, cultureName);
        var file = _files.GetOrAdd(fileName, _ => ReadFile(segments, cultureName));
        if (file is not null && (!ReadsDeclaredRoots || (file.RootNamespace ?? assemblyName) == rootNamespace))
        {
            file.Collect(remainingSegments, entries, fileName);
        }
    }
}
