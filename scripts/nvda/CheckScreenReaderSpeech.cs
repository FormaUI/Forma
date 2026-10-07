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

var positional = args.Where(argument => !argument.StartsWith("--", StringComparison.Ordinal)).ToArray();
var show = args.Contains("--show");

if (positional.Length < 3)
{
    await Console.Error.WriteLineAsync("Usage: CheckScreenReaderSpeech <showcase address> <speech log> <transcript>").ConfigureAwait(false);
    return 2;
}

var baseUrl = positional[0].TrimEnd('/');
var speech = new SpeechLog(positional[1]);
var transcript = new StringBuilder();

Scenario[] scenarios =
[
    new("The slow source's first load", "en", "/data-view/states",
    [
        new("open the page", null, ["Loading", "9 items"]),
    ]),
    new("The tree's focus kept while it scrolls", "en", "/data-view/tree", TreeScrolling(rightToLeft: false)),
    new("The tree's focus kept while it scrolls", "he", "/he/data-view/tree", TreeScrolling(rightToLeft: true)),
    new("The tree's focus handed on when its node goes away", "en", "/data-view/tree", TreeHandOff(rightToLeft: false)),
    new("The tree's focus handed on when its node goes away", "he", "/he/data-view/tree", TreeHandOff(rightToLeft: true)),
    new("A checkbox and its state", "en", "/checkbox", CheckBoxState("Subscribe to updates")),
    new("A checkbox and its state", "he", "/he/checkbox", CheckBoxState("הרשמה לעדכונים")),
    new("A time picker and its list", "en", "/time-picker", TimePickerList("Starts", "12:00 AM", "12:30 AM")),
    new("A time picker and its list", "he", "/he/time-picker", TimePickerList("מתחיל", "0:00", "0:30")),
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

await using var closing = browser.ConfigureAwait(false);

var failures = 0;

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
            failures++;
            break;
        }

        var outcome = await speech.WaitForAsync(mark, step.Expect, step.Timeout ?? TimeSpan.FromSeconds(10)).ConfigureAwait(false);

        transcript.AppendLine(CultureInfo.InvariantCulture, $"## {step.Description}: {(outcome.Missing is null ? "passed" : $"missing \"{outcome.Missing}\"")}");
        transcript.AppendLine(CultureInfo.InvariantCulture, $"Expected, in order: {string.Join(" | ", step.Expect)}");

        foreach (var utterance in outcome.Heard)
        {
            transcript.AppendLine(CultureInfo.InvariantCulture, $"  {utterance.Sequence,4}  {utterance.Text}");
        }

        if (outcome.Missing is not null)
        {
            failures++;
        }

        if (show)
        {
            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        }
    }

    await page.CloseAsync().ConfigureAwait(false);
}

await File.WriteAllTextAsync(positional[2], transcript.ToString()).ConfigureAwait(false);
await Console.Out.WriteAsync(transcript.ToString()).ConfigureAwait(false);

return failures == 0 ? 0 : 1;

// The archive of a hundred thousand nodes: a node focused, scrolled out of view, and the keys carrying on from it. A name is
// expected as NVDA speaks it, its full stop read as "dot".
static Step[] TreeScrolling(bool rightToLeft)
{
    return [
        new("open the page", null, []),
        new("focus the first archive", page => TreeNode(page, 1, "Archive 1").FocusAsync(), ["Archive 1"]),
        new("open it", page => page.Keyboard.PressAsync(rightToLeft ? "ArrowLeft" : "ArrowRight"), ["expanded"]),
        new("move to the third scan", page => PressAsync(page, "ArrowDown", 3), ["Scan 1-00003 dot jpg"]),
        new("scroll it out of view, then move on", async page =>
        {
            await page.Locator(".demo-section").Nth(1).Locator(".forma-data-tree").EvaluateAsync("tree => tree.scrollTop = 60000").ConfigureAwait(false);
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
        new("focus Documents", page => TreeNode(page, 3, "Documents").FocusAsync(), ["Documents"]),
        new("open it", page => page.Keyboard.PressAsync(rightToLeft ? "ArrowLeft" : "ArrowRight"), ["expanded"]),
        new("move to Reports", page => page.Keyboard.PressAsync("ArrowDown"), ["Reports"]),
        new("take Reports away while it holds the focus", page => PressControlAsync(page, 3, 1), ["Contracts"], TimeSpan.FromSeconds(8)),
        new("put everything back, and move to Reports", async page =>
        {
            await PressControlAsync(page, 3, 6).ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
            await page.Keyboard.PressAsync("ArrowUp").ConfigureAwait(false);
        }, ["Reports"]),
        new("open it", page => page.Keyboard.PressAsync(rightToLeft ? "ArrowLeft" : "ArrowRight"), ["expanded"]),
        new("move to Q1.pdf", page => page.Keyboard.PressAsync("ArrowDown"), ["Q 1 dot pdf"]),
        new("close Documents over the focus", page => PressControlAsync(page, 3, 4), ["Documents"], TimeSpan.FromSeconds(8)),
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
static Step[] TimePickerList(string label, string first, string second)
{
    return [
        new("open the page", null, []),
        new("focus the field", page => page.Locator(".forma-time-box input").First.FocusAsync(), [label, "combo box"]),
        new("open the list", page => page.Keyboard.PressAsync("ArrowDown"), [first, "1 of 48"]),
        new("move down the list", page => page.Keyboard.PressAsync("ArrowDown"), [second, "2 of 48"]),
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

static async Task PressAsync(IPage page, string key, int times)
{
    for (var press = 0; press < times; press++)
    {
        await page.Keyboard.PressAsync(key).ConfigureAwait(false);
    }
}

// Presses a section's control without taking the focus to it, as a change arriving from elsewhere would.
static Task PressControlAsync(IPage page, int section, int control)
{
    return page.Locator(".demo-section").Nth(section).Locator(".demo-controls button").Nth(control).EvaluateAsync("button => button.click()");
}

/// <summary>A page in a culture, and the steps taken on it.</summary>
internal sealed record Scenario(string Name, string Culture, string Path, Step[] Steps);

/// <summary>An action, and the utterances it must be followed by, in order.</summary>
/// <remarks>A step with no action opens the scenario's page.</remarks>
internal sealed record Step(string Description, Func<IPage, Task>? Act, string[] Expect, TimeSpan? Timeout = null);

/// <summary>One utterance the recorder wrote.</summary>
internal sealed record Utterance(int Sequence, string Text);

/// <summary>What a step heard, and the first expected utterance it did not.</summary>
internal sealed record Outcome(IReadOnlyList<Utterance> Heard, string? Missing);

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
    public async Task<Outcome> WaitForAsync(int mark, string[] expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            var heard = Read().Skip(mark).ToList();
            var missing = FirstMissing(heard, expected);

            if (missing is null || DateTime.UtcNow >= deadline)
            {
                return new Outcome(heard, missing);
            }

            await Task.Delay(100).ConfigureAwait(false);
        }
    }

    private static string? FirstMissing(List<Utterance> heard, string[] expected)
    {
        var index = 0;
        var offset = 0;

        foreach (var text in expected)
        {
            var words = new Regex($@"(?<!\w){Regex.Escape(text)}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            Match? match = null;

            for (; index < heard.Count; index++, offset = 0)
            {
                match = words.Match(heard[index].Text, offset);

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
