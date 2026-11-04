namespace Forma.Framework.Browser.Interop.Text;

/// <summary>The range a text field's selection spans, in UTF-16 code units.</summary>
/// <param name="Start">Where the selection starts.</param>
/// <param name="End">Where the selection ends, which is its start where nothing is selected.</param>
public sealed record TextSelection(int Start, int End);
