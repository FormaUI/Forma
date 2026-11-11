namespace Forma.Framework.Files;

/// <summary>Projects how long a transfer's remainder needs from what it has moved so far.</summary>
internal static class TransferTimeEstimator
{
    /// <summary>The time the remaining bytes need at the observed rate.</summary>
    internal static TimeSpan? EstimateRemainingTime(long bytesTransferred, long totalBytes, TimeSpan elapsedTime)
    {
        var rate = elapsedTime.TotalSeconds > 0 ? bytesTransferred / elapsedTime.TotalSeconds : 0;
        var remainingBytes = totalBytes - bytesTransferred;

        return rate > 0 && remainingBytes > 0 ? TimeSpan.FromSeconds(remainingBytes / rate) : null;
    }
}
