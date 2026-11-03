namespace Forma.Framework.Culture.Browser.Interop;

/// <summary>Where a switch keeps the pair the page's culture coordinator accepts.</summary>
public enum CultureSwitchPersistence
{
    /// <summary>In the culture states alone, so the next server render is in the request's culture.</summary>
    State,

    /// <summary>In the culture cookie as well, which the coordinator writes and reads back before it accepts the switch.</summary>
    Cookie,
}
