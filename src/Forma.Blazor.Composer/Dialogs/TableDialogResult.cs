namespace Forma.Blazor.Components.Composer.Dialogs;

/// <summary>The data collected from the table dialog.</summary>
/// <param name="Rows">The number of rows in the table.</param>
/// <param name="Columns">The number of columns in the table.</param>
/// <param name="IncludeHeader">Whether the first row is a header row.</param>
/// <param name="Caption">The caption of the table.</param>
public sealed record TableDialogResult(
    int Rows,
    int Columns,
    bool IncludeHeader,
    string? Caption) : ComposerDialogResult;
