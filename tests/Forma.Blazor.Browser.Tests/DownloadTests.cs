namespace Forma.Blazor.Browser.Tests;

using System.Text.Json;

/// <summary>What a WebAssembly client downloads, and what the publish trims.</summary>
public sealed class DownloadTests(BrowserHost host)
{
    /// <summary>The clients measured: the minimal one the host serves, and the one with every package a client can load.</summary>
    public static TheoryData<string> Clients { get; } = ["minimal", "full"];

    [Theory]
    [MemberData(nameof(Clients))]
    public void Should_keep_each_dependency_group_within_its_budget(string client)
    {
        // Arrange
        var budgets = ReadBudgets()[client];

        // Act
        var groups = DownloadGroups.Measure(GetFramework(client));

        // Assert
        var report = string.Join(Environment.NewLine, groups.OrderByDescending(group => group.Value).Select(group => $"{group.Key,-20} {group.Value,12:N0} bytes, budget {(budgets.TryGetValue(group.Key, out var budget) ? budget.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : "none")}"));
        TestContext.Current.TestOutputHelper?.WriteLine($"{client} client, compressed:{Environment.NewLine}{report}");
        Assert.Multiple(
            () => Assert.Empty(groups.Keys.Except(budgets.Keys)),
            () => Assert.Empty(groups.Where(group => group.Value > budgets[group.Key]).Select(group => $"{group.Key}: {group.Value:N0} over {budgets[group.Key]:N0}")));
    }

    [Fact]
    public void Should_trim_the_framework_and_forma_s_libraries()
    {
        // Arrange — the linker reads the build output and writes what it keeps beside the client's intermediate files
        var client = Path.Combine(host.RepositoryRoot, "tests", "Forma.Blazor.Browser.Host.Client");
        var linked = new DirectoryInfo(Path.Combine(client, "obj", "Release", "net11.0", "linked"));
        var built = Path.Combine(client, "bin", "Release", "net11.0");

        // Act
        var assemblies = linked.GetFiles("*.dll")
            .Where(after => File.Exists(Path.Combine(built, after.Name)))
            .Select(after => (Name: Path.GetFileNameWithoutExtension(after.Name), Before: new FileInfo(Path.Combine(built, after.Name)).Length, After: after.Length))
            .ToList();

        // Assert
        TestContext.Current.TestOutputHelper?.WriteLine(string.Join(Environment.NewLine, assemblies.Select(assembly => $"{assembly.Name,-50} {assembly.Before,10:N0} -> {assembly.After,10:N0}")));
        var forma = assemblies.Where(assembly => assembly.Name.StartsWith("Forma.", StringComparison.Ordinal)).ToList();
        var framework = assemblies.Except(forma).ToList();
        var libraries = forma.Where(assembly => assembly.Name != "Forma.Blazor.Browser.Host.Client").ToList();
        Assert.Multiple(
            () => Assert.NotEmpty(libraries),
            () => Assert.All(libraries, assembly => Assert.True(assembly.After <= assembly.Before, $"{assembly.Name} grew")),
            () => Assert.True(libraries.Sum(assembly => assembly.After) < libraries.Sum(assembly => assembly.Before), "Forma's libraries were not trimmed"),
            () => Assert.All(framework, assembly => Assert.True(assembly.After < assembly.Before, $"{assembly.Name} was not trimmed")));
    }

    private string GetFramework(string client)
        => Path.Combine(client == "minimal" ? host.HostFolder : host.FullClientFolder, "wwwroot", "_framework");

    private static Dictionary<string, Dictionary<string, long>> ReadBudgets()
        => JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, long>>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "budgets.json")))
            ?? throw new InvalidOperationException("budgets.json holds no budgets.");
}
