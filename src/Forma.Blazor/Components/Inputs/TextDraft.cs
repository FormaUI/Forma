namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>An edit in progress: the text typed into a field and not yet committed, and the culture it is read in.</summary>
internal sealed class TextDraft
{
    /// <summary>The text as typed.</summary>
    public string? Text { get; private set; }

    /// <summary>The culture in force when the typing began, which reads the text whatever culture is in force when it commits.</summary>
    public CultureInfo? Culture { get; private set; }

    /// <summary>Whether an edit is in progress.</summary>
    [MemberNotNullWhen(true, nameof(Text), nameof(Culture))]
    public bool IsInProgress
        => Text is not null && Culture is not null;

    /// <summary>Takes the text typed, beginning the edit in <paramref name="culture"/> where none is in progress.</summary>
    public void Edit(string? text, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        Culture ??= culture;
        Text = text ?? string.Empty;
    }

    /// <summary>Ends the edit.</summary>
    public void Clear()
    {
        Text = null;
        Culture = null;
    }
}
