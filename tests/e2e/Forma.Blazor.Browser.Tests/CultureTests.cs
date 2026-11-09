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
        var (context, page) = await host.OpenPageAsync(await GetAddressAsync(routing));
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

    [Theory]
    [MemberData(nameof(Routing))]
    public async Task Should_switch_by_address_in_place_through_the_culture_switch(string routing)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(await GetAddressAsync(routing));
        await using var _ = context;
        await page.GotoAsync("culture");
        await WaitForPanelAsync(page);
        await page.Locator("#typed").FillAsync("Ada");

        // Act
        await page.Locator("#switch-state").ClickAsync();

        // Assert
        await WaitForDocumentAsync(page, "he/rtl");
        await Assertions.Expect(page.Locator("#switch-outcome")).ToHaveTextAsync("accepted");
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync("he");
        var typed = await page.Locator("#typed").InputValueAsync();
        Assert.Multiple(
            () => Assert.EndsWith("/he/culture", new Uri(page.Url).AbsolutePath, StringComparison.Ordinal),
            () => Assert.Equal("Ada", typed));
    }

    [Fact]
    public async Task Should_time_out_a_switch_whose_navigation_a_guard_refuses_leaving_the_culture_as_it_was()
    {
        // Arrange — interactive routing, whose router tells nothing of a navigation a handler refuses
        var (context, page) = await host.OpenPageAsync(await host.GetInteractiveAddressAsync());
        await using var _ = context;
        await page.GotoAsync("culture");
        await WaitForPanelAsync(page);
        await page.Locator("#guard-refuse").ClickAsync();
        await Assertions.Expect(page.Locator("#guard-state")).ToHaveTextAsync("refusing");

        // Act
        await page.Locator("#switch-state").ClickAsync();

        // Assert — once the host's switch timeout has passed
        await Assertions.Expect(page.Locator("#switch-outcome")).ToHaveTextAsync("timed out", new() { Timeout = 30000 });
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync("en");
        Assert.EndsWith("/culture", new Uri(page.Url).AbsolutePath, StringComparison.Ordinal);
        Assert.DoesNotContain("/he/", new Uri(page.Url).AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_complete_a_switch_whose_navigation_a_guard_holds_once_the_guard_lets_it_go()
    {
        // Arrange — a guard asking first, as a handler asking the reader does
        var (context, page) = await host.OpenPageAsync(await host.GetInteractiveAddressAsync());
        await using var _ = context;
        await page.GotoAsync("culture");
        await WaitForPanelAsync(page);
        await page.Locator("#guard-hold").ClickAsync();
        await Assertions.Expect(page.Locator("#guard-state")).ToHaveTextAsync("holding");
        await page.Locator("#switch-state").ClickAsync();
        await Assertions.Expect(page.Locator("#guard-state")).ToHaveTextAsync("held");

        // Act
        await page.Locator("#guard-release").ClickAsync();

        // Assert
        await Assertions.Expect(page.Locator("#switch-outcome")).ToHaveTextAsync("accepted");
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync("he");
        Assert.EndsWith("/he/culture", new Uri(page.Url).AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_take_a_switch_made_after_one_a_guard_refused_without_waiting_for_it()
    {
        // Arrange — a switch a guard refused, still waiting for a landing that never comes
        var (context, page) = await host.OpenPageAsync(await host.GetInteractiveAddressAsync());
        await using var _ = context;
        await page.GotoAsync("culture");
        await WaitForPanelAsync(page);
        await page.Locator("#guard-refuse").ClickAsync();
        await Assertions.Expect(page.Locator("#guard-state")).ToHaveTextAsync("refusing");
        await page.Locator("#switch-state").ClickAsync();
        await page.Locator("#guard-end").ClickAsync();
        await Assertions.Expect(page.Locator("#guard-state")).ToHaveTextAsync("none");

        // Act
        await page.Locator("#switch-state").ClickAsync();

        // Assert — the first, still waiting, gave way to the second, which landed
        await Assertions.Expect(page.Locator("#switch-outcomes")).ToHaveTextAsync("canceled accepted");
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync("he");
        Assert.EndsWith("/he/culture", new Uri(page.Url).AbsolutePath, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Routing))]
    public async Task Should_refuse_a_pair_no_route_carries_before_anything_changes(string routing)
    {
        // Arrange
        var (context, page) = await host.OpenPageAsync(await GetAddressAsync(routing));
        await using var _ = context;
        await page.GotoAsync("culture");
        await WaitForPanelAsync(page);
        await WaitForDocumentAsync(page, "en/ltr");

        // Act
        await page.Locator("#switch-unrouted").ClickAsync();

        // Assert
        await Assertions.Expect(page.Locator("#switch-outcome")).ToHaveTextAsync("refused");
        await Assertions.Expect(page.Locator("#state")).ToHaveTextAsync("en");
        var document = await GetDocumentAsync(page);
        Assert.Multiple(
            () => Assert.EndsWith("/culture", new Uri(page.Url).AbsolutePath, StringComparison.Ordinal),
            () => Assert.DoesNotContain("/he/", new Uri(page.Url).AbsolutePath, StringComparison.Ordinal),
            () => Assert.Equal("en/ltr", document));
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
        var (context, page) = await host.OpenPageAsync(await GetAddressAsync(routing));
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
        var stateAddress = await host.GetStateAddressAsync();
        var (context, page) = await host.OpenPageAsync(stateAddress);
        await using var _ = context;
        await page.GotoAsync("state");
        await WaitForPanelAsync(page);
        var served = await GetDocumentAsync(page);
        var servedState = await page.Locator("#state").TextContentAsync();
        var settled = await host.GetDelayedSwitchesAsync(stateAddress);
        var failed = await host.GetDelayedSwitchFailuresAsync(stateAddress);
        await page.Locator("#switch-later").ClickAsync();
        await Assertions.Expect(page.Locator("#held-switch")).ToHaveTextAsync("held");
        await context.SetOfflineAsync(true);

        // Act — the server switches while the browser is offline, and the switch settles once the browser is back: accepted
        // where the coordinator was reached, failed where the circuit lost its connection
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        await host.ReleaseDelayedSwitchAsync(stateAddress, timeout.Token);
        await context.SetOfflineAsync(false);
        await host.WaitForDelayedSwitchAsync(stateAddress, settled, timeout.Token);

        // Assert
        var isFailed = await host.GetDelayedSwitchFailuresAsync(stateAddress) > failed;
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
        var (context, page) = await host.OpenPageAsync(await (isInvariant ? host.GetInvariantStateAddressAsync() : host.GetStateAddressAsync()));
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
        var (context, page) = await host.OpenPageAsync(await host.GetStateAddressAsync());
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

    private Task<Uri> GetAddressAsync(string routing)
        => routing == "interactive" ? host.GetInteractiveAddressAsync() : host.GetAddressAsync();
}
