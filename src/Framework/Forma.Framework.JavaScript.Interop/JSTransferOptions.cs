namespace Forma.Framework.JavaScript.Interop;

/// <summary>What a receiver with no maximum of its own accepts through the transfer.</summary>
public sealed record JSTransferOptions
{
    /// <summary>The options a host that configures none gets: 16 MiB.</summary>
    public static JSTransferOptions Default { get; } = new();

    /// <summary>The size, in bytes, a receiver with no maximum of its own accepts.</summary>
    public long MaximumLength { get; init; } = 16 * 1024 * 1024;
}
