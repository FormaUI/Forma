namespace Forma.Framework.Browser.Interop.Text;

/// <summary>How a text field's running count of its characters is shown.</summary>
public sealed record CharacterCountOptions
{
    /// <summary>The id of the element whose text shows the count.</summary>
    public required string OutputId { get; init; }

    /// <summary>The greatest number of characters the field accepts, shown after the count.</summary>
    public int? MaxLength { get; init; }
}
