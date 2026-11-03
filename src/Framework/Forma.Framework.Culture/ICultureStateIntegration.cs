namespace Forma.Framework.Culture;

/// <summary>Applies the pairs a switch accepted to the culture state a user scope holds.</summary>
public interface ICultureStateIntegration
{
    /// <summary>Applies <paramref name="accepted"/> to the state and to the calling flow, and notifies every subscriber where the state changed.</summary>
    /// <remarks>
    /// A version the state has applied already, or an earlier one of the same document, changes nothing. A later version of the pair the
    /// state holds records the version and applies the pair to the calling flow, without a notification or a new revision. The state and
    /// the calling flow hold the cultures once the method returns, and only the notification completes later.
    /// </remarks>
    /// <param name="accepted">The pair a switch accepted, with its version.</param>
    /// <returns>A task that completes once every subscriber has been notified.</returns>
    Task ApplyAsync(AcceptedCulture accepted);
}
