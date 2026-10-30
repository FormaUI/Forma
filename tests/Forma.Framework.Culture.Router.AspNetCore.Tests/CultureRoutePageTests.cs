namespace Forma.Framework.Culture.Router.AspNetCore.Tests;

using System.Globalization;

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

    private static ServiceProvider CreateServices(FakeNavigationManager navigationManager, IJSRuntime js, CultureSwitchMode cultureSwitchMode = CultureSwitchMode.InPlace, CulturePrefix prefix = CulturePrefix.ExceptDefault, ILoggerFactory? loggerFactory = null)
    {
        var services = new ServiceCollection()
            .AddSingleton<NavigationManager>(navigationManager)
            .AddSingleton(js)
            .AddScoped<ICultureSwitch, TestCultureSwitch>();

        if (loggerFactory is not null)
        {
            services.AddSingleton(loggerFactory).AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        }

        return services
            .AddFormaCultureRouting(new CultureRouterOptions
            {
                Prefix = prefix,
                Switch = cultureSwitchMode,
                DefaultCulture = "en",
                Routes =
                [
                    new() { Token = "en", Culture = new("en") },
                    new() { Token = "he", Culture = new("he"), Aliases = ["iw"] }
                ]
            })
            .BuildServiceProvider();
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

    private static RecordingJSObjectReference CreateModule()
        => new((_, _) => null);

    private static RecordingJSRuntime CreateJS(RecordingJSObjectReference module)
        => new((identifier, _) => identifier == "import" ? module : null);

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
            await using var module = CreateModule();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), CreateJS(module));
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he he he", "parameters set he he he"], lifecycle),
                () => Assert.Equal(1, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Empty(renderer.Exceptions));
        }

        [Fact]
        public async Task Should_follow_a_reused_page_routed_through_its_plain_route_to_the_culture_its_address_names()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            await using var module = CreateModule();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), CreateJS(module));
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Act
            await renderer.RenderAsync(CreatePage(null, lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he he he", "parameters set he he he", "parameters set - en en"], lifecycle),
                () => Assert.Equal(2, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Empty(renderer.Exceptions));
        }

        [Fact]
        public async Task Should_change_nothing_for_a_delivery_carrying_the_route_it_follows()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            await using var module = CreateModule();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), CreateJS(module));
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he he he", "parameters set he he he", "parameters set he he he"], lifecycle),
                () => Assert.Equal(1, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Single(module.Recorder.GetCalls("write")));
        }

        [Fact]
        public async Task Should_run_its_lifecycle_in_the_routes_cultures_where_the_state_holds_them_and_the_calling_flow_does_not()
        {
            // Arrange
            using var fixture = new CultureFixture("he");
            await using var module = CreateModule();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), CreateJS(module));
            await using var scope = provider.CreateAsyncScope();
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
                () => Assert.Equal(0, cultureState.Revision),
                () => Assert.Empty(module.Recorder.GetCalls("write")));
        }

        [Fact]
        public async Task Should_run_its_lifecycle_before_the_change_has_been_notified()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            await using var module = CreateModule();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), CreateJS(module));
            await using var scope = provider.CreateAsyncScope();
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
        public async Task Should_log_a_notification_that_fails()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var loggerFactory = new FakeLoggerFactory();
            await using var module = CreateModule();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), CreateJS(module), loggerFactory: loggerFactory);
            await using var scope = provider.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<ICultureState>().Subscribe(() => Task.FromException(new InvalidOperationException("A subscriber failed.")));
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);

            Assert.Multiple(
                () => Assert.Equal(typeof(CultureSwitcher).FullName, entry.Category),
                () => Assert.Equal(LogLevel.Error, entry.Level),
                () => Assert.Equal(["initialized he he he", "parameters set he he he"], lifecycle),
                () => Assert.Empty(renderer.Exceptions));
        }
    }

    public sealed class Cookie
    {
        [Theory]
        [InlineData("http://localhost/", "forma-culture-route", "/", false)]
        [InlineData("http://localhost/app/", "forma-culture-route%2Fapp", "/app", false)]
        [InlineData("https://localhost/my%20app/", "forma-culture-route%2Fmy%20app", "/my%20app", true)]
        public async Task Should_write_the_cookie_of_the_applications_root_where_the_page_changed_the_culture(string baseAddress, string name, string path, bool isSecure)
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            await using var module = CreateModule();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri(baseAddress), new Uri(new Uri(baseAddress), "he/about")), CreateJS(module));
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);

            // Act
            await renderer.RenderAsync(CreatePage("he", []));

            // Assert
            var write = Assert.Single(module.Recorder.GetCalls("write"));

            Assert.Equal([name, "he", path, 31_536_000L, isSecure], write.Arguments);
        }

        [Fact]
        public async Task Should_write_each_change_in_the_order_the_page_made_it()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            await using var module = CreateModule();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), CreateJS(module));
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();
            module.Recorder.Hold("write");

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));
            await renderer.RenderAsync(CreatePage(null, lifecycle));
            var writesWhileHeld = module.Recorder.GetCalls("write").Count;
            module.Recorder.Release("write");
            module.Recorder.GetCalls("write")[0].Complete(null);
            await scope.ServiceProvider.GetRequiredService<CultureSwitcher>().DisposeAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, writesWhileHeld),
                () => Assert.Equal(["he", "en"], module.Recorder.GetCalls("write").Select(write => write.Arguments[1])));
        }
    }

    public sealed class Eligibility
    {
        [Fact]
        public async Task Should_leave_the_culture_state_to_the_handoff_for_an_interactive_page_outside_the_culture_route_scope()
        {
            // Arrange — an island of its own, which lies outside the culture route scope and whose culture segment the middleware removed
            using var fixture = new CultureFixture("he");
            await using var module = CreateModule();
            var navigationManager = new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about"));
            await using var provider = CreateServices(navigationManager, CreateJS(module));
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage(null, lifecycle, isWithinCultureRouteScope: false));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized - he he", "parameters set - he he"], lifecycle),
                () => Assert.Equal(0, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Empty(navigationManager.Navigations),
                () => Assert.Empty(module.Recorder.Calls));
        }

        [Fact]
        public async Task Should_leave_the_culture_state_to_the_middleware_in_a_static_render()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            await using var module = CreateModule();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), CreateJS(module));
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, Static);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he en en", "parameters set he en en"], lifecycle),
                () => Assert.Equal(0, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Empty(module.Recorder.Calls));
        }

        [Fact]
        public async Task Should_leave_the_culture_state_as_it_is_where_a_switch_loads_a_new_document()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            await using var module = CreateModule();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/he/about")), CreateJS(module), CultureSwitchMode.FullLoad);
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, InteractiveServer);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("he", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["initialized he en en", "parameters set he en en"], lifecycle),
                () => Assert.Equal(0, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
                () => Assert.Empty(module.Recorder.Calls));
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
            await using var module = CreateModule();
            var navigationManager = new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/xx/about"));
            var notFound = 0;
            navigationManager.OnNotFound += (_, _) => notFound++;
            await using var provider = CreateServices(navigationManager, CreateJS(module));
            await using var scope = provider.CreateAsyncScope();
            await using var renderer = new FakeRenderer(scope.ServiceProvider, isInteractive ? InteractiveServer : Static);
            var lifecycle = new List<string>();

            // Act
            await renderer.RenderAsync(CreatePage("xx", lifecycle));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, notFound),
                () => Assert.Empty(lifecycle),
                () => Assert.Equal(0, scope.ServiceProvider.GetRequiredService<ICultureState>().Revision),
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
            await using var module = CreateModule();
            var navigationManager = new FakeNavigationManager(new Uri("http://localhost/"), new Uri(address));
            await using var provider = CreateServices(navigationManager, CreateJS(module), prefix: prefix);
            await using var scope = provider.CreateAsyncScope();
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
                () => Assert.Empty(module.Recorder.Calls));
        }
    }
}
