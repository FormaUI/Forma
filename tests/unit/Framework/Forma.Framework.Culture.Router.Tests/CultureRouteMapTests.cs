namespace Forma.Framework.Culture.Router.Tests;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

using Forma.Framework.Culture.Browser.Interop;
using Forma.Tests.Culture;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

/// <summary>The mapping the page publishes, against which the coordinator's script builds destinations and resolves landings.</summary>
/// <remarks>
/// The script's tests read the cases this writes, so the browser and the server are held to the same addresses: the map
/// each configuration publishes, and the destination the switcher builds for each address and route.
/// </remarks>
public sealed class CultureRouteMapTests
{
    // Set to 1 to write the cases afresh after a change to how either side builds an address.
    private const string UpdateVariable = "FORMA_UPDATE_FIXTURES";

    private static readonly JsonSerializerOptions WireOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly string[] Roots = ["/", "/app/"];

    private static readonly string[] Addresses = ["about", "he/about?tab=2#top", "", "iw/x/y/", "about/", "HE/about", "es-419/about", "he-de/about?q=a%20b"];

    private static readonly string[] Tokens = ["he", "HE", "iw", "es-419", "he-de", "xx"];

    private static CultureRouterOptions CreateOptions(CulturePrefix prefix)
        => new()
        {
            Prefix = prefix,
            Switch = CultureSwitchMode.InPlace,
            DefaultCulture = "en",
            Routes =
            [
                new() { Token = "en", UICulture = new("en") },
                new() { Token = "he", UICulture = new("he"), Aliases = ["iw"] },
                new() { Token = "es", UICulture = new("es-419"), HrefLang = "es", Aliases = ["es-419"] },
                new() { Token = "he-de", Culture = new("de-DE"), UICulture = new("he"), HrefLang = "he" },
            ],
        };

    private static string GetFixturePath()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (folder.GetFiles("*.slnx").Length > 0)
            {
                return Path.Combine(folder.FullName, "tests", "scripts", "culture", "culture-routes.json");
            }
        }

        throw new InvalidOperationException("The tests run from outside the repository.");
    }

    // The destination the switcher builds for the address relative to the root.
    private static async Task<string> GetDestinationAsync(CulturePrefix prefix, string root, string address, string token)
    {
        using var fixture = new CultureFixture("en");
        var baseUri = new Uri($"http://localhost{root}");
        await using var provider = new ServiceCollection()
            .AddSingleton<NavigationManager>(new FakeNavigationManager(baseUri, new Uri(baseUri, address)))
            .AddSingleton<IJSRuntime>(new FakeCultureCoordinatorBrowser().Runtime)
            .AddScoped<ICultureSwitch, TestCultureSwitch>()
            .AddFormaCultureRouting(CreateOptions(prefix))
            .AddFormaCultureCoordination()
            .BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var switcher = scope.ServiceProvider.GetRequiredService<CultureSwitcher>();
        var route = scope.ServiceProvider.GetRequiredService<CultureRouter>().Routes.Single(candidate => candidate.Token == token);

        return switcher.GetUri(route).AbsoluteUri;
    }

    private static async Task<string> BuildCasesAsync()
    {
        var configurations = new JsonArray();

        foreach (var prefix in new[] { CulturePrefix.ExceptDefault, CulturePrefix.Always })
        {
            var router = new CultureRouter(CreateOptions(prefix));

            foreach (var root in Roots)
            {
                var cases = new JsonArray();
                foreach (var address in Addresses)
                {
                    foreach (var route in router.Routes)
                    {
                        var destination = await GetDestinationAsync(prefix, root, address, route.Token);

                        cases.Add(new JsonObject { ["href"] = $"http://localhost{root}{address}", ["token"] = route.Token, ["destination"] = destination });
                    }
                }

                configurations.Add(new JsonObject
                {
                    ["map"] = JsonSerializer.SerializeToNode(router.GetMap(root), WireOptions),
                    ["destinations"] = cases,
                    ["tokens"] = new JsonArray([.. Tokens.Select(token => (JsonNode)new JsonObject { ["token"] = token, ["route"] = router.FindRoute(token)?.Token })]),
                });
            }
        }

        return new JsonObject { ["configurations"] = configurations }.ToJsonString(WireOptions).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    [Fact]
    public async Task Should_publish_the_map_and_destinations_the_scripts_cases_hold()
    {
        // Arrange
        var path = GetFixturePath();
        var expected = await BuildCasesAsync();

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await File.WriteAllTextAsync(path, expected, TestContext.Current.CancellationToken);
        }

        // Act
        var held = File.Exists(path) ? await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken) : string.Empty;

        // Assert
        Assert.True(held == expected, $"The script's culture route cases are out of date; run this test with {UpdateVariable}=1 to write them afresh.");
    }

    [Fact]
    public void Should_publish_each_routes_pair_and_direction_and_the_cookie_of_the_applications_root()
    {
        // Arrange
        var router = new CultureRouter(CreateOptions(CulturePrefix.ExceptDefault));

        // Act
        var map = router.GetMap("/my%20app/");

        // Assert
        var german = map.Routes.Single(route => route.Token == "he-de");
        Assert.Multiple(
            () => Assert.Equal(("de-DE", "he", "rtl"), (german.Culture, german.UICulture, german.Direction)),
            () => Assert.Equal("forma-culture-route%2Fmy%20app", map.CookieName),
            () => Assert.Equal("/my%20app", map.CookiePath),
            () => Assert.Equal("/my%20app/", map.Root),
            () => Assert.False(map.PrefixesDefault),
            () => Assert.True(map.SwitchesInPlace));
    }

    [Theory]
    [InlineData(CultureSwitchMode.FullLoad, "de-DE", "he", "/app/he-de/")]
    [InlineData(CultureSwitchMode.FullLoad, "he", "he", "/app/he/")]
    [InlineData(CultureSwitchMode.FullLoad, "fr", "fr", "/app/")]
    [InlineData(CultureSwitchMode.InPlace, "de-DE", "he", "/app/")]
    public void Should_give_the_document_the_base_of_the_route_carrying_its_pair(CultureSwitchMode cultureSwitchMode, string culture, string uiCulture, string expected)
    {
        // Arrange
        var router = new CultureRouter(new CultureRouterOptions
        {
            Prefix = CulturePrefix.ExceptDefault,
            Switch = cultureSwitchMode,
            DefaultCulture = "en",
            Routes = CreateOptions(CulturePrefix.ExceptDefault).Routes,
        });

        // Act
        var documentBase = router.GetDocumentBase("/app/", new(culture), new(uiCulture));

        // Assert
        Assert.Equal(expected, documentBase);
    }
}
