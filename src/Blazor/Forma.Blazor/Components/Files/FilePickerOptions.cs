namespace Forma.Blazor.Components.Files;

/// <summary>The files a picker accepts: their types, their size, and how many one selection may carry.</summary>
public sealed record FilePickerOptions
{
    /// <summary>The types a file must match, in the HTML <c>accept</c> syntax, where null accepts any.</summary>
    public string? Accept { get; init; }

    /// <summary>Whether more than one file may be selected or dropped at once.</summary>
    public bool AllowMultipleFiles { get; init; } = true;

    /// <summary>The largest a single file may be, in bytes, where null allows any size.</summary>
    public long? MaxFileSize { get; init; }

    /// <summary>The most files one selection may carry, where null allows any number.</summary>
    public int? MaxFiles { get; init; }

    /// <summary>The default file picker options.</summary>
    public static FilePickerOptions Default { get; } = new();
}
