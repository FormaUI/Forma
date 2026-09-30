namespace Forma.Framework.Share.Targets;

using Forma.Framework.Icons;

/// <summary>Opens Messenger's send dialog on the item's address, under the host's Facebook app.</summary>
public sealed class MessengerShare : LinkShare
{
    private readonly string _appId;
    private readonly Uri _redirectAddress;

    /// <summary>Creates a Messenger share target under the host's Facebook app.</summary>
    /// <param name="appId">The host's Facebook app id.</param>
    /// <param name="redirectAddress">The host's address the dialog returns to.</param>
    public MessengerShare(string appId, Uri redirectAddress)
        : base("messenger", "Messenger", Brand.Messenger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);
        ArgumentNullException.ThrowIfNull(redirectAddress);

        _appId = appId;
        _redirectAddress = redirectAddress;
    }

    /// <inheritdoc />
    public override string? GetAddress(ShareItem item)
        => EscapeAddress(item) is { } address
            ? $"https://www.facebook.com/dialog/send?app_id={Uri.EscapeDataString(_appId)}&link={address}&redirect_uri={Uri.EscapeDataString(_redirectAddress.ToString())}"
            : null;
}
