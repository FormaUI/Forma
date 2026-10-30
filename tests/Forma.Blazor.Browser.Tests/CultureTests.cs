namespace Forma.Blazor.Browser.Tests;

using static Forma.Blazor.Browser.Tests.CulturePages;

/// <summary>Today's culture behavior on the Server, the culture plan's checks made by hand in Chrome, as regression checks the coordinator's steps are measured against.</summary>
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
    public async Task Should_follow_a_switch_that_lands_while_the_connection_is_down()
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(host.StateAddress);
        await using var _ = context;
        await page.GotoAsync("state");
        await WaitForPanelAsync(page);
        var landed = await host.GetDelayedSwitchesAsync(host.StateAddress);
        await page.Locator("#switch-later").ClickAsync();
        await context.SetOfflineAsync(true);

        // Act — the server switches while the browser is offline, which the check learns of past the browser
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (await host.GetDelayedSwitchesAsync(host.StateAddress) == landed)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), timeout.Token);
        }

        var whileOffline = await GetDocumentAsync(page);
        await context.SetOfflineAsync(false);

        // Assert
        await WaitForDocumentAsync(page, "he/rtl", timeout: 60000);
        TestContext.Current.TestOutputHelper?.WriteLine($"the document while offline: {whileOffline}");
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
