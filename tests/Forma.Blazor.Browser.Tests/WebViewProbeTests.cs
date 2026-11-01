namespace Forma.Blazor.Browser.Tests;

/// <summary>What Blazor Hybrid's web view guarantees, in two web views of a WPF application and more, which the WebView design and the culture design's Blazor Hybrid section rest on.</summary>
/// <remarks>Each check starts an application of its own; its first web view, <c>A</c>, starts at <c>/en/probe</c> on the application's UI thread, which carries <c>en-US</c> as an assigned culture, as MAUI assigns the device's.</remarks>
[Collection(WebViewChecks.Name)]
public sealed class WebViewProbeTests
{
    private const string Assigned = "en-US|en-US";
    private const string Switched = "es-ES|es-ES";

    [Fact]
    public async Task Should_render_in_the_web_view_s_own_renderer()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();

        // Act
        var webView = await application.GetWebViewAsync("A");

        // Assert
        await Assertions.Expect(webView.Locator("#renderer")).ToHaveTextAsync("WebView True");
        await Assertions.Expect(webView.Locator("#context")).ToHaveTextAsync("DispatcherSynchronizationContext");
    }

    [Fact]
    public async Task Should_reach_a_component_s_script_and_the_library_s_stylesheets()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var webView = await application.GetWebViewAsync("A");

        // Act
        await webView.Locator(".forma-select-trigger-wrapper button").ClickAsync();
        await webView.GetByRole(AriaRole.Option, new() { Name = "Closed" }).ClickAsync();

        // Assert
        await Assertions.Expect(webView.Locator("#status")).ToHaveTextAsync("2");
        Assert.NotEqual(string.Empty, await webView.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--space-1').trim()"));
    }

    [Fact]
    public async Task Should_start_the_document_s_coordinator_from_a_registration_made_as_the_first_component_initializes()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();

        // Act
        var webView = await application.GetWebViewAsync("A");

        // Assert
        await Assertions.Expect(webView.Locator("#registered")).ToHaveTextAsync("registered");
        await Assertions.Expect(webView.Locator("html")).ToHaveAttributeAsync("lang", "en-US");
        await Assertions.Expect(webView.Locator("html")).ToHaveAttributeAsync("dir", "ltr");
    }

    [Fact]
    public async Task Should_run_another_web_view_s_events_in_a_culture_applied_to_each_web_view_s_thread()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await first.Locator("#apply-threads").ClickAsync();
        await second.GetByRole(AriaRole.Button, new() { Name = "Event" }).ClickAsync();

        // Assert
        await Assertions.Expect(second.Locator("#event")).ToHaveTextAsync($"{Switched}|1.234,5|Hola");
    }

    [Fact]
    public async Task Should_leave_a_thread_s_assigned_culture_in_place_of_new_process_defaults()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await first.Locator("#apply-defaults").ClickAsync();
        await second.GetByRole(AriaRole.Button, new() { Name = "Event" }).ClickAsync();

        // Assert
        await Assertions.Expect(second.Locator("#event")).ToHaveTextAsync($"{Assigned}|1,234.5|Hello");
    }

    [Fact]
    public async Task Should_lose_a_culture_applied_in_a_captured_flow_once_that_flow_ends()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await first.Locator("#apply-captured").ClickAsync();
        await second.GetByRole(AriaRole.Button, new() { Name = "Event" }).ClickAsync();

        // Assert
        await Assertions.Expect(second.Locator("#event")).ToHaveTextAsync($"{Assigned}|1,234.5|Hello");
    }

    [Fact]
    public async Task Should_bind_in_the_culture_applied_to_the_web_view_s_thread()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await first.Locator("#apply-threads").ClickAsync();

        // Act
        await second.Locator("#amount").FillAsync("1,5");
        await second.Locator("#amount").BlurAsync();

        // Assert
        await Assertions.Expect(second.Locator("#amount-value")).ToHaveTextAsync("1.5");
    }

    [Fact]
    public async Task Should_finish_work_begun_before_a_switch_in_the_culture_it_captured()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await second.Locator("#begin-work").ClickAsync();
        await Assertions.Expect(second.Locator("#work")).ToHaveTextAsync($"begun {Assigned}");

        // Act
        await first.Locator("#apply-threads").ClickAsync();
        await second.Locator("#end-work").ClickAsync();

        // Assert
        await Assertions.Expect(second.Locator("#work")).ToHaveTextAsync($"begun {Assigned}, ended {Assigned}");
    }

    [Fact]
    public async Task Should_start_a_web_view_opened_after_a_switch_in_the_switched_culture()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var first = await application.GetWebViewAsync("A");
        await first.Locator("#apply-threads").ClickAsync();

        // Act
        await first.Locator("#open").ClickAsync();
        var second = await application.GetWebViewAsync("B");

        // Assert
        await Assertions.Expect(second.Locator("#render-culture")).ToHaveTextAsync(Switched);
    }

    [Fact]
    public async Task Should_replace_another_web_view_s_address_under_a_mark_its_handler_reads_keeping_its_page()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        var instance = await second.Locator("#instance").TextContentAsync();
        await second.Locator("#draft").FillAsync("kept");
        await second.Locator("#draft").BlurAsync();
        var entries = await second.EvaluateAsync<int>("() => history.length");

        // Act
        await first.Locator("#replace-address").ClickAsync();

        // Assert
        await Assertions.Expect(second.Locator("#segment")).ToHaveTextAsync("es");
        await Assertions.Expect(second.Locator("#instance")).ToHaveTextAsync(instance!);
        await Assertions.Expect(second.Locator("#draft")).ToHaveValueAsync("kept");
        await Assertions.Expect(second.Locator("#navigations li")).ToHaveTextAsync(["changing /es/probe state=forma-culture intercepted=False", "changed /es/probe state=forma-culture intercepted=False"]);
        Assert.Equal(entries, await second.EvaluateAsync<int>("() => history.length"));
    }

    [Fact]
    public async Task Should_let_a_web_view_s_handler_refuse_its_replaced_address()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await second.Locator("#guard").CheckAsync();

        // Act
        await first.Locator("#replace-address").ClickAsync();

        // Assert
        await Assertions.Expect(second.Locator("#navigations li")).ToHaveTextAsync(["changing /es/probe state=forma-culture intercepted=False", "prevented"]);
        await Assertions.Expect(second.Locator("#segment")).ToHaveTextAsync("en");
    }

    [Fact]
    public async Task Should_tell_a_traversal_from_a_navigation_the_web_view_makes()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var webView = await application.GetWebViewAsync("A");
        await webView.Locator("#to-es").ClickAsync();
        await Assertions.Expect(webView.Locator("#segment")).ToHaveTextAsync("es");
        var made = await webView.EvaluateAsync<string[]>("() => [...formaProbe.history]");

        // Act
        await webView.GoBackAsync();

        // Assert
        await Assertions.Expect(webView.Locator("#segment")).ToHaveTextAsync("en");
        var traversed = await webView.EvaluateAsync<string[]>("from => formaProbe.history.slice(from)", made.Length);
        Assert.Equal(["navigate push"], made);
        Assert.Contains("navigate traverse", traversed);
        Assert.Contains("popstate", traversed);
        await Assertions.Expect(webView.Locator("#navigations li")).ToHaveTextAsync(["changing /es/probe state=none intercepted=True", "changed /es/probe state=none intercepted=True", "changing /en/probe state=none intercepted=False", "changed /en/probe state=none intercepted=True"]);
    }

    [Fact]
    public async Task Should_render_a_web_view_s_first_landing_with_no_location_change()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();

        // Act
        var webView = await application.GetWebViewAsync("A");

        // Assert
        await Assertions.Expect(webView.Locator("#segment")).ToHaveTextAsync("en");
        await Assertions.Expect(webView.Locator("#navigations li")).ToHaveCountAsync(0);
        Assert.Empty(await webView.EvaluateAsync<string[]>("() => [...formaProbe.history]"));
    }

    [Fact]
    public async Task Should_keep_a_web_view_s_scope_once_its_native_window_closed_until_it_is_disposed()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await second.Locator("#close").ClickAsync();
        await first.Locator("#open").ClickAsync();
        var third = await application.GetWebViewAsync("C");

        // Act
        await third.Locator("#close-disposing").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#released")).ToHaveTextAsync("1");
    }

    [Fact]
    public async Task Should_release_a_web_view_s_scope_as_it_is_disposed()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await second.Locator("#close-disposing").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#released")).ToHaveTextAsync("1");
        await first.GetByRole(AriaRole.Button, new() { Name = "Event" }).ClickAsync();
        await Assertions.Expect(first.Locator("#event")).ToHaveTextAsync($"{Assigned}|1,234.5|Hello");
    }

    [Fact]
    [Trait("Kind", KnownFailure.Kind)]
    public async Task Should_switch_every_web_view_s_state_through_the_application_s_switch()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        await KnownFailure.AssertStillFailsAsync("A switch requested in one web view changes the application's culture state, which every web view reads.", async () =>
        {
            await Assertions.Expect(first.Locator("#switched")).ToHaveTextAsync("switched");
            await second.GetByRole(AriaRole.Button, new() { Name = "Event" }).ClickAsync();
            await Assertions.Expect(second.Locator("#state")).ToHaveTextAsync(Switched);
        });
    }

    [Fact]
    [Trait("Kind", KnownFailure.Kind)]
    public async Task Should_set_every_web_view_s_document_in_the_switched_culture()
    {
        // Arrange
        await using var application = await WebViewApplication.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        await KnownFailure.AssertStillFailsAsync("Every web view's document takes the language and direction of the pair the application's coordinator accepted.", async () =>
        {
            await Assertions.Expect(first.Locator("html")).ToHaveAttributeAsync("lang", "es-ES");
            await Assertions.Expect(second.Locator("html")).ToHaveAttributeAsync("lang", "es-ES");
        });
    }

    private static async Task<(IPage First, IPage Second)> OpenTwoWebViewsAsync(WebViewApplication application)
    {
        var first = await application.GetWebViewAsync("A");
        await first.Locator("#open").ClickAsync();
        var second = await application.GetWebViewAsync("B");

        return (first, second);
    }
}
