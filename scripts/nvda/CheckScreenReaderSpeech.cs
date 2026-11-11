#!/usr/bin/env dotnet
#:package Microsoft.Playwright@1.*
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

// Drives the Showcase in Playwright's Chromium while NVDA runs, and checks that each scenario step is
// followed by the utterances it expects, in order, in the speech the recorder writes. Arguments: the
// Showcase's address, the recorder's file, and the transcript to write; --show slows the browser and holds
// each step on screen. --prepare-browser installs the Chromium it drives, the step the script takes before
// each run. Windows only, since NVDA is.
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Playwright;

if (!OperatingSystem.IsWindows())
{
    await Console.Error.WriteLineAsync("NVDA runs on Windows alone, so the screen reader check does too.").ConfigureAwait(false);
    return 4;
}

if (args.Contains("--prepare-browser"))
{
    return Microsoft.Playwright.Program.Main(["install", "chromium"]);
}

var positionalArguments = args.Where(argument => !argument.StartsWith("--", StringComparison.Ordinal)).ToArray();
var show = args.Contains("--show");

if (positionalArguments.Length < 3)
{
    await Console.Error.WriteLineAsync("Usage: CheckScreenReaderSpeech <showcase address> <speech log> <transcript>").ConfigureAwait(false);
    return 2;
}

var baseUrl = positionalArguments[0].TrimEnd('/');
var speech = new SpeechLog(positionalArguments[1]);
var transcript = new StringBuilder();

Scenario[] scenarios =
[
    new("The slow source's first load, its second page and a refresh", "en", "/data-view/states", SlowSource("Loading", "9 items")),
    new("The slow source's first load, its second page and a refresh", "he", "/he/data-view/states", SlowSource("טוען", "9 פריטים")),
    new("The slow tree's first load", "en", "/data-view/states", [new("open the page", null, ["Loading", "3 items"])]),
    new("The slow tree's first load", "he", "/he/data-view/states", [new("open the page", null, ["טוען", "3 פריטים"])]),
    new("The last page's only item taken away and everything put back", "en", "/data-view/states", PageGivingWay("8 items", "9 items")),
    new("The last page's only item taken away and everything put back", "he", "/he/data-view/states", PageGivingWay("8 פריטים", "9 פריטים")),
    new("A fourth box pressed in a bounded selection", "en", "/data-view/states",
        BoundedSelection("3 of at most 3 selected", "Refused", "at most 3 may be selected")),
    new("A fourth box pressed in a bounded selection", "he", "/he/data-view/states",
        BoundedSelection("נבחרו 3 מתוך 3 לכל היותר", "נדחה", "ניתן לבחור 3 לכל היותר")),
    new("The tree's focus kept while it scrolls", "en", "/data-view/tree", TreeScrolling(rightToLeft: false)),
    new("The tree's focus kept while it scrolls", "he", "/he/data-view/tree", TreeScrolling(rightToLeft: true)),
    new("The tree's focus handed on when its node goes away", "en", "/data-view/tree", TreeHandOff(rightToLeft: false)),
    new("The tree's focus handed on when its node goes away", "he", "/he/data-view/tree", TreeHandOff(rightToLeft: true)),
    new("A checkbox and its state", "en", "/checkbox", CheckBoxState("Subscribe to updates")),
    new("A checkbox and its state", "he", "/he/checkbox", CheckBoxState("הרשמה לעדכונים")),
    new("A time picker and its list", "en", "/time-picker", TimePickerList("Starts", "12:00 AM", "12:30 AM")),
    new("A time picker and its list", "he", "/he/time-picker", TimePickerList("מתחיל", "0:00", "0:30")),
    new("A date and a time under one label", "en", "/date-time-picker", DateTimePickerNames("Starts", "Date", "Time")),
    new("A date and a time under one label", "he", "/he/date-time-picker", DateTimePickerNames("מתחיל", "תאריך", "שעה")),
    new("A currency input and its currency", "en", "/currency-input", CurrencyInputCurrency("Price")),
    new("A currency input and its currency", "he", "/he/currency-input", CurrencyInputCurrency("מחיר")),
];

using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);

IBrowser browser;

try
{
    browser = await playwright.Chromium.LaunchAsync(new() { Headless = false, SlowMo = show ? 500 : 0 }).ConfigureAwait(false);
}
catch (PlaywrightException exception)
{
    await Console.Error.WriteLineAsync($"Chromium would not start: {exception.Message.Split('\n')[0]}").ConfigureAwait(false);
    return 3;
}

await using var browserDisposal = browser.ConfigureAwait(false);

var failureCount = 0;

foreach (var scenario in scenarios)
{
    transcript.AppendLine(CultureInfo.InvariantCulture, $"# {scenario.Name} ({scenario.Culture})");

    var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 1000 } }).ConfigureAwait(false);
    await page.BringToFrontAsync().ConfigureAwait(false);

    foreach (var step in scenario.Steps)
    {
        var mark = speech.Count;

        try
        {
            if (step.Act is null)
            {
                await OpenAsync(page, baseUrl + scenario.Path).ConfigureAwait(false);
            }
            else
            {
                await step.Act(page).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is PlaywrightException or TimeoutException)
        {
            transcript.AppendLine(CultureInfo.InvariantCulture, $"## {step.Description}: the action failed: {exception.Message.Split('\n')[0]}");
            failureCount++;
            break;
        }

        var outcome = await speech.WaitForAsync(mark, step.Expect, step.Timeout ?? TimeSpan.FromSeconds(10)).ConfigureAwait(false);

        transcript.AppendLine(CultureInfo.InvariantCulture, $"## {step.Description}: {(outcome.MissingUtterance is null ? "passed" : $"missing \"{outcome.MissingUtterance}\"")}");
        transcript.AppendLine(CultureInfo.InvariantCulture, $"Expected, in order: {string.Join(" | ", step.Expect)}");

        foreach (var utterance in outcome.HeardUtterances)
        {
            transcript.AppendLine(CultureInfo.InvariantCulture, $"  {utterance.Sequence,4}  {utterance.Text}");
        }

        if (outcome.MissingUtterance is not null)
        {
            failureCount++;
        }

        if (show)
        {
            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        }
    }

    await page.CloseAsync().ConfigureAwait(false);
}

await File.WriteAllTextAsync(positionalArguments[2], transcript.ToString()).ConfigureAwait(false);
await Console.Out.WriteAsync(transcript.ToString()).ConfigureAwait(false);

return failureCount == 0 ? 0 : 1;

// The states tab's slow source, three items to a page: its first load, a move to its second page and a refresh, each spoken as loading
// and then the count.
static Step[] SlowSource(string loading, string count)
{
    return [
        new("open the page", null, [loading, count]),
        new("move to the second page", page => PressNextPageAsync(page, 1), [loading, count]),
        new("refresh", page => PressControlAsync(page, 1, 0), [loading, count]),
    ];
}

// The states tab's shelf, four items to a page and nine items: its last page's only item taken away, and everything put back.
static Step[] PageGivingWay(string fewer, string all)
{
    return [
        new("open the page", null, []),
        new("go to the last page", page => page.Locator(".demo-section").Nth(3).Locator(".forma-pagination-navigation-step")
            .GetByText("3", new() { Exact = true }).EvaluateAsync("step => step.click()"), []),
        new("take its only item away", page => PressControlAsync(page, 3, 0), [fewer]),
        new("put everything back", page => PressControlAsync(page, 3, 1), [all]),
    ];
}

// The states tab's bounded selection, one product checked and three at most: two more checked, then a fourth box pressed.
static Step[] BoundedSelection(string full, string refused, string limit)
{
    return [
        new("open the page", null, []),
        new("check two more products", async page =>
        {
            await PressBoxAsync(page, 4, 1).ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
            await PressBoxAsync(page, 4, 2).ConfigureAwait(false);
        }, [full]),
        new("press a fourth box", page => PressBoxAsync(page, 4, 3), [refused, limit]),
    ];
}

// The archive of a hundred thousand nodes: a node focused, scrolled out of view, and the keys carrying on from it. A name is
// expected as NVDA speaks it, its full stop read as "dot".
static Step[] TreeScrolling(bool rightToLeft)
{
    return [
        new("open the page", null, []),
        new("focus the first archive", page => TreeNode(page, 3, "Archive 1").FocusAsync(), ["Archive 1"]),
        new("open it", page => page.Keyboard.PressAsync(rightToLeft ? "ArrowLeft" : "ArrowRight"), ["expanded"]),
        new("move to the third scan", page => PressAsync(page, "ArrowDown", 3), ["Scan 1-00003 dot jpg"]),
        new("scroll it out of view, then move on", async page =>
        {
            await page.Locator(".demo-section").Nth(3).Locator(".forma-data-tree").EvaluateAsync("tree => tree.scrollTop = 60000").ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
            await page.Keyboard.PressAsync("ArrowDown").ConfigureAwait(false);
        }, ["Scan 1-00004 dot jpg"]),
        new("go to the last node", page => page.Keyboard.PressAsync("End"), ["Archive 10"]),
    ];
}

// The tree whose source changes under it: the node holding the focus taken away, and an ancestor closed over it.
static Step[] TreeHandOff(bool rightToLeft)
{
    return [
        new("open the page", null, []),
        new("focus Documents", page => TreeNode(page, 5, "Documents").FocusAsync(), ["Documents"]),
        new("open it", page => page.Keyboard.PressAsync(rightToLeft ? "ArrowLeft" : "ArrowRight"), ["expanded"]),
        new("move to Reports", page => page.Keyboard.PressAsync("ArrowDown"), ["Reports"]),
        new("take Reports away while it holds the focus", page => PressControlAsync(page, 5, 1), ["Contracts"], TimeSpan.FromSeconds(8)),
        new("put everything back, and move to Reports", async page =>
        {
            await PressControlAsync(page, 5, 6).ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
            await page.Keyboard.PressAsync("ArrowUp").ConfigureAwait(false);
        }, ["Reports"]),
        new("open it", page => page.Keyboard.PressAsync(rightToLeft ? "ArrowLeft" : "ArrowRight"), ["expanded"]),
        new("move to Q1.pdf", page => page.Keyboard.PressAsync("ArrowDown"), ["Q 1 dot pdf"]),
        new("close Documents over the focus", page => PressControlAsync(page, 5, 4), ["Documents"], TimeSpan.FromSeconds(8)),
    ];
}

// The checkbox named by its label: announced as one, then its state as Space turns it on and off.
static Step[] CheckBoxState(string label)
{
    return [
        new("open the page", null, []),
        new("focus the box", page => page.Locator("label.forma-checkbox-label").Filter(new() { HasTextString = label }).Locator("input").FocusAsync(), [label, "check box", "not checked"]),
        new("check it", page => page.Keyboard.PressAsync("Space"), ["checked"]),
        new("clear it", page => page.Keyboard.PressAsync("Space"), ["not checked"]),
    ];
}

// The first time picker on its page: the field named and announced as a combobox, and the list's first two suggestions announced as
// the down arrow highlights them.
static Step[] TimePickerList(string label, string firstSuggestion, string secondSuggestion)
{
    return [
        new("open the page", null, []),
        new("focus the field", page => page.Locator(".forma-time-box input").First.FocusAsync(), [label, "combo box"]),
        new("open the list", page => page.Keyboard.PressAsync("ArrowDown"), [firstSuggestion, "1 of 48"]),
        new("move down the list", page => page.Keyboard.PressAsync("ArrowDown"), [secondSuggestion, "2 of 48"]),
    ];
}

static Step[] DateTimePickerNames(string label, string date, string time)
{
    return [
        new("open the page", null, []),
        new("focus the date", page => page.Locator(".forma-date-time-picker .forma-date-box-trigger").First.FocusAsync(), [label, "grouping", date, "button"]),
        new("move to the time", page => page.Keyboard.PressAsync("Tab"), [time, "combo box"]),
    ];
}

static Step[] CurrencyInputCurrency(string label)
{
    return [
        new("open the page", null, []),
        new("focus the amount", page => page.Locator(".forma-currency-input").First.FocusAsync(), [label, "spin button", "USD"]),
    ];
}

// Opens a page once it is interactive. A page is drawn first on the server and drawn again once its circuit starts, which throws
// away what it drew first, the element a step focused with it, so the page is ready once its heading has been drawn again; a page
// that is never drawn again is taken as ready once the wait runs out.
static async Task OpenAsync(IPage page, string url)
{
    await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);

    if (await page.QuerySelectorAsync("h1").ConfigureAwait(false) is not { } heading)
    {
        return;
    }

    try
    {
        await page.WaitForFunctionAsync("heading => !heading.isConnected", heading, new() { Timeout = 5000 }).ConfigureAwait(false);
    }
    catch (TimeoutException)
    {
    }
}

// A node of the tree in a section of the tree tab, by the section's place on the page.
static ILocator TreeNode(IPage page, int section, string name)
{
    return page.Locator(".demo-section").Nth(section).Locator(".forma-data-tree-node")
        .Filter(new() { Has = page.Locator(".forma-data-tree-node-name").GetByText(name, new() { Exact = true }) });
}

static async Task PressAsync(IPage page, string key, int pressCount)
{
    for (var press = 0; press < pressCount; press++)
    {
        await page.Keyboard.PressAsync(key).ConfigureAwait(false);
    }
}

// Presses a section's control without taking the focus to it, as a change arriving from elsewhere would.
static Task PressControlAsync(IPage page, int section, int control)
{
    return page.Locator(".demo-section").Nth(section).Locator(".demo-controls button").Nth(control).EvaluateAsync("button => button.click()");
}

// Presses a section's Next page without taking the focus to it.
static Task PressNextPageAsync(IPage page, int section)
{
    return page.Locator(".demo-section").Nth(section).Locator(".forma-pagination-navigation-button-next").First.EvaluateAsync("button => button.click()");
}

// Presses the box of a section's list item without taking the focus to it.
static Task PressBoxAsync(IPage page, int section, int item)
{
    return page.Locator(".demo-section").Nth(section).Locator(".forma-data-list-item-lead input").Nth(item).EvaluateAsync("box => box.click()");
}

/// <summary>A page in a culture, and the steps taken on it.</summary>
internal sealed record Scenario(string Name, string Culture, string Path, Step[] Steps);

/// <summary>An action, and the utterances it must be followed by, in order.</summary>
/// <remarks>A step with no action opens the scenario's page.</remarks>
internal sealed record Step(string Description, Func<IPage, Task>? Act, string[] Expect, TimeSpan? Timeout = null);

/// <summary>One utterance the recorder wrote.</summary>
internal sealed record Utterance(int Sequence, string Text);

/// <summary>What a step heard, and the first expected utterance it did not.</summary>
internal sealed record Outcome(IReadOnlyList<Utterance> HeardUtterances, string? MissingUtterance);

/// <summary>The recorder's file, read from a mark onward.</summary>
internal sealed class SpeechLog(string path)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>How many utterances the file holds.</summary>
    public int Count
        => Read().Count;

    /// <summary>Waits for the expected utterances, in order, among those recorded after the mark.</summary>
    /// <remarks>Each expected text is matched as whole words, after the one matched before it, further along the same utterance or in a later one,
    /// since NVDA speaks a control's name, role and state as one utterance or several; "9 items" is never matched by "19 items".</remarks>
    public async Task<Outcome> WaitForAsync(int mark, string[] expectedUtterances, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            var heardUtterances = Read().Skip(mark).ToList();
            var missingUtterance = GetFirstMissingUtterance(heardUtterances, expectedUtterances);

            if (missingUtterance is null || DateTime.UtcNow >= deadline)
            {
                return new Outcome(heardUtterances, missingUtterance);
            }

            await Task.Delay(100).ConfigureAwait(false);
        }
    }

    private static string? GetFirstMissingUtterance(List<Utterance> heardUtterances, string[] expectedUtterances)
    {
        var index = 0;
        var offset = 0;

        foreach (var text in expectedUtterances)
        {
            var words = new Regex($@"(?<!\w){Regex.Escape(text)}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            Match? match = null;

            for (; index < heardUtterances.Count; index++, offset = 0)
            {
                match = words.Match(heardUtterances[index].Text, offset);

                if (match.Success)
                {
                    break;
                }
            }

            if (match is not { Success: true })
            {
                return text;
            }

            offset = match.Index + match.Length;
        }

        return null;
    }

    private List<Utterance> Read()
    {
        // The recorder appends while this reads, so the file is shared and a half-written last line is left
        // for the next read.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var utterances = new List<Utterance>();

        while (reader.ReadLine() is { } line)
        {
            try
            {
                if (JsonSerializer.Deserialize<Utterance>(line, Options) is { } utterance)
                {
                    utterances.Add(utterance);
                }
            }
            catch (JsonException)
            {
                break;
            }
        }

        return utterances;
    }
}
