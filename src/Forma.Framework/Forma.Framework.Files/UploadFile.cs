namespace Forma.Framework.Files;

using System.Diagnostics;

/// <summary>One file moving into a store, with how far its transfer has come.</summary>
/// <param name="Name">The file's name.</param>
/// <param name="TotalBytes">How many bytes the file holds.</param>
public sealed record UploadFile(string Name, long TotalBytes)
{
    /// <summary>How long the finished transfer ran.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>How much of the file has reached the store, as a whole percentage.</summary>
    public int ProgressPercentage { get; init; }

    /// <summary>When the transfer began, as a stopwatch timestamp.</summary>
    public long StartTimestamp { get; init; }

    /// <summary>Where this file has reached.</summary>
    public UploadState State { get; init; }

    /// <summary>How many of the file's bytes have reached the store.</summary>
    public long BytesTransferred { get; init; }

    /// <summary>How long the transfer has been running, or ran in total.</summary>
    public TimeSpan Elapsed
        => Duration ?? (StartTimestamp == 0 ? TimeSpan.Zero : Stopwatch.GetElapsedTime(StartTimestamp));

    /// <summary>How fast the bytes are moving, in bytes per second.</summary>
    public double BytesPerSecond
        => Elapsed.TotalSeconds > 0 ? BytesTransferred / Elapsed.TotalSeconds : 0;

    /// <summary>How long the rest of the file needs at the current rate.</summary>
    public TimeSpan? EstimatedTimeRemaining
        => TransferTimeEstimator.EstimateRemainingTime(BytesTransferred, TotalBytes, Elapsed);
}
