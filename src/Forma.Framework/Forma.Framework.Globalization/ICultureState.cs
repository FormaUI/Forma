namespace Forma.Framework.Globalization;

using System.Globalization;

using Forma.Framework.Events;

/// <summary>The cultures a user scope shows, announced to its subscribers as they change.</summary>
public interface ICultureState : INotifyStateChanged
{
    /// <summary>The culture that formats dates, numbers and collation.</summary>
    CultureInfo Culture { get; }

    /// <summary>The culture that chooses strings and direction.</summary>
    CultureInfo UICulture { get; }

    /// <summary>Whether the UI culture is written right to left.</summary>
    bool IsRightToLeft { get; }

    /// <summary>The revision of the state, advanced once per change.</summary>
    int Revision { get; }

    /// <summary>Changes the cultures, applies them to the calling flow and notifies every subscriber.</summary>
    /// <param name="culture">The culture that formats dates, numbers and collation.</param>
    /// <param name="uiCulture">The culture that chooses strings and direction; <see langword="null"/> takes <paramref name="culture"/>.</param>
    /// <returns>A task that completes once every subscriber has been notified.</returns>
    Task ChangeAsync(CultureInfo culture, CultureInfo? uiCulture = null);
}
