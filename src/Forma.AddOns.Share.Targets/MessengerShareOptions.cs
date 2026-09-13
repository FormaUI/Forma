namespace Forma.Framework.Share.Targets;

/// <summary>What the host configures for sharing through Messenger's send dialog.</summary>
public sealed class MessengerShareOptions
{
    /// <summary>The host's Facebook app id.</summary>
    public string? AppId { get; set; }

    /// <summary>The host's address the dialog returns to once the message is sent or dismissed.</summary>
    public Uri? RedirectAddress { get; set; }
}
