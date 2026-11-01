namespace Forma.Framework.Culture.Router.Tests;

using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;
using Forma.Tests.Culture;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

public sealed class CultureRoutePageTests
{
    private static readonly RendererInfo InteractiveServer = new("Server", isInteractive: true);
    private static readonly RendererInfo Static = new("Static", isInteractive: false);

    private static ServiceProvider CreateServices(FakeNavigationManager navigationManager, FakeCultureCoordinatorBrowser browser, CultureSwitchMode cultureSwitchMode = CultureSwitchMode.InPlace, CulturePrefix prefix = CulturePrefix.ExceptDefault, ILoggerFactory? loggerFactory = null, bool registersOnStart = false)
    {
        browser.UseRoutes("en", ("en", "en", "en"), ("he", "he", "he"));

        var services = new ServiceCollection()
            .AddSingleton<NavigationManager>(navigationManager)
            .AddSingleton<IJSRuntime>(browser.Runtime)
            .AddScoped<ICultureSwitch, TestCultureSwitch>();

        if (loggerFactory is not null)
        {
            services.AddSingleton(loggerFactory).AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        }

        // A render mode that starts nothing of its own, as a web view's, registers the scope as its part in the culture starts.
        if (registersOnStart)
        {
            services.AddScoped<ICultureScopeStart>(scoped => new FakeCultureScopeStart { OnStart = () => _ = scoped.GetRequiredService<CultureCoordinatorRegistration>().RegisterAsync() });
        }

        return services
            .AddFormaCultureRouting(new CultureRouterOptions
            {
                Prefix = prefix,
                Switch = cultureSwitchMode,
                DefaultCulture = "en",
                Routes =
                [
                    new() { Token = "en", UICulture = new("en") },
                    new() { Token = "he", UICulture = new("he"), Aliases = ["iw"] }
                ]
            })
            .AddFormaCultureCoordination()
            .BuildServiceProvider();
    }

    // The scope's state registered with the page's coordinator, as the circuit registers it as its connection comes up.
    private static async Task<IServiceScope> CreateScopeAsync(ServiceProvider provider)
    {
        var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CultureCoordinatorRegistration>().RegisterAsync();

        return scope;
    }

    // The page as a router hands it its route values, under the template's own spelling of the culture segment.
    private static RenderFragment CreatePage(string? culture, List<string> lifecycle, bool isWithinCultureRouteScope = true)
    {
        RenderFragment page = builder =>
        {
            builder.OpenComponent<Page>(0);
            builder.AddComponentParameter(1, "culture", culture);
            builder.AddComponentParameter(2, nameof(Page.Lifecycle), lifecycle);
            builder.CloseComponent();
        };

        if (!isWithinCultureRouteScope)
        {
            return page;
        }

        return builder =>
        {
            builder.OpenComponent<CultureRouteScope>(0);
            builder.AddComponentParameter(1, nameof(CultureRouteScope.ChildContent), page);
            builder.CloseComponent();
        };
    }

    private static IReadOnlyList<RecordedCall> GetLandings(FakeCultureCoordinatorBrowser browser)
        => browser.GetRegistration(browser.Identities.Single()).Recorder.GetCalls("reportLanding");

    /// <summary>A culture route page recording each step of its lifecycle in the cultures it runs in.</summary>
    public sealed class Page : CultureRoutePage
    {
        [Inject] private ICultureState CultureState { get; set; } = default!;

        /// <summary>The steps of the page's lifecycle, each with its culture segment, the state's UI culture and the calling flow's.</summary>
        [Parameter] public List<string> Lifecycle { get; set; } = default!;

        /// <inheritdoc />
        protected override void OnInitialized()
            => Lifecycle.Add($"initialized {Culture ?? "-"} {CultureState.UICulture.Name} {CultureInfo.CurrentUICulture.Name}");

        /// <inheritdoc />
        protected override void OnParametersSet()
            => Lifecycle.Add($"parameters set {Culture ?? "-"} {CultureState.UICulture.Name} {CultureInfo.CurrentUICulture.Name}");
    }

    public sealed class Following
    {
        [Fact]
        public async Task Should_bring_the_culture_state_and_the_calling_flow_to_its_route_before_its_lifecycle_runs()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var browser = new FakeCultureCoordinatorBrowser("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), browser);
            using var scope = await CreateScopeAsync(provider);
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he he he", "parameters set he he he"], lifecycle),
                () => Assert.Equal(1, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Equal(["he"], browser.RouteCookies),
                () => Assert.Empty(renderer.Exceptions));
        }

        [Fact]
        public async Task Should_start_its_scope_s_part_in_the_culture_before_it_reports_its_landing()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var browser = new FakeCultureCoordinatorBrowser("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), browser, registersOnStart: true);
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);

            // Act
            await renderer.RenderAsync(CreatePage("he", []));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["he"], GetLandings(browser).Select(landing => (string?)landing.Arguments[0])),
                () => Assert.Equal("he", scope.ServiceProvider.GetRequiredService<ICultureState>().UICulture.Name));
        }

        [Fact]
        public async Task Should_follow_a_reused_page_routed_through_its_plain_route_to_the_culture_its_address_names()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var browser = new FakeCultureCoordinatorBrowser("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), browser);
            using var scope = await CreateScopeAsync(provider);
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Act
            await renderer.RenderAsync(CreatePage(null, lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he he he", "parameters set he he he", "parameters set - en en"], lifecycle),
                () => Assert.Equal(2, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Equal(["he", null], GetLandings(browser).Select(landing => (string?)landing.Arguments[0])));
        }

        [Fact]
        public async Task Should_report_each_landing_after_the_one_before_it()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var browser = new FakeCultureCoordinatorBrowser("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), browser);
            using var scope = await CreateScopeAsync(provider);
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he he he", "parameters set he he he", "parameters set he he he"], lifecycle),
                () => Assert.Equal(1, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Equal([1, 2], GetLandings(browser).Select(landing => (int)landing.Arguments[1]!)));
        }

        [Fact]
        public async Task Should_run_its_lifecycle_in_the_routes_cultures_where_the_state_holds_them_and_the_calling_flow_does_not()
        {
            // Arrange
            using var fixture = new CultureFixture("he");
            var browser = new FakeCultureCoordinatorBrowser("he");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), browser);
            using var scope = await CreateScopeAsync(provider);
            var cultureState = scope.ServiceProvider.GetRequiredService<ICultureState>();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he he he", "parameters set he he he"], lifecycle),
                () => Assert.Equal(0, cultureState.Revision));
        }

        [Fact]
        public async Task Should_run_its_lifecycle_before_the_change_has_been_notified()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var browser = new FakeCultureCoordinatorBrowser("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), browser);
            using var scope = await CreateScopeAsync(provider);
            var gate = new TaskCompletionSource();
            scope.ServiceProvider.GetRequiredService<ICultureState>().Subscribe(() => gate.Task);
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            Assert.Equal(["initialized he he he", "parameters set he he he"], lifecycle);
            gate.SetResult();
        }

        [Fact]
        public async Task Should_log_a_notification_that_fails_under_the_registration()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeCultureCoordinatorBrowser("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), browser, loggerFactory: loggerFactory);
            using var scope = await CreateScopeAsync(provider);
            scope.ServiceProvider.GetRequiredService<ICultureState>().Subscribe(() => Task.FromException(new InvalidOperationException("A subscriber failed.")));
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);

            Assert.Multiple(
                () => Assert.Equal(typeof(CultureCoordinatorRegistration).FullName, entry.Category),
                () => Assert.Equal(LogLevel.Error, entry.Level),
                () => Assert.Equal(["initialized he he he", "parameters set he he he"], lifecycle),
                () => Assert.Empty(renderer.Exceptions));
        }

        [Fact]
        public async Task Should_render_in_the_culture_state_as_it_stands_and_log_a_landing_that_fails()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var loggerFactory = new FakeLoggerFactory();
            var browser = new FakeCultureCoordinatorBrowser("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), browser, loggerFactory: loggerFactory);
            using var scope = await CreateScopeAsync(provider);
            browser.FailNext("reportLanding", new JSException("The coordinator is gone."));
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            var entry = Assert.Single(loggerFactory.Entries, candidate => candidate.Category == typeof(CultureSwitcher).FullName);

            Assert.Multiple(
                () => Assert.Equal(LogLevel.Error, entry.Level),
                () => Assert.Equal(["initialized he en en", "parameters set he en en"], lifecycle),
                () => Assert.Empty(renderer.Exceptions));
        }
    }

    public sealed class Eligibility
    {
        [Fact]
        public async Task Should_leave_the_culture_state_to_the_server_rendered_landing_for_an_interactive_page_outside_the_culture_route_scope()
        {
            // Arrange — an island of its own, which lies outside the culture route scope and whose culture segment the middleware removed
            using var fixture = new CultureFixture("he");
            var browser = new FakeCultureCoordinatorBrowser("he");
            var navigationManager = new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about"));
            await using var provider = CreateServices(navigationManager, browser);
            using var scope = await CreateScopeAsync(provider);
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage(null, lifecycle, isWithinCultureRouteScope: false));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized - he he", "parameters set - he he"], lifecycle),
                () => Assert.Equal(0, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Empty(navigationManager.Navigations),
                () => Assert.Empty(GetLandings(browser)));
        }

        [Fact]
        public async Task Should_leave_the_culture_state_to_the_middleware_in_a_static_render()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var browser = new FakeCultureCoordinatorBrowser("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), browser);
            using var scope = await CreateScopeAsync(provider);
            await using var renderer = new FakeRenderer(scope.ServiceProvider, Static);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he en en", "parameters set he en en"], lifecycle),
                () => Assert.Equal(0, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Empty(GetLandings(browser)));
        }

        [Fact]
        public async Task Should_leave_the_culture_state_as_it_is_where_a_switch_loads_a_new_document()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var browser = new FakeCultureCoordinatorBrowser("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), browser, CultureSwitchMode.FullLoad);
            using var scope = await CreateScopeAsync(provider);
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he en en", "parameters set he en en"], lifecycle),
                () => Assert.Equal(0, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Empty(GetLandings(browser)));
        }
    }

    public sealed class Validation
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Should_stop_a_segment_that_isnt_a_cultures_token_with_the_not_found_outcome_before_its_lifecycle(bool isInteractive)
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var browser = new FakeCultureCoordinatorBrowser("en");
            var navigationManager = new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/xx/about"));
            var notFound = 0;
            navigationManager.OnNotFound += (_, _) => notFound++;
            await using var provider = CreateServices(navigationManager, browser);
            using var scope = await CreateScopeAsync(provider);
            await using var renderer = new FakeRenderer(scope.ServiceProvider, isInteractive ? InteractiveServer : Static);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("xx", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, notFound),
                () => Assert.Empty(lifecycle),
                () => Assert.Equal(0, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Empty(GetLandings(browser)),
                () => Assert.Empty(renderer.Exceptions));
        }

        [Theory]
        [InlineData(CulturePrefix.ExceptDefault, "http://localhost/iw/about?tab=2", "iw", "http://localhost/he/about?tab=2")]
        [InlineData(CulturePrefix.ExceptDefault, "http://localhost/HE/about", "HE", "http://localhost/he/about")]
        [InlineData(CulturePrefix.ExceptDefault, "http://localhost/en/about", "en", "http://localhost/about")]
        [InlineData(CulturePrefix.Always, "http://localhost/about", null, "http://localhost/en/about")]
        public async Task Should_replace_an_address_that_isnt_its_cultures_canonical_one_before_its_lifecycle(CulturePrefix prefix, string address, string? culture, string canonical)
        {
            // Arrange
            using var fixture = new CultureFixture("he");
            var browser = new FakeCultureCoordinatorBrowser("he");
            var navigationManager = new FakeNavigationManager(new Uri("http://localhost/"), new Uri(address));
            await using var provider = CreateServices(navigationManager, browser, prefix: prefix);
            using var scope = await CreateScopeAsync(provider);
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage(culture, lifecycle));

            // Assert
            var navigation = Assert.Single(navigationManager.Navigations);

            Assert.Multiple(
                () => Assert.Equal(canonical, navigation.Uri),
                () => Assert.True(navigation.Options.ReplaceHistoryEntry),
                () => Assert.False(navigation.Options.ForceLoad),
                () => Assert.Empty(lifecycle),
                () => Assert.Equal(0, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Empty(GetLandings(browser)));
        }
    }
}
