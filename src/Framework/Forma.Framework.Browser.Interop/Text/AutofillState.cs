namespace Forma.Framework.Browser.Interop.Text;

/// <summary>Whether the browser filled a text field without the reader typing.</summary>
/// <param name="IsFilled">Whether the field holds what the browser filled.</param>
public sealed record AutofillState(bool IsFilled);
