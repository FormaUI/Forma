namespace Forma.Framework.Browser.Interop.Text;

/// <summary>How a text field sends its edits, the most it accepts, and where it shows the running count of its characters.</summary>
public sealed record TextEditOptions
{
    /// <summary>Whether the field sends each edit as it is typed, rather than the edits since its last commit as it commits.</summary>
    public bool SendsOnInput { get; init; }

    /// <summary>The greatest number of characters the field accepts, shown after the count.</summary>
    public int? MaxLength { get; init; }

    /// <summary>The id of the element whose text shows the running count of the field's characters, which the browser keeps as the reader types.</summary>
    public string? CountOutputId { get; init; }
}
