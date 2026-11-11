namespace Forma.Blazor.Components.Composer.Dialogs;

using System.Diagnostics.CodeAnalysis;

/// <summary>The data collected from the table dialog.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record TableDialogResult : ComposerDialogResult
{
    /// <summary>The number of rows in the table.</summary>
    public required int RowCount { get; init; }

    /// <summary>The number of columns in the table.</summary>
    public required int ColumnCount { get; init; }

    /// <summary>Whether the first row is a header row.</summary>
    public required bool IncludeHeader { get; init; }

    /// <summary>The caption of the table.</summary>
    public required string? Caption { get; init; }
}
