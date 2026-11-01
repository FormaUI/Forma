namespace Forma.Blazor.Browser.WebView;

using System.Collections.Concurrent;
using System.Globalization;
using System.Windows;

using Microsoft.AspNetCore.Components;

/// <summary>The check host's native side: its windows, what each window's web view attached, and the ways a culture can be applied to them.</summary>
/// <remarks>WPF's web view dispatches its components on the application's dispatcher whatever thread its window runs on, so every window here runs on the application's UI thread.</remarks>
internal sealed class ProbeShell
{
    private readonly ConcurrentDictionary<string, ProbeWindow> _windows = new();
    private readonly ConcurrentDictionary<string, ProbeAttachment> _attachments = new();
    private int _windowCount;
    private int _released;

    /// <summary>The services every window's web view resolves from.</summary>
    public IServiceProvider Services { get; set; } = default!;

    /// <summary>Raised after a web view's scope is released.</summary>
    public event Action? ScopeReleased;

    /// <summary>How many web views' scopes have been released.</summary>
    public int Released
        => Volatile.Read(ref _released);

    /// <summary>Opens a window at <paramref name="startPath"/>, named by the order windows open in, <c>A</c> first.</summary>
    /// <param name="startPath">The address its web view starts at.</param>
    /// <returns>The window's name.</returns>
    public string Open(string startPath)
    {
        var name = ((char)('A' + Interlocked.Increment(ref _windowCount) - 1)).ToString();
        Application.Current.Dispatcher.Invoke(() => Show(name, startPath));

        return name;
    }

    /// <summary>Closes the window <paramref name="name"/> names, disposing its web view first where <paramref name="isDisposing"/> says so.</summary>
    /// <param name="name">The window's name.</param>
    /// <param name="isDisposing">Whether the web view is disposed before the window closes.</param>
    public void Close(string name, bool isDisposing)
    {
        if (_windows.TryRemove(name, out var window))
        {
            window.Dispatcher.InvokeAsync(() => window.CloseAsync(isDisposing));
        }
    }

    /// <summary>Records what a window's web view attached as its page initialized.</summary>
    /// <param name="attachment">What the web view attached.</param>
    public void Attach(ProbeAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        _attachments[attachment.Window] = attachment;
    }

    /// <summary>Records that the web view of <paramref name="window"/> released its scope.</summary>
    /// <param name="window">The window whose web view released it.</param>
    public void Release(string? window)
    {
        if (window is not null)
        {
            _attachments.TryRemove(window, out _);
        }

        Interlocked.Increment(ref _released);
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

    /// <summary>Sets the process's default cultures and, outside any flow a caller captured, the cultures each window's UI thread runs its events in.</summary>
    /// <param name="culture">The formatting culture.</param>
    /// <param name="uiCulture">The UI culture.</param>
    public void ApplyToThreads(CultureInfo culture, CultureInfo uiCulture)
    {
        ApplyToDefaults(culture, uiCulture);

        using (ExecutionContext.SuppressFlow())
        {
            foreach (var attachment in _attachments.Values)
            {
                attachment.Context.Post(
                    _ =>
                    {
                        CultureInfo.CurrentCulture = culture;
                        CultureInfo.CurrentUICulture = uiCulture;
                    },
                    null);
            }
        }
    }

    /// <summary>Sets the process's default cultures and the cultures of each window's renderer, through its dispatcher, in the flow the caller captured.</summary>
    /// <param name="culture">The formatting culture.</param>
    /// <param name="uiCulture">The UI culture.</param>
    /// <returns>A task that completes once every window's renderer ran the change.</returns>
    public Task ApplyInCapturedFlowAsync(CultureInfo culture, CultureInfo uiCulture)
    {
        ApplyToDefaults(culture, uiCulture);

        return Task.WhenAll(_attachments.Values.Select(attachment => attachment.InvokeAsync(() =>
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        })));
    }

    /// <summary>Replaces the address of the window <paramref name="name"/> names with its page under <paramref name="segment"/>, under the mark a handler reads.</summary>
    /// <param name="name">The window's name.</param>
    /// <param name="segment">The address's first segment.</param>
    /// <returns>A task that completes once the window's renderer asked its navigation to replace the address.</returns>
    public Task ReplaceAddressAsync(string name, string segment)
    {
        var attachment = _attachments[name];

        return attachment.InvokeAsync(() =>
        {
            var current = new Uri(attachment.Navigation.Uri);
            var rest = current.AbsolutePath.Split('/', 3, StringSplitOptions.RemoveEmptyEntries) is [_, var path, ..] ? path : "probe";
            attachment.Navigation.NavigateTo($"{segment}/{rest}{current.Query}{current.Fragment}", new NavigationOptions { ReplaceHistoryEntry = true, HistoryEntryState = ProbeAttachment.Mark });
        });
    }

    private void Show(string name, string startPath)
    {
        var window = new ProbeWindow(this, name, startPath);
        _windows[name] = window;
        window.Show();
    }
}
