#:package Microsoft.Playwright@1.*
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false
#:property NoWarn=CA1303;CA1812

// Measures every control the Showcase renders and reports any whose height is off the control scale.
using Microsoft.Playwright;

var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var show = args.Contains("--show");

// The steps of the control scale, in pixels at the 16px root: sm, md, touch, lg and xl.
int[] scale = [24, 32, 40, 44, 48];

using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
var browser = await playwright.Chromium.LaunchAsync(new() { Channel = "chrome", Headless = !show, SlowMo = show ? 200 : 0 }).ConfigureAwait(false);
var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 1000 } }).ConfigureAwait(false);

await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);
var routes = await page.EvaluateAsync<string[]>("() => [...new Set([...document.querySelectorAll('.showcase-sidebar a[href]')].map(a => new URL(a.href).pathname))]").ConfigureAwait(false);

const string Probe = """
    () => {
        const controls = document.querySelectorAll('.showcase-main :is(button, input:not([type=checkbox]):not([type=radio]):not([type=hidden]):not([type=range]):not([type=color]):not([type=file]), select, [role=switch], [role=combobox], [role=menuitem], [role=option], [role=tab])');
        // A surface the reader presses is not a control standing in a row: a tile, a drop zone, a media facade.
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
    var measured = await page.EvaluateAsync<Measurement[]>(Probe).ConfigureAwait(false);

    foreach (var control in measured)
    {
        var off = control.Heights.Where(height => !scale.Contains(height)).ToArray();

        if (off.Length > 0)
        {
            offenders.Add($"{route}  {control.Key}  {string.Join("/", off)}px");
        }
    }

    Console.WriteLine($"{route}: {measured.Length} kinds of control, {measured.Count(c => c.Heights.All(scale.Contains))} on the scale");
}

Console.WriteLine();

if (offenders.Count == 0)
{
    Console.WriteLine("Every control stands on the scale.");
    await browser.DisposeAsync().ConfigureAwait(false);
    return 0;
}

Console.WriteLine("Off the scale:");

foreach (var offender in offenders)
{
    Console.WriteLine("  " + offender);
}

await browser.DisposeAsync().ConfigureAwait(false);
return 1;

internal sealed class Measurement
{
    public string Key { get; set; } = string.Empty;

    public int[] Heights { get; set; } = [];
}
