namespace Forma.Blazor.Components;

using Forma.Framework.Css;
using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>The base of every Forma form input: the framework's input with the context, strings, log, host surface and disposal of any Forma component.</summary>
/// <typeparam name="TValue">The value the input binds.</typeparam>
public abstract class FormaInputBase<TValue> : InputBase<TValue>, IAsyncDisposable
{
    /// <summary>The root element's id.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The id the input's elements derive theirs from.</summary>
    /// <remarks>The host's id when it gave one, otherwise one minted from the runtime id.</remarks>
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

    /// <summary>The type whose locale files the component reads.</summary>
    protected virtual Type LocalizationSourceType
        => GetType();

    /// <summary>The component's context: its identity, its strings, its log and its JS environment.</summary>
    protected IComponentContext Context
        => field ??= new ComponentContext(
            () => JSRuntime,
            () => LocaleFactory.Create(LocalizationSourceType),
            () => LoggerFactory.CreateLogger(GetType()));

    /// <summary>The component's strings, in the reader's culture.</summary>
    protected IStringLocale L
        => Context.Locale;

    /// <summary>The class written on the root element: the component's own, the host's, then the field's validation class.</summary>
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

    /// <summary>The style written on the root element: the component's own, then the host's.</summary>
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

    /// <summary>The id of the element holding the field's validation messages.</summary>
    protected string MessageContainerId
        => $"{ElementId}-messages";

    /// <summary>Whether the input is drawn inside its field container.</summary>
    protected virtual bool HasFieldContainer
        => true;

    /// <summary>The space-separated element IDs used for the input's aria-describedby attribute.</summary>
    protected string? DescribedBy
    {
        get
        {
            List<string> ids = [];

            if (AdditionalAttributes?.GetValueOrDefault("aria-describedby")?.ToString() is { } host && !string.IsNullOrWhiteSpace(host))
            {
                ids.Add(host);
            }

            BuildDescribedBy(ids);

            if (HasFieldContainer && EditContext is { } editContext && editContext.GetValidationMessages(FieldIdentifier).Any())
            {
                ids.Add(MessageContainerId);
            }

            return ids.Count > 0 ? string.Join(' ', ids) : null;
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

    /// <summary>Adds the IDs of elements rendered by this component that describe the input.</summary>
    protected virtual void BuildDescribedBy(ICollection<string> ids)
    {
    }

    /// <summary>Releases what the component holds.</summary>
    /// <remarks>The base runs it once.</remarks>
    protected virtual ValueTask DisposeAsyncCore()
        => ValueTask.CompletedTask;
}
