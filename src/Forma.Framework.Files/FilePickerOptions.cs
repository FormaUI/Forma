namespace Forma.Framework.Files;

/// <summary>The files a picker accepts: their types, their size, and how many one selection may carry.</summary>
public sealed record FilePickerOptions
{
    /// <summary>HTML `accept` syntax (MIME types, wildcards, or extensions) a file must match; unset accepts anything.</summary>
    public string? Accept { get; init; }

    /// <summary>Whether more than one file may be selected or dropped at once.</summary>
    public bool AllowMultipleFiles { get; init; } = true;

    /// <summary>The largest a single file may be, in bytes; unset allows any size.</summary>
    public long? MaxFileSize { get; init; }

    /// <summary>The most files one selection may carry; unset allows any number.</summary>
    public int? MaxFiles { get; init; }

    /// <summary>Options accepting any number of files, of any type and size.</summary>
    public static FilePickerOptions Default { get; } = new();
}
