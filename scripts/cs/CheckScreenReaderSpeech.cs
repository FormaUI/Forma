#!/usr/bin/env dotnet
#:package Microsoft.Playwright@1.*
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

// Drives the Showcase in Chrome while NVDA runs, and checks that each scenario step is followed by the
// utterances it expects, in order, in the speech the recorder writes. Arguments: the Showcase's address,
// the recorder's file, and the transcript to write; --show slows the browser and holds each step on screen.
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Playwright;

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
];

using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);

IBrowser browser;

try
{
    browser = await playwright.Chromium.LaunchAsync(new() { Channel = "chrome", Headless = false, SlowMo = show ? 500 : 0 }).ConfigureAwait(false);
}
catch (PlaywrightException exception)
{
    await Console.Error.WriteLineAsync($"Chrome would not start: {exception.Message.Split('\n')[0]}").ConfigureAwait(false);
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
        if (step.Act is null)
        {
            await page.GotoAsync(baseUrl + scenario.Path, new() { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);
        }
        else
        {
            await step.Act(page).ConfigureAwait(false);
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
    /// <remarks>Each expected text is matched as whole words an utterance contains, each after the one matched before it, so "9 items" is never matched by "19 items".</remarks>
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
        var position = 0;

        foreach (var text in expected)
        {
            var words = new Regex($@"(?<!\w){Regex.Escape(text)}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var found = heard.FindIndex(position, utterance => words.IsMatch(utterance.Text));

            if (found < 0)
            {
                return text;
            }

            position = found + 1;
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
