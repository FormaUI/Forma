namespace Forma.Framework.Culture.Blazor.WebView.Tests;

using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;
using Forma.Framework.Culture.Router;
using Forma.Tests.Culture;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;

public sealed class ServiceCollectionExtensionsTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he");

    // Each web view's scope has a document of its own, which the test hands it as it opens the web view.
    private static ServiceProvider CreateServices(Action<IServiceCollection>? configure = null, Action<IServiceCollection>? configureAfter = null, CultureArrangement arrangement = CultureArrangement.Coordinated)
    {
        var services = new ServiceCollection()
            .AddScoped(_ => new WebViewDocument())
            .AddScoped<IJSRuntime>(provider => provider.GetRequiredService<WebViewDocument>().Browser.Runtime)
            .AddScoped<NavigationManager>(_ => new FakeNavigationManager(new Uri("https://0.0.0.1/"), new Uri("https://0.0.0.1/about")));

        configure?.Invoke(services);
        services.AddFormaCultureWebView(arrangement);
        configureAfter?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private static void AddRouting(IServiceCollection services, CultureSwitchMode mode = CultureSwitchMode.InPlace)
        => services.AddFormaCultureRouting(new CultureRouterOptions
        {
            DefaultCulture = "en",
            Switch = mode,
            Routes = [new() { Token = "en", UICulture = CultureInfo.GetCultureInfo("en-US") }, new() { Token = "he", UICulture = Hebrew }],
        });

    // A web view opens as its first component initializes, which starts its part in the culture.
    private static AsyncServiceScope OpenWebView(ServiceProvider provider, FakeCultureCoordinatorBrowser browser)
    {
        var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<WebViewDocument>().Browser = browser;
        scope.ServiceProvider.GetRequiredService<ICultureScopeStart>().Start();

        return scope;
    }

    private static object GetStart(FakeCultureCoordinatorBrowser browser)
        => Assert.Single(browser.Module.Recorder.GetCalls("register")).Arguments[3]!;

    private static IReadOnlyDictionary<string, string> GetStartAttributes(FakeCultureCoordinatorBrowser browser, string name)
        => (IReadOnlyDictionary<string, string>)InteropPayload.GetValue(GetStart(browser), name)!;

    private static Task SwitchAsync(ServiceProvider provider, CultureInfo culture, CultureInfo uiCulture)
        => provider.GetRequiredService<ICultureSwitch>().SwitchAsync(culture, uiCulture);

    private static string Describe(ICultureState state)
        => $"{state.Culture.Name}/{state.UICulture.Name}";

    private static string GetDefaults()
        => $"{CultureInfo.DefaultThreadCurrentCulture?.Name}/{CultureInfo.DefaultThreadCurrentUICulture?.Name}";

    private static string GetCurrent()
        => $"{CultureInfo.CurrentCulture.Name}/{CultureInfo.CurrentUICulture.Name}";

    private static string GetPatterns(ICultureState state)
        => $"{state.Culture.DateTimeFormat.ShortTimePattern}/{state.UICulture.DateTimeFormat.ShortTimePattern}";

    public sealed class Lifetimes : IDisposable
    {
        private readonly ProcessCultures _cultures = new();

        public void Dispose()
            => _cultures.Dispose();

        [Fact]
        public async Task Should_hold_one_culture_state_and_one_switch_for_every_web_view()
        {
            // Arrange
            await using var provider = CreateServices();

            // Act
            await using var first = OpenWebView(provider, new FakeCultureCoordinatorBrowser());
            await using var second = OpenWebView(provider, new FakeCultureCoordinatorBrowser());

            // Assert
            Assert.Multiple(
                () => Assert.Same(first.ServiceProvider.GetRequiredService<ICultureState>(), second.ServiceProvider.GetRequiredService<ICultureState>()),
                () => Assert.Same(first.ServiceProvider.GetRequiredService<ICultureSwitch>(), second.ServiceProvider.GetRequiredService<ICultureSwitch>()),
                () => Assert.NotSame(first.ServiceProvider.GetRequiredService<CultureCoordinatorRegistration>(), second.ServiceProvider.GetRequiredService<CultureCoordinatorRegistration>()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Should_hold_one_culture_state_whether_culture_routing_registers_before_or_after_it(bool isRoutingFirst)
        {
            // Arrange
            await using var provider = isRoutingFirst ? CreateServices(services => AddRouting(services)) : CreateServices(configureAfter: services => AddRouting(services));

            // Act
            await using var first = OpenWebView(provider, new FakeCultureCoordinatorBrowser());
            await using var second = OpenWebView(provider, new FakeCultureCoordinatorBrowser());

            // Assert
            Assert.Same(first.ServiceProvider.GetRequiredService<ICultureState>(), second.ServiceProvider.GetRequiredService<ICultureState>());
        }

        [Fact]
        public async Task Should_reject_a_culture_state_each_web_view_resolves_for_itself()
        {
            // Arrange
            await using var provider = CreateServices(services => services.AddScoped<ICultureState>(_ => new FakeCultureState(Hebrew)));

            // Act
            var exception = Record.Exception(() => OpenWebView(provider, new FakeCultureCoordinatorBrowser()));

            // Assert
            Assert.Contains("singleton", Assert.IsType<InvalidOperationException>(exception).Message, StringComparison.Ordinal);
        }
    }

    public sealed class WebViews : IDisposable
    {
        private readonly ProcessCultures _cultures = new();

        public void Dispose()
            => _cultures.Dispose();

        [Fact]
        public async Task Should_start_a_web_view_s_document_from_the_application_s_pair_under_a_policy_that_follows_it()
        {
            // Arrange
            await using var provider = CreateServices();
            var browser = new FakeCultureCoordinatorBrowser();

            // Act
            await using var webView = OpenWebView(provider, browser);

            // Assert
            var marker = GetStartAttributes(browser, "marker");
            var policy = GetStartAttributes(browser, "policy");
            Assert.Multiple(
                () => Assert.Equal("en-US/en-US", $"{marker["data-culture"]}/{marker["data-ui-culture"]}"),
                () => Assert.Equal("state", policy["content"]),
                () => Assert.True(policy.ContainsKey("data-follows")),
                () => Assert.True(policy.ContainsKey("data-web-view")),
                () => Assert.Equal("ltr", InteropPayload.GetValue(GetStart(browser), "direction")));
        }

        [Fact]
        public async Task Should_register_a_web_view_once_however_many_of_its_components_start_it()
        {
            // Arrange
            await using var provider = CreateServices();
            var browser = new FakeCultureCoordinatorBrowser();
            await using var webView = OpenWebView(provider, browser);

            // Act
            webView.ServiceProvider.GetRequiredService<ICultureScopeStart>().Start();

            // Assert
            Assert.Equal(1, browser.CountRegistrations());
        }

        [Fact]
        public async Task Should_start_a_web_view_opened_after_a_switch_in_the_pair_the_switch_accepted()
        {
            // Arrange
            await using var provider = CreateServices();
            await SwitchAsync(provider, Hebrew, Hebrew);
            var browser = new FakeCultureCoordinatorBrowser("he");

            // Act
            await using var webView = OpenWebView(provider, browser);

            // Assert
            var marker = GetStartAttributes(browser, "marker");
            Assert.Multiple(
                () => Assert.Equal("he/he", $"{marker["data-culture"]}/{marker["data-ui-culture"]}"),
                () => Assert.Equal("rtl", InteropPayload.GetValue(GetStart(browser), "direction")));
        }

        [Fact]
        public async Task Should_apply_a_switch_to_the_state_and_the_process_s_defaults_and_have_every_web_view_s_document_follow_it()
        {
            // Arrange
            await using var provider = CreateServices();
            var first = new FakeCultureCoordinatorBrowser();
            var second = new FakeCultureCoordinatorBrowser();
            await using var firstWebView = OpenWebView(provider, first);
            await using var secondWebView = OpenWebView(provider, second);

            // Act
            await SwitchAsync(provider, German, Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", Describe(provider.GetRequiredService<ICultureState>())),
                () => Assert.Equal("de-DE/he", GetDefaults()),
                () => Assert.Equal([("de-DE", "he")], first.Followed),
                () => Assert.Equal([("de-DE", "he")], second.Followed));
        }

        [Fact]
        public async Task Should_hold_the_provider_s_cultures_for_a_switch_and_for_a_web_view_s_landing()
        {
            // Arrange
            await using var provider = CreateServices(services => services.AddSingleton<ICultureProvider>(new FakeCultureProvider()));
            var browser = new FakeCultureCoordinatorBrowser();
            await using var webView = OpenWebView(provider, browser);
            var state = provider.GetRequiredService<ICultureState>();
            var patterns = $"{FakeCultureProvider.ShortTimePattern}/{FakeCultureProvider.ShortTimePattern}";

            // Act
            await SwitchAsync(provider, German, Hebrew);
            var switched = $"{Describe(state)} {GetPatterns(state)} {CultureInfo.DefaultThreadCurrentCulture?.DateTimeFormat.ShortTimePattern}";
            await browser.LandAsync("en-US", "en-US");

            // Assert
            Assert.Multiple(
                () => Assert.Equal($"de-DE/he {patterns} {FakeCultureProvider.ShortTimePattern}", switched),
                () => Assert.Equal($"en-US/en-US {patterns}", $"{Describe(state)} {GetPatterns(state)}"));
        }

        [Fact]
        public async Task Should_switch_the_application_as_a_web_view_lands_at_another_culture_s_address()
        {
            // Arrange
            await using var provider = CreateServices();
            var landing = new FakeCultureCoordinatorBrowser();
            var other = new FakeCultureCoordinatorBrowser();
            await using var landingWebView = OpenWebView(provider, landing);
            await using var otherWebView = OpenWebView(provider, other);

            // Act
            await landing.LandAsync("he", "he");

            // Assert
            Assert.Multiple(
                () => Assert.Equal("he/he", Describe(provider.GetRequiredService<ICultureState>())),
                () => Assert.Equal([("he", "he")], other.Followed));
        }

        [Fact]
        public async Task Should_keep_the_formats_the_state_holds_as_a_web_view_s_document_adopts_the_same_pair_by_name()
        {
            // Arrange
            await using var provider = CreateServices();
            var pinned = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
            pinned.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            await SwitchAsync(provider, pinned, pinned);

            // Act
            await using var webView = OpenWebView(provider, new FakeCultureCoordinatorBrowser());

            // Assert
            Assert.Equal("yyyy-MM-dd", provider.GetRequiredService<ICultureState>().Culture.DateTimeFormat.ShortDatePattern);
        }

        [Fact]
        public async Task Should_leave_out_a_web_view_whose_scope_was_disposed()
        {
            // Arrange
            await using var provider = CreateServices();
            var closed = new FakeCultureCoordinatorBrowser();
            var open = new FakeCultureCoordinatorBrowser();
            await using (OpenWebView(provider, closed))
            {
            }

            await using var openWebView = OpenWebView(provider, open);

            // Act
            await SwitchAsync(provider, Hebrew, Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.Empty(closed.Followed),
                () => Assert.Equal([("he", "he")], open.Followed));
        }

        [Fact]
        public async Task Should_leave_out_a_web_view_whose_document_could_not_follow_a_switch()
        {
            // Arrange
            await using var provider = CreateServices();
            var abandoned = new FakeCultureCoordinatorBrowser();
            await using var webView = OpenWebView(provider, abandoned);
            abandoned.FailNext("follow", new JSDisconnectedException("The web view's native window closed."));
            await SwitchAsync(provider, Hebrew, Hebrew);

            // Act
            await SwitchAsync(provider, German, German);

            // Assert
            Assert.Multiple(
                () => Assert.Empty(abandoned.Followed),
                () => Assert.Equal("de-DE/de-DE", Describe(provider.GetRequiredService<ICultureState>())));
        }

        [Theory]
        [InlineData(59, 2)]
        [InlineData(60, 1)]
        public async Task Should_leave_out_a_web_view_whose_document_left_a_follow_unanswered_for_a_minute(int seconds, int follows)
        {
            // Arrange
            var clock = new FakeTimeProvider();
            await using var provider = CreateServices(services => services.AddSingleton<TimeProvider>(clock));
            var unanswering = new FakeCultureCoordinatorBrowser();
            await using var webView = OpenWebView(provider, unanswering);
            var registration = unanswering.GetRegistration(Assert.Single(unanswering.Identities));
            registration.Recorder.Hold("follow");
            await SwitchAsync(provider, Hebrew, Hebrew);
            clock.Advance(TimeSpan.FromSeconds(seconds));

            // Act
            await SwitchAsync(provider, German, German);

            // Assert
            Assert.Equal(follows, registration.Recorder.GetCalls("follow").Count);
        }

        [Fact]
        public async Task Should_put_the_accepted_pair_on_each_web_view_s_UI_thread_for_its_later_events()
        {
            // Arrange
            await using var provider = CreateServices();
            using var thread = new UIThreadSynchronizationContext();
            await thread.RunAsync(() => CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR"));
            await using var webView = await thread.RunAsync(() => OpenWebView(provider, new FakeCultureCoordinatorBrowser()));
            var opened = await thread.RunAsync(GetCurrent);

            // Act
            await SwitchAsync(provider, German, Hebrew);

            // Assert
            var switched = await thread.RunAsync(GetCurrent);
            Assert.Multiple(
                () => Assert.Equal("en-US/en-US", opened),
                () => Assert.Equal("de-DE/he", switched));
        }

        [Fact]
        public async Task Should_apply_a_pair_given_the_integration_as_the_application_s_next_switch()
        {
            // Arrange
            await using var provider = CreateServices();
            var browser = new FakeCultureCoordinatorBrowser();
            await using var webView = OpenWebView(provider, browser);
            var integration = webView.ServiceProvider.GetRequiredService<ICultureStateIntegration>();

            // Act
            await new LocalCultureSwitch(integration).SwitchAsync(Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("he/he", Describe(provider.GetRequiredService<ICultureState>())),
                () => Assert.Equal([("he", "he")], browser.Followed));
        }
    }

    public sealed class Routing : IDisposable
    {
        private readonly ProcessCultures _cultures = new();

        public void Dispose()
            => _cultures.Dispose();

        [Fact]
        public async Task Should_start_a_web_view_s_document_under_the_address_policy_that_follows_the_application()
        {
            // Arrange
            await using var provider = CreateServices(services => AddRouting(services));
            var browser = new FakeCultureCoordinatorBrowser();

            // Act
            await using var webView = OpenWebView(provider, browser);

            // Assert
            var policy = GetStartAttributes(browser, "policy");
            Assert.Multiple(
                () => Assert.Equal("address /", $"{policy["content"]} {policy["data-root"]}"),
                () => Assert.True(policy.ContainsKey("data-follows")));
        }

        [Fact]
        public async Task Should_reject_a_pair_no_route_carries_before_anything_changes()
        {
            // Arrange
            await using var provider = CreateServices(services => AddRouting(services));
            var browser = new FakeCultureCoordinatorBrowser();
            await using var webView = OpenWebView(provider, browser);

            // Act
            var exception = await Record.ExceptionAsync(() => SwitchAsync(provider, German, German));

            // Assert
            Assert.Multiple(
                () => Assert.IsType<InvalidOperationException>(exception),
                () => Assert.Equal("en-US/en-US", Describe(provider.GetRequiredService<ICultureState>())),
                () => Assert.Empty(browser.Followed));
        }

        [Fact]
        public async Task Should_reject_a_full_load_switch_as_the_coordinator_is_first_resolved()
        {
            // Arrange
            await using var provider = CreateServices(services => AddRouting(services, CultureSwitchMode.FullLoad));

            // Act
            var exception = Record.Exception(provider.GetRequiredService<ICultureSwitch>);

            // Assert
            Assert.Contains(nameof(CultureSwitchMode.InPlace), Assert.IsType<InvalidOperationException>(exception).Message, StringComparison.Ordinal);
        }
    }

    public sealed class CulturePerWebView : IDisposable
    {
        private readonly ProcessCultures _cultures = new();

        public void Dispose()
            => _cultures.Dispose();

        private static ServiceProvider CreateServices(Action<IServiceCollection>? configure = null)
            => ServiceCollectionExtensionsTests.CreateServices(configure, arrangement: CultureArrangement.Independent);

        private static Task SwitchAsync(AsyncServiceScope webView, CultureInfo culture, CultureInfo uiCulture)
            => webView.ServiceProvider.GetRequiredService<ICultureSwitch>().SwitchAsync(culture, uiCulture);

        private static string Describe(AsyncServiceScope webView)
            => ServiceCollectionExtensionsTests.Describe(webView.ServiceProvider.GetRequiredService<ICultureState>());

        [Fact]
        public async Task Should_hold_a_culture_state_and_a_switch_for_each_web_view()
        {
            // Arrange
            await using var provider = CreateServices();

            // Act
            await using var first = OpenWebView(provider, new FakeCultureCoordinatorBrowser());
            await using var second = OpenWebView(provider, new FakeCultureCoordinatorBrowser());

            // Assert
            Assert.Multiple(
                () => Assert.NotSame(first.ServiceProvider.GetRequiredService<ICultureState>(), second.ServiceProvider.GetRequiredService<ICultureState>()),
                () => Assert.NotSame(first.ServiceProvider.GetRequiredService<ICultureSwitch>(), second.ServiceProvider.GetRequiredService<ICultureSwitch>()));
        }

        [Fact]
        public async Task Should_start_a_web_view_s_document_from_its_own_pair_under_a_policy_of_its_own()
        {
            // Arrange
            await using var provider = CreateServices();
            var browser = new FakeCultureCoordinatorBrowser();

            // Act
            await using var webView = OpenWebView(provider, browser);

            // Assert
            var marker = GetStartAttributes(browser, "marker");
            var policy = GetStartAttributes(browser, "policy");
            Assert.Multiple(
                () => Assert.Equal("en-US/en-US", $"{marker["data-culture"]}/{marker["data-ui-culture"]}"),
                () => Assert.Equal("state", policy["content"]),
                () => Assert.False(policy.ContainsKey("data-follows")),
                () => Assert.True(policy.ContainsKey("data-web-view")));
        }

        [Fact]
        public async Task Should_switch_a_web_view_alone_leaving_the_others_and_the_process_s_defaults()
        {
            // Arrange
            await using var provider = CreateServices();
            var defaults = GetDefaults();
            var other = new FakeCultureCoordinatorBrowser();
            await using var switching = OpenWebView(provider, new FakeCultureCoordinatorBrowser());
            await using var otherWebView = OpenWebView(provider, other);

            // Act
            await SwitchAsync(switching, German, Hebrew);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", Describe(switching)),
                () => Assert.Equal("en-US/en-US", Describe(otherWebView)),
                () => Assert.Equal(defaults, GetDefaults()),
                () => Assert.Empty(other.Followed));
        }

        [Fact]
        public async Task Should_switch_a_web_view_alone_as_it_lands_at_another_culture_s_address()
        {
            // Arrange
            await using var provider = CreateServices();
            var landing = new FakeCultureCoordinatorBrowser();
            await using var landingWebView = OpenWebView(provider, landing);
            await using var otherWebView = OpenWebView(provider, new FakeCultureCoordinatorBrowser());

            // Act
            await landing.LandAsync("he", "he");

            // Assert
            Assert.Multiple(
                () => Assert.Equal("he/he", Describe(landingWebView)),
                () => Assert.Equal("en-US/en-US", Describe(otherWebView)));
        }

        [Fact]
        public async Task Should_reject_a_culture_state_two_web_views_share()
        {
            // Arrange
            await using var provider = CreateServices(services => services.AddSingleton<ICultureState>(_ => new FakeCultureState(Hebrew)));
            await using var first = OpenWebView(provider, new FakeCultureCoordinatorBrowser());

            // Act
            var exception = Record.Exception(() => OpenWebView(provider, new FakeCultureCoordinatorBrowser()));

            // Assert
            Assert.Contains("scoped", Assert.IsType<InvalidOperationException>(exception).Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Should_reject_a_full_load_switch_and_a_cookie_policy_as_a_web_view_starts()
        {
            // Arrange
            await using var fullLoad = CreateServices(services => AddRouting(services, CultureSwitchMode.FullLoad));
            await using var cookie = CreateServices(services => services.AddFormaCultureSwitchPolicy(new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie }));

            // Act
            var fullLoadException = Record.Exception(() => OpenWebView(fullLoad, new FakeCultureCoordinatorBrowser()));
            var cookieException = Record.Exception(() => OpenWebView(cookie, new FakeCultureCoordinatorBrowser()));

            // Assert
            Assert.Multiple(
                () => Assert.Contains(nameof(CultureSwitchMode.InPlace), Assert.IsType<InvalidOperationException>(fullLoadException).Message, StringComparison.Ordinal),
                () => Assert.Contains(nameof(CultureSwitchPersistence.State), Assert.IsType<InvalidOperationException>(cookieException).Message, StringComparison.Ordinal));
        }

        [Fact]
        public void Should_reject_an_arrangement_neither_coordinated_nor_independent()
        {
            // Act
            var exception = Record.Exception(() => new ServiceCollection().AddFormaCultureWebView((CultureArrangement)2));

            // Assert
            Assert.IsType<ArgumentOutOfRangeException>(exception);
        }
    }

    public sealed class Policy
    {
        [Fact]
        public async Task Should_reject_a_cookie_policy_as_the_coordinator_is_first_resolved()
        {
            // Arrange
            await using var provider = CreateServices(services => services.AddFormaCultureSwitchPolicy(new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie }));

            // Act
            var exception = Record.Exception(provider.GetRequiredService<ICultureSwitch>);

            // Assert
            Assert.Contains(nameof(CultureSwitchPersistence.State), Assert.IsType<InvalidOperationException>(exception).Message, StringComparison.Ordinal);
        }
    }

    /// <summary>The document of the web view a scope belongs to.</summary>
    private sealed class WebViewDocument
    {
        public FakeCultureCoordinatorBrowser Browser { get; set; } = default!;
    }

    /// <summary>Holds the test's flow in English and restores the process's default cultures, which an application sets.</summary>
    private sealed class ProcessCultures : IDisposable
    {
        private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        private readonly CultureInfo? _defaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
        private readonly CultureFixture _fixture = new("en-US");

        public void Dispose()
        {
            CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _defaultUICulture;
            _fixture.Dispose();
        }
    }
}
