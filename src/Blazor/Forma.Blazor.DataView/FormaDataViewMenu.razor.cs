namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components;

/// <summary>The menu a narrow view draws in place of its actions column: Select, and the host's commands on the selected items.</summary>
/// <typeparam name="TItem">The item the data view manages.</typeparam>
/// <remarks>It draws again as the data view's state changes, the selection above all, which decides what each command can do.</remarks>
public sealed partial class FormaDataViewMenu<TItem> : FormaComponentBase
    where TItem : notnull
{
    private IDataView<TItem>? _observedDataView;

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocaleSourceType
        => typeof(FormaDataView<TItem>);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataViewMenu<>)} must be rendered inside a {nameof(FormaDataView<>)}.");
        }

        if (!ReferenceEquals(_observedDataView, View))
        {
            _observedDataView?.Unsubscribe(HandleDataViewChangedAsync);
            _observedDataView = View;
            _observedDataView.Subscribe(HandleDataViewChangedAsync);
        }
    }

    private Task HandleDataViewChangedAsync()
        => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        _observedDataView?.Unsubscribe(HandleDataViewChangedAsync);

        return base.DisposeAsyncCore();
    }
}
