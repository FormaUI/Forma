namespace Forma.Framework.Themes;

using Forma.Framework.Events;

/// <summary>The theme choice a user scope draws, announced to its subscribers as it changes, which changes only as the state's integration applies a choice a switch accepted.</summary>
public interface IThemeState : INotifyStateChanged
{
    /// <summary>The choice in force, naming a registered theme.</summary>
    ThemeChoice Choice { get; }

    /// <summary>The revision of the state, advanced once per change after the change's choice is in place.</summary>
    int Revision { get; }

    /// <summary>The version of the accepted choice the state last applied.</summary>
    ThemeVersion Version { get; }
}
