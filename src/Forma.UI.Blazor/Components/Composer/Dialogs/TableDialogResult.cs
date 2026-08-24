namespace Forma.UI.Blazor.Components.Composer.Dialogs;

/// <summary>The data collected from the table dialog.</summary>
/// <param name="Rows">The total number of rows in the table.</param>
/// <param name="Columns">The total number of columns in the table.</param>
/// <param name="IncludeHeader">Whether the first row is rendered inside a thead with th header cells.</param>
/// <param name="Caption">The optional accessibility caption for the table.</param>
public sealed record TableDialogResult(
    int Rows,
    int Columns,
    bool IncludeHeader,
    string? Caption);
