// The build's reading of locale files, compiled by MSBuild's inline task factory against .NET Standard wherever MSBuild runs, so
// that the targets beside it only say when each task runs and what it is given.
namespace Forma.Framework.Localization.Build;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

/// <summary>Reads the folder of locale files an assembly's <c>.locale.json</c> names, <c>.locales</c> where it names none.</summary>
public sealed class ReadLocaleConfiguration : Task
{
    private const string DefaultLocalesDir = ".locales";

    private static readonly Regex LocalesDirEntry = new("\"localesDir\"\\s*:\\s*\"(?<value>(?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.CultureInvariant);

    /// <summary>The assembly's config file, which needn't exist.</summary>
    [Required]
    public string ConfigFile { get; set; } = string.Empty;

    /// <summary>The folder of locale files, written with <c>/</c>.</summary>
    [Output]
    public string LocalesDir { get; set; } = DefaultLocalesDir;

    /// <inheritdoc />
    public override bool Execute()
    {
        if (!File.Exists(ConfigFile))
        {
            return true;
        }

        var match = LocalesDirEntry.Match(LocaleJson.WithoutComments(File.ReadAllText(ConfigFile)));

        if (!match.Success)
        {
            return true;
        }

        var localesDir = Regex.Unescape(match.Groups["value"].Value).Replace('\\', '/').Trim('/');

        if (localesDir.Length == 0)
        {
            Log.LogError($"{ConfigFile} names an empty localesDir; it names the folder of locale files, .locales unless it names another.");
            return false;
        }

        LocalesDir = localesDir;
        return true;
    }
}

/// <summary>Keeps the JSON files carrying the <c>$localization</c> entry, names each by its path, refuses two that take one name, and holds a pack's files to the cultures it declares.</summary>
public sealed class PrepareLocaleResources : Task
{
    private static readonly Regex Marker = new("\"\\$localization\"\\s*:", RegexOptions.CultureInvariant);

    // The task compiles against .NET Standard, which lacks the overload the runtime calls, so it is found where MSBuild runs on
    // .NET; MSBuild on .NET Framework knows the predefined cultures alone.
    private static readonly MethodInfo PredefinedCulture = typeof(CultureInfo).GetMethod("GetCultureInfo", [typeof(string), typeof(bool)]);

    /// <summary>The JSON files a locale file may be among, by their paths in the project.</summary>
    [Required]
    public ITaskItem[] Files { get; set; } = [];

    /// <summary>What every resource name starts with: the project's root namespace and a dot, or nothing for a pack.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>The folder of locale files, written with <c>/</c>.</summary>
    [Required]
    public string LocalesDir { get; set; } = string.Empty;

    /// <summary>The cultures a pack declares, separated by semicolons; none for a project that isn't a pack.</summary>
    public string Cultures { get; set; } = string.Empty;

    /// <summary>The locale files, each carrying the resource name it is embedded under as <c>LogicalName</c>.</summary>
    [Output]
    public ITaskItem[] LocaleFiles { get; set; } = [];

    /// <inheritdoc />
    public override bool Execute()
    {
        var localeFiles = new List<ITaskItem>();

        foreach (var file in Files)
        {
            if (!Marker.IsMatch(File.ReadAllText(file.GetMetadata("FullPath"))))
            {
                continue;
            }

            var localeFile = new TaskItem(file);
            localeFile.SetMetadata("LogicalName", Prefix + file.ItemSpec.Replace('\\', '.').Replace('/', '.'));
            localeFiles.Add(localeFile);
        }

        // A folder and a dot name a file alike, so two files can take one name, which one assembly can't embed twice.
        foreach (var sharing in localeFiles.GroupBy(file => file.GetMetadata("LogicalName"), StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            Log.LogError($"{string.Join(" and ", sharing.Select(file => file.ItemSpec))} are both embedded as {sharing.Key}: a locale file is named by its path with every / made a ., so a folder and a dot name it alike. Keep their entries in one of them.");
        }

        var cultures = Cultures.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(culture => culture.Trim()).ToArray();

        if (cultures.Length > 0)
        {
            var others = localeFiles.Where(file => !cultures.Contains(ReadCulture(file.ItemSpec), StringComparer.Ordinal)).Select(file => file.ItemSpec).ToArray();

            if (others.Length > 0)
            {
                Log.LogError($"This locale pack declares the culture '{Cultures}', and {string.Join(", ", others)} is for another or for none.");
            }
        }

        LocaleFiles = localeFiles.ToArray();
        return !Log.HasLoggedErrors;
    }

    // A file's culture, read from its path as the runtime's default reading does: a folder and a dot separate the name alike, the
    // folder of locale files is left out, a name with an empty part has none, and the first part that is a culture, written as
    // .NET names it, is the file's; empty where it has none.
    private string ReadCulture(string path)
    {
        var withoutExtension = path.Substring(0, path.Length - Path.GetExtension(path).Length);
        var parts = withoutExtension.Split('.', '/', '\\');
        var localesParts = LocalesDir.Split('.', '/');
        var culture = string.Empty;

        for (var part = 0; part < parts.Length; part++)
        {
            if (StartsWith(parts, part, localesParts))
            {
                part += localesParts.Length - 1;
            }
            else if (parts[part].Length == 0)
            {
                return string.Empty;
            }
            else if (culture.Length == 0 && IsCulture(parts[part]))
            {
                culture = parts[part];
            }
        }

        return culture;
    }

    private static bool StartsWith(string[] parts, int start, string[] prefix)
    {
        if (start + prefix.Length > parts.Length)
        {
            return false;
        }

        for (var index = 0; index < prefix.Length; index++)
        {
            if (!string.Equals(parts[start + index], prefix[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    // A culture's name starts with a language of two or three lowercase letters, which tells a part such as a type's name apart
    // without asking .NET, which answers a name it doesn't know by throwing.
    private static bool IsCulture(string part)
    {
        var hyphen = part.IndexOf('-');
        var language = hyphen >= 0 ? part.Substring(0, hyphen) : part;

        if (language.Length < 2 || language.Length > 3 || language.Any(letter => letter < 'a' || letter > 'z'))
        {
            return false;
        }

        try
        {
            var culture = PredefinedCulture is null ? CultureInfo.GetCultureInfo(part) : (CultureInfo)PredefinedCulture.Invoke(null, [part, true]);

            return string.Equals(culture.Name, part, StringComparison.Ordinal);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is CultureNotFoundException)
        {
            return false;
        }
    }
}

/// <summary>Reads JSON the way a locale file and its config are written, with comments allowed.</summary>
internal static class LocaleJson
{
    /// <summary>The text with its comments taken out, outside its strings.</summary>
    public static string WithoutComments(string text)
    {
        var result = new StringBuilder(text.Length);
        var index = 0;

        while (index < text.Length)
        {
            var character = text[index];

            if (character == '"')
            {
                var end = index + 1;

                while (end < text.Length && text[end] != '"')
                {
                    end += text[end] == '\\' ? 2 : 1;
                }

                end = Math.Min(end + 1, text.Length);
                result.Append(text, index, end - index);
                index = end;
            }
            else if (character == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                index = text.IndexOf('\n', index) is var lineEnd && lineEnd < 0 ? text.Length : lineEnd;
            }
            else if (character == '/' && index + 1 < text.Length && text[index + 1] == '*')
            {
                index = text.IndexOf("*/", index + 2, StringComparison.Ordinal) is var blockEnd && blockEnd < 0 ? text.Length : blockEnd + 2;
            }
            else
            {
                result.Append(character);
                index++;
            }
        }

        return result.ToString();
    }
}
