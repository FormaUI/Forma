namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A row of toggle buttons, one per view a data view registered, pressing the one showing; a page composes it beside the data view it switches.</summary>
public sealed partial class FormaDataViewSwitch : FormaElementBase
{
    private IDataViewSwitcher? _observedDataView;

    /// <summary>The data view whose views the switch offers, for a switch composed outside it; inside, the data view reaches the switch itself.</summary>
    [Parameter] public IDataViewSwitcher? DataView { get; set; }

    [CascadingParameter] private IDataViewSwitcher? EnclosingDataView { get; set; }

    // The data view the switch drives: the one given, else the one it stands inside.
    private IDataViewSwitcher Views
        => DataView ?? EnclosingDataView ?? throw new InvalidOperationException($"{nameof(FormaDataViewSwitch)} needs a {nameof(DataView)}, or to be declared inside a data view.");

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // The switch follows the data view it drives, and lets go of one it drove before.
        if (!ReferenceEquals(_observedDataView, Views))
        {
            _observedDataView?.Unsubscribe(HandleDataViewChangedAsync);
            _observedDataView = Views;
            _observedDataView.Subscribe(HandleDataViewChangedAsync);
        }
    }

    private Task HandleDataViewChangedAsync()
        => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add("forma-data-view-switch");
    }

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        _observedDataView?.Unsubscribe(HandleDataViewChangedAsync);

        return base.DisposeAsyncCore();
    }
}
