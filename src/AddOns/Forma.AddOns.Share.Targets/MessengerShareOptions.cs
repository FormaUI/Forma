namespace Forma.Framework.Share.Targets;

/// <summary>The options for sharing through Messenger's send dialog.</summary>
public sealed class MessengerShareOptions
{
    /// <summary>The host's Facebook app id.</summary>
    public string? AppId { get; set; }

    /// <summary>The host's address the dialog returns to.</summary>
    public Uri? RedirectAddress { get; set; }
}
