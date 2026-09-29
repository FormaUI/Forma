namespace Forma.Blazor.Components;

using Forma.Framework.Css;
using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>What every Forma form input is: the framework's input with the same reach, strings, log, host surface and disposal as any other Forma component.</summary>
/// <typeparam name="TValue">The value the input binds.</typeparam>
public abstract class FormaInputBase<TValue> : InputBase<TValue>, IAsyncDisposable
{
    /// <summary>The root element's id, verbatim.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The id the input's elements derive theirs from: the root's own when the host gave one, and one minted from the runtime id otherwise.</summary>
    public string ElementId
        => Id ?? $"forma-{Context.RuntimeId:N}";

    /// <summary>The classes the host adds to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The style the host adds to the root element.</summary>
    [Parameter] public string? Style { get; set; }

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = default!;
    [Inject] private IStringLocaleFactory LocaleFactory { get; set; } = default!;

    /// <summary>Whether the component has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>The root element, once rendered.</summary>
    public virtual ElementReference RootElement { get; protected set; }

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

    /// <summary>The class written on the root element: the component's own, the host's, then the field's validation class; null when there is none.</summary>
    protected string? RootClass
    {
        get
        {
            var builder = new CssClassBuilder();
            BuildRootClass(builder);
            builder.Add(Class);
            builder.Add(CssClass);

            return builder.ToStringOrNull();
        }
    }

    /// <summary>The style written on the root element: the component's own, then the host's; null when there is none.</summary>
    protected string? RootStyle
    {
        get
        {
            var builder = new CssStyleBuilder();
            BuildRootStyle(builder);
            builder.Add(Style);

            return builder.ToStringOrNull();
        }
    }

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

    /// <summary>Adds the declarations the component gives its root element in its current state.</summary>
    protected virtual void BuildRootStyle(CssStyleBuilder builder)
    {
    }

    /// <summary>Releases what the component holds; runs once.</summary>
    protected virtual ValueTask DisposeAsyncCore()
        => ValueTask.CompletedTask;
}
