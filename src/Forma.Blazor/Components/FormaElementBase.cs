namespace Forma.Blazor.Components;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A component with a root element its host addresses: by id, class, style and any further attribute.</summary>
public abstract class FormaElementBase : FormaComponentBase
{
    /// <summary>The root element's id, verbatim.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The classes the host adds to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The style the host adds to the root element.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Additional attributes applied to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The root element, once rendered.</summary>
    public virtual ElementReference RootElement { get; protected set; }

    /// <summary>The class written on the root element: the component's own, then the host's; null when there is none.</summary>
    protected string? RootClass
    {
        get
        {
            var builder = new CssClassBuilder();
            BuildRootClass(builder);
            builder.Add(Class);

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

    /// <summary>Adds the classes the component gives its root element in its current state.</summary>
    protected virtual void BuildRootClass(CssClassBuilder builder)
    {
    }

    /// <summary>Adds the declarations the component gives its root element in its current state.</summary>
    protected virtual void BuildRootStyle(CssStyleBuilder builder)
    {
    }
}
