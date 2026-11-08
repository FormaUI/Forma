namespace Forma.Blazor.Browser.WebView;

using System.Collections.Concurrent;
using System.Globalization;
using System.Windows;

using Forma.Framework.Culture;

using Microsoft.Extensions.DependencyInjection;

/// <summary>The check host's native side: its native windows, what each one's web view attached, the ways a culture can be applied to them, and a switch made from native code.</summary>
/// <remarks>WPF's web view dispatches its components on the application's dispatcher whatever thread its native window runs on, so every native window here runs on the application's UI thread.</remarks>
internal sealed class ProbeShell
{
    private readonly ConcurrentDictionary<string, WebViewHostWindow> _hostWindows = new();
    private readonly ConcurrentDictionary<string, ProbeAttachment> _attachments = new();
    private readonly ConcurrentQueue<string> _releasedWebViews = new();
    private int _webViewCount;

    /// <summary>The services every web view resolves from.</summary>
    public IServiceProvider Services { get; set; } = default!;

    /// <summary>Raised after a web view's scope is released.</summary>
    public event Action? ScopeReleased;

    /// <summary>The names of the web views whose scopes have been released, in the order they were.</summary>
    public IReadOnlyList<string> ReleasedWebViews
        => [.. _releasedWebViews];

    /// <summary>Opens a native window whose web view starts at <paramref name="startPath"/>, the web view named <paramref name="name"/>, or by the order web views open in, <c>A</c> to <c>Z</c>, then <c>AA</c> on, where none is given.</summary>
    /// <param name="startPath">The address the web view starts at.</param>
    /// <param name="name">The web view's name, which a check that opens a web view of its own gives it.</param>
    /// <returns>The web view's name.</returns>
    public string Open(string startPath, string? name = null)
    {
        var webViewName = name ?? GetName(Interlocked.Increment(ref _webViewCount));
        Application.Current.Dispatcher.Invoke(() => Show(webViewName, startPath));

        return webViewName;
    }

    /// <summary>Closes the native window of the web view <paramref name="name"/> names, disposing the web view first where <paramref name="isDisposing"/> says so.</summary>
    /// <param name="name">The web view's name.</param>
    /// <param name="isDisposing">Whether the web view is disposed before its native window closes.</param>
    public void Close(string name, bool isDisposing)
    {
        if (_hostWindows.TryRemove(name, out var hostWindow))
        {
            hostWindow.Dispatcher.InvokeAsync(() => hostWindow.CloseAsync(isDisposing));
        }
    }

    /// <summary>Records what a web view attached as its page initialized.</summary>
    /// <param name="attachment">What the web view attached.</param>
    public void Attach(ProbeAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        _attachments[attachment.WebView] = attachment;
    }

    /// <summary>Records that the web view <paramref name="webView"/> names released its scope.</summary>
    /// <param name="webView">The web view's name, none where its page never learned it.</param>
    public void Release(string? webView)
    {
        if (webView is not null)
        {
            _attachments.TryRemove(webView, out _);
        }

        _releasedWebViews.Enqueue(webView ?? "unnamed");
        ScopeReleased?.Invoke();
    }

    /// <summary>Sets the process's default cultures alone.</summary>
    /// <param name="culture">The formatting culture.</param>
    /// <param name="uiCulture">The UI culture.</param>
    public static void ApplyToDefaults(CultureInfo culture, CultureInfo uiCulture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
    }

    /// <summary>Sets the process's default cultures and, outside any flow a caller captured, the cultures each web view's UI thread runs its events in.</summary>
    /// <param name="culture">The formatting culture.</param>
    /// <param name="uiCulture">The UI culture.</param>
    /// <returns>A task that completes once every web view's UI thread took the cultures.</returns>
    public Task ApplyToThreadsAsync(CultureInfo culture, CultureInfo uiCulture)
    {
        ApplyToDefaults(culture, uiCulture);
        var threadAssignments = new List<Task>();

        using (ExecutionContext.SuppressFlow())
        {
            foreach (var attachment in _attachments.Values)
            {
                var threadAssignment = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                attachment.Context.Post(
                    _ =>
                    {
                        CultureInfo.CurrentCulture = culture;
                        CultureInfo.CurrentUICulture = uiCulture;
                        threadAssignment.SetResult();
                    },
                    null);
                threadAssignments.Add(threadAssignment.Task);
            }
        }

        return Task.WhenAll(threadAssignments);
    }

    /// <summary>Sets the process's default cultures and the cultures of each web view's renderer, through its dispatcher, in the flow the caller captured.</summary>
    /// <param name="culture">The formatting culture.</param>
    /// <param name="uiCulture">The UI culture.</param>
    /// <returns>A task that completes once every web view's renderer ran the change.</returns>
    public Task ApplyInCapturedFlowAsync(CultureInfo culture, CultureInfo uiCulture)
    {
        ApplyToDefaults(culture, uiCulture);

        return Task.WhenAll(_attachments.Values.Select(attachment => attachment.InvokeAsync(() =>
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        })));
    }

    /// <summary>Switches the application's culture from the application's native code, on a thread of the thread pool rather than any web view's.</summary>
    /// <param name="culture">The culture switched to, both formatting and UI.</param>
    /// <returns>A task that completes once the switch completed.</returns>
    public Task SwitchFromNativeCodeAsync(CultureInfo culture)
        => Task.Run(() => Services.GetRequiredService<ICultureSwitch>().SwitchAsync(culture));

    // The letters a spreadsheet names its columns by: 1 is A, 26 is Z and 27 is AA.
    private static string GetName(int number)
    {
        var name = string.Empty;

        for (; number > 0; number = (number - 1) / 26)
        {
            name = (char)('A' + ((number - 1) % 26)) + name;
        }

        return name;
    }

    private void Show(string name, string startPath)
    {
        var hostWindow = new WebViewHostWindow(this, name, startPath);
        _hostWindows[name] = hostWindow;
        hostWindow.Show();
    }
}
