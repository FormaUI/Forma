namespace Forma.Blazor.Browser.Tests;

/// <summary>A published client's compressed download, grouped by what each file belongs to.</summary>
/// <remarks>
/// Only what the runtime fetches counts: the assemblies, the runtime's own files and the globalization data, each as
/// the Brotli file the server sends. Source maps and the Server's script stay on the disk. A file belongs to the runtime
/// where it is .NET's or ASP.NET Core's, to the globalization data where it is ICU's, to the app where the check's own
/// client ships it, to Forma where Forma ships it, and otherwise to the dependency its name begins with, so
/// AngleSharp.Css is AngleSharp's.
/// </remarks>
public static class DownloadGroups
{
    /// <summary>Measures the client whose _framework folder is given.</summary>
    /// <param name="framework">The published _framework folder.</param>
    /// <returns>Each group's compressed size in bytes.</returns>
    public static Dictionary<string, long> Measure(string framework)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(framework);

        return new DirectoryInfo(framework).GetFiles("*.br")
            .Where(file => IsFetched(file.Name))
            .GroupBy(file => GetGroup(file.Name))
            .ToDictionary(group => group.Key, group => group.Sum(file => file.Length));
    }

    private static bool IsFetched(string name)
        => name.EndsWith(".wasm.br", StringComparison.Ordinal)
            || name.EndsWith(".dat.br", StringComparison.Ordinal)
            || (name.StartsWith("dotnet.", StringComparison.Ordinal) && name.EndsWith(".js.br", StringComparison.Ordinal));

    private static string GetGroup(string name)
    {
        if (name.StartsWith("icudt", StringComparison.Ordinal))
        {
            return "globalization";
        }

        if (name.StartsWith("Forma.Blazor.Browser.", StringComparison.Ordinal))
        {
            return "app";
        }

        if (name.StartsWith("Forma.", StringComparison.Ordinal))
        {
            return "forma";
        }

        if (name.StartsWith("System.", StringComparison.Ordinal) || name.StartsWith("Microsoft.", StringComparison.Ordinal)
            || name.StartsWith("dotnet.", StringComparison.Ordinal) || name.StartsWith("netstandard.", StringComparison.Ordinal)
            || name.StartsWith("mscorlib.", StringComparison.Ordinal))
        {
            return "runtime";
        }

        return name[..name.IndexOf('.', StringComparison.Ordinal)];
    }
}
