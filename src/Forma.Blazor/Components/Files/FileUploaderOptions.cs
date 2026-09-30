namespace Forma.Blazor.Components.Files;

/// <summary>How an uploader runs a set of files, and which measurements it shows beside each one.</summary>
public sealed record FileUploaderOptions
{
    /// <summary>The default file uploader options, showing the progress bar and state alone.</summary>
    public static FileUploaderOptions Default { get; } = new();

    /// <summary>The options that show every measurement.</summary>
    public static FileUploaderOptions Detailed { get; } = new()
    {
        ShowByteCount = true,
        ShowElapsed = true,
        ShowTransferRate = true,
        ShowEstimate = true,
        ShowTotalMetrics = true
    };

    /// <summary>Whether the upload begins as soon as a new set of files arrives.</summary>
    public bool AutoStart { get; init; } = true;

    /// <summary>Whether a failed file removes everything the set already stored.</summary>
    public bool RollbackOnFailure { get; init; }

    /// <summary>Whether the upload stops at the first failed file.</summary>
    public bool StopOnFailure { get; init; }

    /// <summary>Whether the uploader shows how much of each file has reached the store, against its total.</summary>
    public bool ShowByteCount { get; init; }

    /// <summary>Whether the uploader shows how long the set has left at the rate it is moving.</summary>
    public bool ShowEstimate { get; init; }

    /// <summary>Whether the uploader shows how long the transfer has been running.</summary>
    public bool ShowElapsed { get; init; }

    /// <summary>Whether the uploader shows how fast bytes are reaching the store.</summary>
    public bool ShowTransferRate { get; init; }

    /// <summary>Whether the uploader shows the measurements for the whole set of files beneath the list.</summary>
    public bool ShowTotalMetrics { get; init; }

    /// <summary>Whether a live region beneath the set reports how far it has reached.</summary>
    public bool ShowSummary { get; init; } = true;
}
