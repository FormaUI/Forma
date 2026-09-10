namespace Forma.Blazor.Components;

using Forma.Framework.Css;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>What every Forma form input is: the framework's input with the same reach, strings, log, host surface and disposal as any other Forma component.</summary>
/// <typeparam name="TValue">The value the input binds.</typeparam>
public abstract class FormaInputBase<TValue> : InputBase<TValue>, IAsyncDisposable
{
    private ILogger? _logger;
    private IStringLocale? _locale;

    /// <summary>The root element's id, verbatim.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The classes the host adds to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The style the host adds to the root element.</summary>
    [Parameter] public string? Style { get; set; }

    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = default!;
    [Inject] private IStringLocaleFactory LocaleFactory { get; set; } = default!;

    /// <summary>The component's identity for its lifetime, which the ids of its elements derive from.</summary>
    public Guid RuntimeId { get; } = Guid.CreateVersion7();

    /// <summary>Whether the component has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>The root element, once rendered.</summary>
    public virtual ElementReference RootElement { get; protected set; }

    /// <summary>The type whose translation files the component reads: its own, unless it shares a feature's.</summary>
    protected virtual Type LocalizationSourceType
        => GetType();

    /// <summary>The component's log, under its own type as the category.</summary>
    protected ILogger Logger
        => _logger ??= LoggerFactory.CreateLogger(GetType());

    /// <summary>The component's strings, in the reader's culture.</summary>
    protected IStringLocale L
        => _locale ??= LocaleFactory.Create(LocalizationSourceType);

    /// <summary>The class written on the root element: the component's own, the host's, then the field's validation class; null when there is none.</summary>
    protected string? RootClass
        => RootAttributeValues.GetRootClass(BuildRootHostAndFieldClass);

    /// <summary>The style written on the root element: the component's own, then the host's; null when there is none.</summary>
    protected string? RootStyle
        => RootAttributeValues.GetRootStyle(BuildRootStyle(), Style);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        await DisposeAsyncCore();

        // The renderer calls only DisposeAsync on a component that implements it, so the framework's own cleanup runs from here.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Adds the classes the component gives its root element in its current state.</summary>
    protected virtual void BuildRootClass(CssClassBuilder builder)
    {
    }

    /// <summary>The style the component gives its root element in its current state; nothing by default.</summary>
    protected virtual string? BuildRootStyle()
        => null;

    /// <summary>Releases what the component holds; runs once.</summary>
    protected virtual ValueTask DisposeAsyncCore()
        => ValueTask.CompletedTask;

    private void BuildRootHostAndFieldClass(CssClassBuilder builder)
    {
        BuildRootClass(builder);
        builder.Add(Class);
        builder.Add(CssClass);
    }
}
