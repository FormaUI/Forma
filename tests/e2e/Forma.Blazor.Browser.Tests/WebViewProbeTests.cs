namespace Forma.Blazor.Browser.Tests;

/// <summary>What Blazor Hybrid's web view guarantees, in two web views of a WPF application and more, which the WebView design and the culture design's Blazor Hybrid section rest on.</summary>
/// <remarks>Each check starts an application of its own; its first web view, <c>A</c>, starts at <c>/en/probe</c> on the application's UI thread, which carries <c>en-US</c> as an assigned culture, as MAUI assigns the device's. A check applies a culture itself where it probes how one reaches a web view.</remarks>
[Collection(WebViewChecks.Name)]
public sealed class WebViewProbeTests
{
    private const string Assigned = "en-US|en-US";
    private const string Switched = "es-ES|es-ES";

    [Fact]
    public async Task Should_render_in_the_web_view_s_own_renderer()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();

        // Act
        var webView = await application.GetWebViewAsync("A");

        // Assert
        await Assertions.Expect(webView.Locator("#runtime")).ToHaveTextAsync("WebView True");
        await Assertions.Expect(webView.Locator("#context")).ToHaveTextAsync("DispatcherSynchronizationContext");
    }

    [Fact]
    public async Task Should_reach_a_component_s_script_and_the_library_s_stylesheets()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var webView = await application.GetWebViewAsync("A");

        // Act
        await webView.Locator(".forma-select-trigger-wrapper button").ClickAsync();
        await webView.GetByRole(AriaRole.Option, new() { Name = "Closed" }).ClickAsync();

        // Assert
        await Assertions.Expect(webView.Locator("#status")).ToHaveTextAsync("2");
        Assert.NotEqual(string.Empty, await webView.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).getPropertyValue('--space-1').trim()"));
    }

    [Fact]
    public async Task Should_run_another_web_view_s_events_in_a_culture_applied_to_each_web_view_s_thread()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await application.OpenTwoWebViewsAsync();

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
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await application.OpenTwoWebViewsAsync();

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
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await application.OpenTwoWebViewsAsync();

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
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await application.OpenTwoWebViewsAsync();
        await first.Locator("#apply-threads").ClickAsync();

        // Act
        await second.Locator("#amount").FillAsync("1,5");
        await second.Locator("#amount").BlurAsync();

        // Assert
        await Assertions.Expect(second.Locator("#amount-value")).ToHaveTextAsync("1.5");
    }

    [Fact]
    public async Task Should_tell_a_traversal_from_a_navigation_the_web_view_makes()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var webView = await application.GetWebViewAsync("A");
        await webView.Locator("#to-entry").ClickAsync();
        await Assertions.Expect(webView.Locator("#navigations li")).ToHaveTextAsync(["changing /en/probe?entry=2 state=none intercepted=True", "changed /en/probe?entry=2 state=none intercepted=True"]);
        var made = await webView.EvaluateAsync<string[]>("() => [...formaProbe.history]");

        // Act
        await webView.GoBackAsync();

        // Assert
        await Assertions.Expect(webView.Locator("#navigations li")).ToHaveTextAsync(
        [
            "changing /en/probe?entry=2 state=none intercepted=True",
            "changed /en/probe?entry=2 state=none intercepted=True",
            "changing /en/probe state=none intercepted=False",
            "changed /en/probe state=none intercepted=True",
        ]);
        var traversed = await webView.EvaluateAsync<string[]>("from => formaProbe.history.slice(from)", made.Length);
        Assert.Equal(["navigate push"], made);
        Assert.Contains("navigate traverse", traversed);
        Assert.Contains("popstate", traversed);
    }

    [Fact]
    public async Task Should_render_a_web_view_s_first_landing_with_no_location_change()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();

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
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await application.OpenTwoWebViewsAsync();
        await second.Locator("#close").ClickAsync();
        await first.Locator("#open").ClickAsync();
        var third = await application.GetWebViewAsync("C");

        // Act
        await third.Locator("#close-disposing").ClickAsync();

        // Assert — C's scope alone, B's kept though its native window closed before C opened
        await Assertions.Expect(first.Locator("#released")).ToHaveTextAsync("C");
    }

    [Fact]
    public async Task Should_release_a_web_view_s_scope_as_it_is_disposed()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await application.OpenTwoWebViewsAsync();

        // Act
        await second.Locator("#close-disposing").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#released")).ToHaveTextAsync("B");
        await first.GetByRole(AriaRole.Button, new() { Name = "Event" }).ClickAsync();
        await Assertions.Expect(first.Locator("#event")).ToHaveTextAsync($"{Assigned}|1,234.5|Hello");
    }
}
