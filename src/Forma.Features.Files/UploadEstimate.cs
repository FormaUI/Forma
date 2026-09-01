namespace Forma.Features.Files;

/// <summary>Projects how long a transfer's remainder needs from what it has moved so far.</summary>
internal static class UploadEstimate
{
    /// <summary>The time the remaining bytes need at the observed rate, or null while that cannot be said.</summary>
    internal static TimeSpan? Of(long transferred, long total, TimeSpan elapsed)
    {
        var rate = elapsed.TotalSeconds > 0 ? transferred / elapsed.TotalSeconds : 0;
        var remaining = total - transferred;

        return rate > 0 && remaining > 0 ? TimeSpan.FromSeconds(remaining / rate) : null;
    }
}
