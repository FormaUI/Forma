namespace Forma.UI.Features.Share;

/// <summary>The kinds of thing a share menu acts on, deciding which baseline actions apply.</summary>
public static class ShareItemKind
{
    /// <summary>A page or article living at an address: the networks, email and copying the link apply.</summary>
    public const string Link = "link";

    /// <summary>A file: downloading and saving apply; nobody posts a file to a network.</summary>
    public const string File = "file";

    /// <summary>A picture living at an address: the networks apply, and so does a board that pins media.</summary>
    public const string Image = "image";

    /// <summary>A person or organization: email and downloading a card apply.</summary>
    public const string Contact = "contact";
}
