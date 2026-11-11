namespace Forma.Blazor.Components.Composer.Dialogs;

using Microsoft.AspNetCore.Components;

/// <summary>The dialog that collects a table.</summary>
public sealed partial class FormaComposerTableDialog : ComposerDialogBase
{
    private int _rowCount = 3;
    private int _columnCount = 3;
    private bool _includeHeader = true;
    private string? _caption;

    /// <summary>The maximum number of rows.</summary>
    [Parameter] public int MaxRowCount { get; set; } = 20;

    /// <summary>The maximum number of columns.</summary>
    [Parameter] public int MaxColumnCount { get; set; } = 10;

    /// <summary>Callback invoked with the confirmed table.</summary>
    [Parameter] public EventCallback<TableDialogResult> OnInsert { get; set; }

    private async Task HandleConfirmAsync()
    {
        var result = new TableDialogResult
        {
            RowCount = Math.Clamp(_rowCount, 1, MaxRowCount),
            ColumnCount = Math.Clamp(_columnCount, 1, MaxColumnCount),
            IncludeHeader = _includeHeader,
            Caption = _caption,
        };

        await OnInsert.InvokeAsync(result);
    }
}
