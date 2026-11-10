namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A data view's footer: the paginator at the inline start, the status at the inline end and the offer of the whole set.</summary>
public sealed partial class FormaDataViewFooter : FormaElementBase
{
    [CascadingParameter] private IDataViewFooter Footer { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaDataView<>);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Footer is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataViewFooter)} must be declared inside a {nameof(FormaDataView<>)}.");
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add("forma-data-view-footer");
    }
}
