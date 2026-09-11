namespace Forma.Blazor.Components;

using Forma.Framework.Core.Interop;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>What every Forma component is: it reaches the browser, speaks the reader's language, can say what happened to it, and is disposed once.</summary>
public abstract class FormaComponentBase : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = default!;
    [Inject] private IStringLocaleFactory LocaleFactory { get; set; } = default!;

    /// <summary>Whether the component has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>The type whose translation files the component reads: its own, unless it shares a feature's.</summary>
    protected virtual Type LocalizationSourceType
        => GetType();

    /// <summary>What the component is given: its identity, its strings, its log and its JS environment.</summary>
    protected IComponentContext Context
        => field ??= new ComponentContext(
            () => JSRuntime,
            () => LocaleFactory.Create(LocalizationSourceType),
            () => LoggerFactory.CreateLogger(GetType()));

    /// <summary>The component's strings, in the reader's culture.</summary>
    protected IStringLocale L
        => Context.Locale;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases what the component holds; runs once.</summary>
    protected virtual ValueTask DisposeAsyncCore()
        => ValueTask.CompletedTask;
}
