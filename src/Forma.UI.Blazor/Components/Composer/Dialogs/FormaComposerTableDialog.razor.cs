namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for configuring and inserting HTML tables.</summary>
public sealed partial class FormaComposerTableDialog : ComposerDialogBase
{
    private int _rows = 3;
    private int _columns = 3;
    private bool _includeHeader = true;
    private string? _caption;

    /// <summary>The maximum rows allowed.</summary>
    [Parameter] public int MaxRows { get; set; } = 20;

    /// <summary>The maximum columns allowed.</summary>
    [Parameter] public int MaxColumns { get; set; } = 10;

    /// <summary>Callback invoked when table configuration is confirmed.</summary>
    [Parameter] public EventCallback<TableDialogResult> OnInsert { get; set; }

    private async Task HandleConfirmAsync()
    {
        var result = new TableDialogResult(
            Math.Clamp(_rows, 1, MaxRows),
            Math.Clamp(_columns, 1, MaxColumns),
            _includeHeader,
            _caption);

        await OnInsert.InvokeAsync(result);
    }
}
