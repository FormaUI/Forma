namespace Forma.Blazor.Browser.Tests.Components;

using System.Text.RegularExpressions;

using Forma.Tests.Shared;

/// <summary>Every render mode held to every component: each component the catalog lists has a check page, which the static check renders statically, and each check page is opened by a check that runs in every interactive runtime.</summary>
public sealed partial class RenderModeInventoryTests
{
    [Fact]
    public void Should_give_every_component_the_catalog_lists_a_check_page()
    {
        // Arrange
        var root = RepositoryFolder.Find();
        var catalog = File.ReadAllText(Path.Combine(root, "docs", "component-catalog.md"));
        var placed = Directory.EnumerateFiles(Path.Combine(root, "tests", "e2e", "Components", "Forma.Blazor.Browser.Components.Checks"), "*.razor", SearchOption.AllDirectories)
            .Append(Path.Combine(root, "tests", "e2e", "Components", "Forma.Blazor.Browser.Components", "Components", "App.razor"))
            .SelectMany(file => PlacedComponent().Matches(File.ReadAllText(file)).Select(match => match.Groups["name"].Value))
            .ToHashSet(StringComparer.Ordinal);

        // Act
        var missing = CatalogComponent().Matches(catalog)
            .Select(match => match.Groups["name"].Value)
            .Where(name => !placed.Contains(name))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        // Assert
        Assert.True(missing.Count == 0, $"The catalog lists components no check page places:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    [Fact]
    public void Should_open_every_check_page_in_a_check_that_runs_in_every_interactive_runtime()
    {
        // Arrange — the text of each check that runs in every interactive runtime, from its runtimes to the next check
        var root = RepositoryFolder.Find();
        var opened = Directory.EnumerateFiles(Path.Combine(root, "tests", "e2e", "Forma.Blazor.Browser.Tests", "Components"), "*.cs")
            .SelectMany(file => RuntimeCheck().Split(File.ReadAllText(file)).Skip(1))
            .Select(check => NextCheck().Split(check)[0])
            .SelectMany(check => StringLiteral().Matches(check).Select(match => match.Groups["text"].Value))
            .ToHashSet(StringComparer.Ordinal);

        // Act
        var unopened = ComponentChecks.ReadPagePaths().Where(path => !opened.Contains(path)).ToList();

        // Assert
        Assert.True(unopened.Count == 0, $"No check that runs in every interactive runtime opens these check pages:{Environment.NewLine}{string.Join(Environment.NewLine, unopened)}");
    }

    [GeneratedRegex(@"[✅◐] (?<name>Forma\w+)")]
    private static partial Regex CatalogComponent();

    [GeneratedRegex(@"<(?<name>Forma\w+)[\s/>]")]
    private static partial Regex PlacedComponent();

    [GeneratedRegex(@"\[MemberData\(nameof\((ComponentChecks\.)?Runtimes\), MemberType = typeof\(ComponentChecks\)\)\]")]
    private static partial Regex RuntimeCheck();

    [GeneratedRegex(@"\[(Theory|Fact)\]")]
    private static partial Regex NextCheck();

    [GeneratedRegex("\"(?<text>[^\"]+)\"")]
    private static partial Regex StringLiteral();
}
