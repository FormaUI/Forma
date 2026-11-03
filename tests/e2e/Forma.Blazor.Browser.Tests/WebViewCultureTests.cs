namespace Forma.Blazor.Browser.Tests;

/// <summary>The one culture a Blazor Hybrid application holds across its web views, as the culture design's Blazor Hybrid section states it, in the WebView check host's web views under culture routing.</summary>
/// <remarks>Each check starts an application of its own; its first web view, <c>A</c>, starts at <c>/en/probe</c> on the application's UI thread, which carries <c>en-US</c> as an assigned culture, and its culture routes are <c>en</c>, <c>es</c> for <c>es-ES</c> and <c>he</c>, every address carrying one.</remarks>
[Collection(WebViewChecks.Name)]
public sealed class WebViewCultureTests
{
    private const string Assigned = "en-US|en-US";
    private const string Switched = "es-ES|es-ES";

    [Fact]
    public async Task Should_start_each_web_view_s_document_in_the_application_s_culture()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();

        // Act
        var webView = await application.GetWebViewAsync("A");

        // Assert
        await Assertions.Expect(webView.Locator("#state")).ToHaveTextAsync(Assigned);
        await Assertions.Expect(webView.Locator("html")).ToHaveAttributeAsync("lang", "en-US");
        await Assertions.Expect(webView.Locator("html")).ToHaveAttributeAsync("dir", "ltr");
    }

    [Fact]
    public async Task Should_switch_the_state_every_web_view_reads_and_run_their_later_events_in_the_switched_culture()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#switched")).ToHaveTextAsync("switched");
        await Assertions.Expect(second.Locator("#state")).ToHaveTextAsync(Switched);
        await second.GetByRole(AriaRole.Button, new() { Name = "Event" }).ClickAsync();
        await Assertions.Expect(second.Locator("#event")).ToHaveTextAsync($"{Switched}|1.234,5|Hola");
    }

    [Fact]
    public async Task Should_show_a_string_of_the_host_s_own_from_files_its_application_embeds_in_the_switched_culture()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await Assertions.Expect(first.Locator("#welcome")).ToHaveTextAsync("Welcome");

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#switched")).ToHaveTextAsync("switched");
        await Assertions.Expect(first.Locator("#welcome")).ToHaveTextAsync("Bienvenido");
        await second.GetByRole(AriaRole.Button, new() { Name = "Event" }).ClickAsync();
        await Assertions.Expect(second.Locator("#welcome")).ToHaveTextAsync("Bienvenido");
        Assert.False(Directory.Exists(Path.Combine(application.Folder, ".locales")), "The check host's publish carries no folder of locale files, so its strings come from its assembly.");
    }

    [Fact]
    public async Task Should_bind_in_another_web_view_in_the_switched_culture()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await first.Locator("#switch").ClickAsync();
        await Assertions.Expect(second.Locator("#state")).ToHaveTextAsync(Switched);

        // Act
        await second.Locator("#amount").FillAsync("1,5");
        await second.Locator("#amount").BlurAsync();

        // Assert
        await Assertions.Expect(second.Locator("#amount-value")).ToHaveTextAsync("1.5");
    }

    [Fact]
    public async Task Should_set_every_web_view_s_document_in_the_switched_culture_s_language_and_direction()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await first.Locator("#culture").FillAsync("he");
        await first.Locator("#culture").BlurAsync();

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        foreach (var webView in new[] { first, second })
        {
            await Assertions.Expect(webView.Locator("html")).ToHaveAttributeAsync("lang", "he");
            await Assertions.Expect(webView.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        }
    }

    [Fact]
    public async Task Should_move_every_web_view_s_address_into_the_switched_culture_s_route_keeping_a_guarded_page_and_its_work()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await second.Locator("#guard-work").CheckAsync();
        var instance = await second.Locator("#instance").TextContentAsync();
        await second.Locator("#draft").FillAsync("kept");
        await second.Locator("#draft").BlurAsync();
        var entries = await second.EvaluateAsync<int>("() => history.length");

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#segment")).ToHaveTextAsync("es");
        await Assertions.Expect(second.Locator("#segment")).ToHaveTextAsync("es");
        await Assertions.Expect(second.Locator("#instance")).ToHaveTextAsync(instance!);
        await Assertions.Expect(second.Locator("#draft")).ToHaveValueAsync("kept");
        await Assertions.Expect(second.Locator("#navigations li")).ToHaveTextAsync(["changing /es/probe state=forma-culture intercepted=False", "changed /es/probe state=forma-culture intercepted=False"]);
        Assert.Equal(entries, await second.EvaluateAsync<int>("() => history.length"));
    }

    [Fact]
    public async Task Should_hold_a_navigation_away_from_a_guarded_page()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var webView = await application.GetWebViewAsync("A");
        await webView.Locator("#guard-work").CheckAsync();

        // Act
        await webView.Locator("#to-he").ClickAsync();

        // Assert
        await Assertions.Expect(webView.Locator("#navigations li")).ToHaveTextAsync(["changing /he/probe state=none intercepted=True", "prevented"]);
        await Assertions.Expect(webView.Locator("#segment")).ToHaveTextAsync("en");
        await Assertions.Expect(webView.Locator("#state")).ToHaveTextAsync(Assigned);
    }

    [Fact]
    public async Task Should_keep_the_address_of_a_web_view_whose_handler_refuses_to_follow_while_it_shows_the_switched_culture()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await second.Locator("#refuse").CheckAsync();

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#segment")).ToHaveTextAsync("es");
        await Assertions.Expect(second.Locator("#navigations li")).ToHaveTextAsync(["changing /es/probe state=forma-culture intercepted=False", "prevented"]);
        await Assertions.Expect(second.Locator("#segment")).ToHaveTextAsync("en");
        await Assertions.Expect(second.Locator("#state")).ToHaveTextAsync(Switched);
        await Assertions.Expect(second.Locator("html")).ToHaveAttributeAsync("lang", "es-ES");
    }

    [Fact]
    public async Task Should_switch_the_application_as_a_web_view_navigates_to_another_culture_s_address()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await first.Locator("#to-he").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#segment")).ToHaveTextAsync("he");
        await Assertions.Expect(first.Locator("#state")).ToHaveTextAsync("he|he");
        await Assertions.Expect(second.Locator("#segment")).ToHaveTextAsync("he");
        await Assertions.Expect(second.Locator("html")).ToHaveAttributeAsync("lang", "he");
        await Assertions.Expect(second.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
    }

    [Fact]
    public async Task Should_return_to_an_entry_s_page_in_the_application_s_culture_on_a_traversal()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var webView = await application.GetWebViewAsync("A");
        await webView.Locator("#to-es").ClickAsync();
        await Assertions.Expect(webView.Locator("#state")).ToHaveTextAsync(Switched);

        // Act
        await webView.GoBackAsync();

        // Assert
        await Assertions.Expect(webView.Locator("#navigations li")).ToHaveTextAsync(
        [
            "changing /es/probe state=none intercepted=True",
            "changed /es/probe state=none intercepted=True",
            "changing /en/probe state=none intercepted=False",
            "changed /en/probe state=none intercepted=True",
            "changing /es/probe state=forma-culture intercepted=False",
            "changed /es/probe state=forma-culture intercepted=False",
        ]);
        await Assertions.Expect(webView.Locator("#segment")).ToHaveTextAsync("es");
        await Assertions.Expect(webView.Locator("#state")).ToHaveTextAsync(Switched);
    }

    [Fact]
    public async Task Should_start_a_web_view_opened_at_another_culture_s_address_in_the_application_s_culture()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var first = await application.GetWebViewAsync("A");
        await first.Locator("#switch").ClickAsync();
        await Assertions.Expect(first.Locator("#segment")).ToHaveTextAsync("es");

        // Act
        await first.Locator("#open").ClickAsync();
        var second = await application.GetWebViewAsync("B");

        // Assert
        await Assertions.Expect(second.Locator("#segment")).ToHaveTextAsync("es");
        await Assertions.Expect(second.Locator("html")).ToHaveAttributeAsync("lang", "es-ES");
        await Assertions.Expect(second.Locator("#render-culture")).ToHaveTextAsync(Switched);
        await Assertions.Expect(first.Locator("#state")).ToHaveTextAsync(Switched);
    }

    [Fact]
    public async Task Should_finish_work_begun_before_a_switch_in_the_culture_it_captured()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await second.Locator("#begin-work").ClickAsync();
        await Assertions.Expect(second.Locator("#work")).ToHaveTextAsync($"begun {Assigned}");

        // Act
        await first.Locator("#switch").ClickAsync();
        await Assertions.Expect(second.Locator("#state")).ToHaveTextAsync(Switched);
        await second.Locator("#end-work").ClickAsync();

        // Assert
        await Assertions.Expect(second.Locator("#work")).ToHaveTextAsync($"begun {Assigned}, ended {Assigned}");
    }

    [Fact]
    public async Task Should_switch_every_web_view_from_the_application_s_native_code()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await first.Locator("#switch-native").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#switched")).ToHaveTextAsync("switched from native code");
        await Assertions.Expect(second.Locator("#segment")).ToHaveTextAsync("es");
        await second.GetByRole(AriaRole.Button, new() { Name = "Event" }).ClickAsync();
        await Assertions.Expect(second.Locator("#event")).ToHaveTextAsync($"{Switched}|1.234,5|Hola");
    }

    [Theory]
    [InlineData("forma-first")]
    [InlineData("platform-first")]
    public async Task Should_switch_every_web_view_whichever_registers_first_Forma_or_the_platform(string order)
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync(new Dictionary<string, string> { ["FORMA_WEBVIEW_ORDER"] = order });
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        await Assertions.Expect(second.Locator("#state")).ToHaveTextAsync(Switched);
        await Assertions.Expect(second.Locator("#segment")).ToHaveTextAsync("es");
    }

    [Fact]
    public async Task Should_keep_switching_once_a_web_view_s_native_window_closed_without_its_web_view_disposed()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await second.Locator("#close").ClickAsync();
        await first.Locator("#switch").ClickAsync();
        await Assertions.Expect(first.Locator("#switched")).ToHaveTextAsync("switched");
        await first.Locator("#culture").FillAsync("he");
        await first.Locator("#culture").BlurAsync();

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("html")).ToHaveAttributeAsync("lang", "he");
        await Assertions.Expect(first.Locator("#segment")).ToHaveTextAsync("he");
    }

    [Fact]
    public async Task Should_keep_switching_once_a_web_view_was_disposed()
    {
        // Arrange
        await using var application = await WebViewCheckHost.StartAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);
        await second.Locator("#close-disposing").ClickAsync();
        await Assertions.Expect(first.Locator("#released")).ToHaveTextAsync("1");

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#switched")).ToHaveTextAsync("switched");
        await Assertions.Expect(first.Locator("html")).ToHaveAttributeAsync("lang", "es-ES");
    }

    [Fact]
    public async Task Should_switch_one_web_view_alone_under_a_culture_per_web_view_leaving_the_ambient_culture_to_the_host()
    {
        // Arrange
        await using var application = await StartWithACulturePerWebViewAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await first.Locator("#switch").ClickAsync();

        // Assert
        await Assertions.Expect(first.Locator("#switched")).ToHaveTextAsync("switched");
        await Assertions.Expect(first.Locator("#state")).ToHaveTextAsync(Switched);
        await Assertions.Expect(first.Locator("#segment")).ToHaveTextAsync("es");
        await Assertions.Expect(first.Locator("html")).ToHaveAttributeAsync("lang", "es-ES");
        await Assertions.Expect(first.Locator("#welcome")).ToHaveTextAsync("Bienvenido");
        await first.GetByRole(AriaRole.Button, new() { Name = "Event" }).ClickAsync();
        await Assertions.Expect(first.Locator("#event")).ToHaveTextAsync($"{Assigned}|1,234.5|Hello");
        await Assertions.Expect(second.Locator("#state")).ToHaveTextAsync(Assigned);
        await Assertions.Expect(second.Locator("#segment")).ToHaveTextAsync("en");
        await Assertions.Expect(second.Locator("html")).ToHaveAttributeAsync("lang", "en-US");
        await Assertions.Expect(second.Locator("#welcome")).ToHaveTextAsync("Welcome");
    }

    [Fact]
    public async Task Should_switch_a_web_view_alone_as_it_lands_at_another_culture_s_address_under_a_culture_per_web_view()
    {
        // Arrange
        await using var application = await StartWithACulturePerWebViewAsync();
        var (first, second) = await OpenTwoWebViewsAsync(application);

        // Act
        await second.Locator("#to-he").ClickAsync();

        // Assert
        await Assertions.Expect(second.Locator("#state")).ToHaveTextAsync("he|he");
        await Assertions.Expect(second.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Assertions.Expect(first.Locator("#state")).ToHaveTextAsync(Assigned);
        await Assertions.Expect(first.Locator("html")).ToHaveAttributeAsync("dir", "ltr");
    }

    [Fact]
    public async Task Should_open_a_web_view_in_the_culture_its_start_path_names_under_a_culture_per_web_view()
    {
        // Arrange
        await using var application = await StartWithACulturePerWebViewAsync();
        var first = await application.GetWebViewAsync("A");
        await first.Locator("#start-path").FillAsync("/es/probe");
        await first.Locator("#start-path").BlurAsync();

        // Act
        await first.Locator("#open").ClickAsync();

        // Assert
        var second = await application.GetWebViewAsync("B");
        await Assertions.Expect(second.Locator("#state")).ToHaveTextAsync(Switched);
        await Assertions.Expect(second.Locator("html")).ToHaveAttributeAsync("lang", "es-ES");
        await Assertions.Expect(first.Locator("#state")).ToHaveTextAsync(Assigned);
    }

    private static Task<WebViewApplication> StartWithACulturePerWebViewAsync()
        => WebViewCheckHost.StartAsync(new Dictionary<string, string> { ["FORMA_WEBVIEW_CULTURE"] = "per-web-view" });

    private static async Task<(IPage First, IPage Second)> OpenTwoWebViewsAsync(WebViewApplication application)
    {
        var first = await application.GetWebViewAsync("A");
        await first.Locator("#open").ClickAsync();
        var second = await application.GetWebViewAsync("B");

        return (first, second);
    }
}
