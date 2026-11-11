#!/usr/bin/env dotnet
#:package Microsoft.Playwright@1.*
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

// Measures the controls Forma’s components render across the Showcase and reports any standing on no
// step of the scale. The steps are the target sizes the tokens set, which is what a coarse pointer is
// judged against, so a control off them is off the one measure a reader feels rather than reads.
using Microsoft.Playwright;

// The address is whichever argument is not a switch, so a switch given on its own is not read as a place
// to go.
var baseUrl = args.FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal))?.TrimEnd('/') ?? "http://localhost:5000";
var show = args.Contains("--show");

// The browser this drives is not the repository's to install, so the script fetches it on request rather
// than leaving someone to work out what a binding's failure was asking for.
if (args.Contains("--install"))
{
    return Microsoft.Playwright.Program.Main(["install", "chrome"]);
}

// The steps, in pixels at the 16px root: sm, md, touch, lg and xl.
int[] scale = [24, 32, 40, 44, 48];

using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);

var browser = await LaunchChromeAsync(playwright, show).ConfigureAwait(false);
if (browser is null)
{
    return 3;
}

// Closed when the run ends, whichever way it ends.
await using var browserDisposal = browser.ConfigureAwait(false);

var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 1000 } }).ConfigureAwait(false);

await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);
var routes = await page.EvaluateAsync<string[]>("() => [...new Set([...document.querySelectorAll('.showcase-sidebar a[href]')].map(a => new URL(a.href).pathname))]").ConfigureAwait(false);

const string Probe = """
    () => {
        const controls = document.querySelectorAll('.showcase-main :is(button, input:not([type=checkbox]):not([type=radio]):not([type=hidden]):not([type=range]):not([type=color]):not([type=file]), select, [role=switch], [role=combobox], [role=menuitem], [role=option], [role=tab])');
        // Surfaces the reader presses: a tile, a drop zone, a media facade.
        const surfaces = '.forma-explorer-item-open, .forma-video-player-facade';
        const seen = new Map();
        for (const control of controls) {
            if (control.matches(surfaces)) continue;
            const rect = control.getBoundingClientRect();
            if (rect.height === 0 || rect.width === 0) continue;
            const classes = [...control.classList].filter(c => c.startsWith('forma-')).slice(0, 2).join('.');
            const key = control.tagName.toLowerCase() + (classes ? '.' + classes : '');
            const heights = seen.get(key) ?? new Set();
            heights.add(Math.round(rect.height));
            seen.set(key, heights);
        }
        return [...seen].map(([key, heights]) => ({ key, heights: [...heights] }));
    }
    """;

var offenders = new List<string>();

foreach (var route in routes)
{
    try
    {
        var response = await page.GotoAsync(baseUrl + route, new() { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);

        if (response is null || !response.Ok)
        {
            Console.WriteLine($"{route}: HTTP {response?.Status}");
            continue;
        }
    }
    catch (Exception exception) when (exception is PlaywrightException or TimeoutException)
    {
        Console.WriteLine($"{route}: {exception.Message.Split('\n')[0]}");
        continue;
    }

    await page.WaitForTimeoutAsync(show ? 800 : 300).ConfigureAwait(false);
    var measurements = await page.EvaluateAsync<Measurement[]>(Probe).ConfigureAwait(false);

    foreach (var control in measurements)
    {
        var off = control.Heights.Where(height => !scale.Contains(height)).ToArray();

        if (off.Length > 0)
        {
            offenders.Add($"{route}  {control.Key}  {string.Join("/", off)}px");
        }
    }

    Console.WriteLine($"{route}: {measurements.Length} kinds of control, {measurements.Count(c => c.Heights.All(scale.Contains))} on the scale");
}

Console.WriteLine();

if (offenders.Count == 0)
{
    Console.WriteLine("Every control stands on the scale.");
    return 0;
}

Console.WriteLine("Off the scale:");

foreach (var offender in offenders)
{
    Console.WriteLine("  " + offender);
}

return 1;

// Chrome is not the repository's to install, so its absence is reported with what to run rather than left
// as a binding's exception to interpret.
static async Task<IBrowser?> LaunchChromeAsync(IPlaywright playwright, bool isVisible)
{
    try
    {
        return await playwright.Chromium.LaunchAsync(new() { Channel = "chrome", Headless = !isVisible, SlowMo = isVisible ? 200 : 0 }).ConfigureAwait(false);
    }
    catch (PlaywrightException exception)
    {
        await Console.Error.WriteLineAsync("Could not start Chrome, which this script drives to measure the Showcase as a reader sees it.").ConfigureAwait(false);
        await Console.Error.WriteLineAsync("Install what it needs, once, with: dotnet run scripts/cs/CheckComponentScale.cs -- --install").ConfigureAwait(false);
        await Console.Error.WriteLineAsync($"  {exception.Message.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()}").ConfigureAwait(false);
        return null;
    }
}

internal sealed class Measurement
{
    public string Key { get; set; } = string.Empty;

    public int[] Heights { get; set; } = [];
}
