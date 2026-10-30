namespace Forma.Blazor.Browser.Tests;

using static Forma.Blazor.Browser.Tests.CulturePages;

/// <summary>The culture's behavior on the Server: a switch, the document following it through navigations and updates, and the culture in the address.</summary>
public sealed class CultureTests(BrowserHost host)
{
    /// <summary>The two ways the host routes its pages: islands under a static router, and an interactive router.</summary>
    public static TheoryData<string> Routing { get; } = ["islands", "interactive"];

    [Theory]
    [MemberData(nameof(Routing))]
    public async Task Should_follow_a_switch_to_another_culture_in_place(string routing)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(GetAddress(routing));
        await using var _ = context;
        await page.GotoAsync("culture");
        await WaitForPanelAsync(page);
        await page.Locator("#typed").FillAsync("Ada");

        // Act
        await page.Locator("#lang-he").ClickAsync();

        // Assert
        await WaitForDocumentAsync(page, "he/rtl");
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync("he");
        var typed = await page.Locator("#typed").InputValueAsync();
        Assert.Multiple(
            () => Assert.EndsWith("/he/culture", new Uri(page.Url).AbsolutePath, StringComparison.Ordinal),
            () => Assert.Equal("Ada", typed));
    }

    [Fact]
    public async Task Should_keep_the_document_through_an_enhanced_navigation_within_the_culture()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        await page.GotoAsync("he/culture");
        await WaitForPanelAsync(page);
        await WaitForDocumentAsync(page, "he/rtl");

        // Act
        await page.Locator("#go-other").ClickAsync();
        await Assertions.Expect(page.Locator("#page")).ToHaveTextAsync("other");

        // Assert
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync("he");
        Assert.Equal("he/rtl", await GetDocumentAsync(page));
    }

    [Fact]
    public async Task Should_keep_the_document_through_a_streaming_update()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        await page.GotoAsync("he/culture");
        await WaitForPanelAsync(page);

        // Act
        await page.Locator("#go-streaming").ClickAsync();
        await Assertions.Expect(page.Locator("#streamed")).ToBeVisibleAsync();

        // Assert
        Assert.Equal("he/rtl", await GetDocumentAsync(page));
    }

    [Theory]
    [MemberData(nameof(Routing))]
    public async Task Should_end_rapid_switches_on_the_last(string routing)
    {
        // Arrange — each switch is a navigation the page reports, so the last one's landing is known
        var (context, page) = await host.OpenPageAsync(GetAddress(routing));
        await using var _ = context;
        await page.GotoAsync("culture");
        await WaitForPanelAsync(page);
        var navigations = 0;
        page.FrameNavigated += (_, frame) =>
        {
            if (frame == page.MainFrame)
            {
                Interlocked.Increment(ref navigations);
            }
        };

        // Act
        foreach (var token in new[] { "he", "en", "he", "en", "he" })
        {
            await page.Locator($"#lang-{token}").ClickAsync();
        }

        // Assert
        await page.WaitForFunctionAsync("() => location.pathname.endsWith('/he/culture')");
        await WaitForDocumentAsync(page, "he/rtl");
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync("he");
        Assert.True(Volatile.Read(ref navigations) >= 5, $"{navigations} navigations");
    }

    [Fact]
    public async Task Should_leave_the_document_and_the_state_on_one_pair_after_a_switch_made_while_the_browser_is_offline()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(host.StateAddress);
        await using var _ = context;
        await page.GotoAsync("state");
        await WaitForPanelAsync(page);
        var served = await GetDocumentAsync(page);
        var servedState = await page.Locator("#state").TextContentAsync();
        var settled = await host.GetDelayedSwitchesAsync(host.StateAddress);
        var failed = await host.GetDelayedSwitchFailuresAsync(host.StateAddress);
        await page.Locator("#switch-later").ClickAsync();
        await context.SetOfflineAsync(true);

        // Act — the server switches while the browser is offline, and the switch settles once the browser is back: accepted
        // where the coordinator was reached, failed where the circuit lost its connection
        await Task.Delay(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);
        await context.SetOfflineAsync(false);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (await host.GetDelayedSwitchesAsync(host.StateAddress) == settled)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), timeout.Token);
        }

        // Assert
        var isFailed = await host.GetDelayedSwitchFailuresAsync(host.StateAddress) > failed;
        TestContext.Current.TestOutputHelper?.WriteLine($"the switch {(isFailed ? "failed" : "was accepted")}");
        await WaitForDocumentAsync(page, isFailed ? served : "he/rtl", timeout: 60000);
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync(isFailed ? servedState! : "he");
    }

    [Fact]
    public async Task Should_follow_the_culture_after_a_pause_and_a_resume()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync();
        await using var _ = context;
        await page.GotoAsync("he/culture");
        await WaitForPanelAsync(page);

        // Act
        await page.EvaluateAsync("() => Blazor.pauseCircuit()");
        await page.EvaluateAsync("() => Blazor.resumeCircuit()");
        await WaitForPanelAsync(page);
        await page.Locator("#lang-en").ClickAsync();

        // Assert
        await WaitForDocumentAsync(page, "en/ltr");
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync("en");
    }

    /// <summary>The cultures a host switching the state alone renders every request in, named and invariant.</summary>
    public static TheoryData<string> RequestCultures { get; } = ["en-US", "invariant"];

    [Theory]
    [MemberData(nameof(RequestCultures))]
    public async Task Should_register_from_the_marker_of_the_culture_the_page_was_rendered_in_and_switch(string requestCulture)
    {
        // Arrange — a host that renders every request in the culture named, whatever the runner's
        var isInvariant = requestCulture == "invariant";
        var (context, page) = await host.OpenPageAsync(isInvariant ? host.InvariantStateAddress : host.StateAddress);
        await using var _ = context;
        var errors = new List<string>();
        page.Console += (_, message) => errors.Add(message.Text);
        await page.GotoAsync("state");
        await WaitForPanelAsync(page);
        var name = isInvariant ? string.Empty : requestCulture;
        var marker = page.Locator("meta[name='forma-culture']");
        var published = $"{await marker.GetAttributeAsync("data-culture")}|{await marker.GetAttributeAsync("data-ui-culture")}";
        var started = await page.Locator("#state").TextContentAsync();

        // Act
        await page.Locator("#switch-state").ClickAsync();

        // Assert — the switch is accepted only where the circuit registered with the page's coordinator
        Assert.Equal($"{name}|{name}", published);
        Assert.Equal(name, started);
        await WaitForDocumentAsync(page, "he/rtl");
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync("he");
        Assert.DoesNotContain(errors, error => error.Contains("culture marker", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_show_the_served_pair_once_the_circuit_closes()
    {
        // Arrange — the state switched alone, with the server still serving English
        var (context, page) = await host.OpenPageAsync(host.StateAddress);
        await using var _ = context;
        await page.GotoAsync("state");
        await WaitForPanelAsync(page);
        var served = await GetDocumentAsync(page);
        await page.Locator("#switch-state").ClickAsync();
        await WaitForDocumentAsync(page, "he/rtl");

        // Act — a page without islands, whose circuit the client closes once nothing on the page needs it
        await page.Locator("#go-state-static").ClickAsync();
        await Assertions.Expect(page.Locator("#page")).ToHaveTextAsync("state static");

        // Assert
        Assert.NotEqual("he/rtl", served);
        await WaitForDocumentAsync(page, served, timeout: 60000);
    }

    private Uri GetAddress(string routing)
        => routing == "interactive" ? host.InteractiveAddress : host.Address;
}
