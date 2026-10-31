namespace Forma.Blazor.Components.Composer.Dialogs;

using Microsoft.AspNetCore.Components;

/// <summary>The dialog that collects a table.</summary>
public sealed partial class FormaComposerTableDialog : ComposerDialogBase
{
    private int _rows = 3;
    private int _columns = 3;
    private bool _includeHeader = true;
    private string? _caption;

    /// <summary>The maximum number of rows.</summary>
    [Parameter] public int MaxRows { get; set; } = 20;

    /// <summary>The maximum number of columns.</summary>
    [Parameter] public int MaxColumns { get; set; } = 10;

    /// <summary>Callback invoked with the confirmed table.</summary>
    [Parameter] public EventCallback<TableDialogResult> OnInsert { get; set; }

    private async Task HandleConfirmAsync()
    {
        var result = new TableDialogResult
        {
            Rows = Math.Clamp(_rows, 1, MaxRows),
            Columns = Math.Clamp(_columns, 1, MaxColumns),
            IncludeHeader = _includeHeader,
            Caption = _caption,
        };

        await OnInsert.InvokeAsync(result);
    }
}
