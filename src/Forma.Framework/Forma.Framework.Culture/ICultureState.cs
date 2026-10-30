namespace Forma.Framework.Culture;

using System.Globalization;

using Forma.Framework.Events;

/// <summary>The cultures a user scope shows, announced to its subscribers as they change, which change only as the state's integration applies a pair a switch accepted.</summary>
public interface ICultureState : INotifyStateChanged
{
    /// <summary>The culture that formats dates, numbers and collation.</summary>
    CultureInfo Culture { get; }

    /// <summary>The culture that chooses strings and direction.</summary>
    CultureInfo UICulture { get; }

    /// <summary>Whether the UI culture is written right to left.</summary>
    bool IsRightToLeft { get; }

    /// <summary>The revision of the state, advanced once per change after the change's cultures are in place.</summary>
    /// <remarks>The revision is published with release semantics and read with acquire semantics, as <see cref="Volatile"/> does, so a thread that reads a revision reads the cultures of that change or of a later one.</remarks>
    int Revision { get; }

    /// <summary>The version of the accepted pair the state last applied.</summary>
    CultureVersion Version { get; }
}
