namespace Forma.Framework.Culture.Blazor.WebView;

using Microsoft.AspNetCore.Components.Routing;

/// <summary>Reads the navigations a web view makes to follow the application's culture.</summary>
public static class LocationChangingContextExtensions
{
    // The history entry state the culture coordinator's script gives an address it replaces to follow the application's culture.
    private const string FollowMark = "forma-culture";

    /// <summary>Whether the navigation replaces the web view's address with the same page in the route of the pair the application accepted, which keeps the page and the user's work, so a handler guarding the work lets it go on.</summary>
    /// <param name="context">The navigation a location-changing handler receives.</param>
    public static bool IsCultureFollowing(this LocationChangingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return string.Equals(context.HistoryEntryState, FollowMark, StringComparison.Ordinal);
    }
}
