namespace Forma.UI.Components.Files;

/// <summary>How a <see cref="FormaFileUploader"/> runs a set, and which measurements it shows beside each file.</summary>
public sealed record FormaFileUploaderOptions
{
    /// <summary>The progress bar and state alone, with no measurements.</summary>
    public static FormaFileUploaderOptions Default { get; } = new();

    /// <summary>Every measurement at once.</summary>
    public static FormaFileUploaderOptions Detailed { get; } = new()
    {
        ShowByteCount = true,
        ShowElapsed = true,
        ShowTransferRate = true,
        ShowEstimate = true,
        ShowTotalMetrics = true
    };

    /// <summary>Whether the upload begins as soon as a new set of files arrives.</summary>
    public bool AutoStart { get; init; } = true;

    /// <summary>Removes everything the set already stored when any file in it fails.</summary>
    public bool RollbackOnFailure { get; init; }

    /// <summary>Leaves the files after the first failure unattempted instead of storing them.</summary>
    public bool StopOnFailure { get; init; }

    /// <summary>Shows how much of the file has reached the store, against its total.</summary>
    public bool ShowByteCount { get; init; }

    /// <summary>Shows how long the set has left at the rate it is currently moving.</summary>
    public bool ShowEstimate { get; init; }

    /// <summary>Shows how long the transfer has been running.</summary>
    public bool ShowElapsed { get; init; }

    /// <summary>Shows how fast bytes are reaching the store.</summary>
    public bool ShowTransferRate { get; init; }

    /// <summary>Repeats the measurements for the set of files beneath the list, once it holds more than one file.</summary>
    public bool ShowTotalMetrics { get; init; }

    /// <summary>Reports how far the set of files has reached in a live region beneath it.</summary>
    public bool ShowSummary { get; init; } = true;
}
