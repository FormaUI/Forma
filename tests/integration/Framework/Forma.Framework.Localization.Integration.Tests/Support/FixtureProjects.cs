namespace Forma.Framework.Localization.Integration.Tests.Support;

using System.Diagnostics;
using System.Text.Json;

using Forma.Tests.Shared;

/// <summary>A folder of projects made for one case, outside the repository, each importing the localization package's targets, which MSBuild is asked what a build of them would embed and record.</summary>
public sealed class FixtureProjects : IDisposable
{
    private static readonly string Repository = RepositoryFolder.Find();
    private static readonly string Targets = Path.Combine(Repository, "src", "Framework", "Forma.Framework.Localization", "build", "Forma.Framework.Localization.targets");

    private readonly string _folder = Directory.CreateTempSubdirectory("forma-locale-targets-").FullName;

    /// <summary>Creates the folder, building with the SDK the repository names.</summary>
    public FixtureProjects()
    {
        File.Copy(Path.Combine(Repository, "global.json"), Path.Combine(_folder, "global.json"));
    }

    /// <summary>Writes the project <paramref name="name"/>, a library unless <paramref name="properties"/> makes it an application, with <paramref name="items"/> of its own.</summary>
    /// <param name="name">The project's name, which its folder and assembly take.</param>
    /// <param name="properties">The project's own properties, as MSBuild elements.</param>
    /// <param name="items">The project's own items, as MSBuild elements.</param>
    /// <param name="sdk">The SDK the project builds with.</param>
    public void WriteProject(string name, string properties = "", string items = "", string sdk = "Microsoft.NET.Sdk")
        => WriteFile(Path.Combine(name, $"{name}.csproj"), $"""
            <Project Sdk="{sdk}">
                <PropertyGroup>
                    <TargetFramework>net11.0</TargetFramework>
                    {properties}
                </PropertyGroup>
                <ItemGroup>
                    {items}
                </ItemGroup>
                <Import Project="{Targets}" />
            </Project>
            """);

    /// <summary>Writes a locale file of <paramref name="culture"/>, carrying the entry every locale file carries, at <paramref name="path"/>.</summary>
    /// <param name="path">The file's path in the folder, its project's folder first.</param>
    /// <param name="culture">The culture the file is for.</param>
    public void WriteLocaleFile(string path, string culture = "he")
        => WriteFile(path, $$"""{ "$localization": "SchemaVersion: 1.0.0; Culture: {{culture}};" }""");

    /// <summary>Writes <paramref name="content"/> at <paramref name="path"/>.</summary>
    /// <param name="path">The file's path in the folder, its project's folder first.</param>
    /// <param name="content">The file's content.</param>
    public void WriteFile(string path, string content)
    {
        var fullPath = Path.Combine(_folder, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    /// <summary>The resources a build of the project <paramref name="name"/> embeds, each by the path it embeds from, with <c>/</c> between folders, and the name it is embedded under.</summary>
    /// <param name="name">The project's name.</param>
    public async Task<IReadOnlyList<(string Path, string Name, string WithCulture)>> GetEmbeddedResourcesAsync(string name)
    {
        var items = await GetItemsAsync(name, "PrepareResourceNames", "EmbeddedResource").ConfigureAwait(false);

        return [.. items.Select(item => (
            item.GetProperty("Identity").GetString()!.Replace('\\', '/'),
            item.TryGetProperty("LogicalName", out var logicalName) && logicalName.GetString() is { Length: > 0 } logical ? logical : item.GetProperty("ManifestResourceName").GetString()!,
            item.TryGetProperty("WithCulture", out var withCulture) ? withCulture.GetString() ?? string.Empty : string.Empty))];
    }

    /// <summary>The content items a build of the project <paramref name="name"/> keeps once it has embedded its resources, each by its path, with <c>/</c> between folders.</summary>
    /// <param name="name">The project's name.</param>
    public async Task<IReadOnlyList<string>> GetContentAsync(string name)
    {
        var items = await GetItemsAsync(name, "PrepareResourceNames", "Content").ConfigureAwait(false);

        return [.. items.Select(item => item.GetProperty("Identity").GetString()!.Replace('\\', '/'))];
    }

    /// <summary>The first argument of each assembly attribute of <paramref name="attributeType"/> a build of the project <paramref name="name"/> writes.</summary>
    /// <param name="name">The project's name.</param>
    /// <param name="attributeType">The attribute's full type name.</param>
    public async Task<IReadOnlyList<string>> GetAssemblyAttributeArgumentsAsync(string name, string attributeType)
    {
        var items = await GetItemsAsync(name, "GetAssemblyAttributes", "AssemblyAttribute").ConfigureAwait(false);

        return [.. items
            .Where(item => item.GetProperty("Identity").GetString() == attributeType)
            .Select(item => item.GetProperty("_Parameter1").GetString()!)];
    }

    /// <inheritdoc />
    public void Dispose()
        => Directory.Delete(_folder, recursive: true);

    // Runs the target, restoring first, and reads the item type's items after it, which MSBuild prints as JSON alone.
    private async Task<IReadOnlyList<JsonElement>> GetItemsAsync(string name, string target, string itemType)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = _folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in (string[])["msbuild", Path.Combine(name, $"{name}.csproj"), "-restore", $"-t:{target}", $"-getItem:{itemType}", "-nologo", "-nodeReuse:false"])
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"MSBuild failed on {name}: {await output.ConfigureAwait(false)}{await error.ConfigureAwait(false)}");
        }

        using var document = JsonDocument.Parse(await output.ConfigureAwait(false));

        return document.RootElement.GetProperty("Items").TryGetProperty(itemType, out var items)
            ? [.. items.EnumerateArray().Select(item => item.Clone())]
            : [];
    }
}
