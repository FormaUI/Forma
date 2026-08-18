namespace Forma.UI.Blazor.Components.Files;

/// <summary>Where one file has reached in an upload.</summary>
public enum UploadState
{
    /// <summary>Queued, with nothing sent yet.</summary>
    Pending,

    /// <summary>Currently crossing to the store.</summary>
    Uploading,

    /// <summary>Stored successfully.</summary>
    Completed,

    /// <summary>Stopped by an error, leaving the rest of the set to continue.</summary>
    Failed,

    /// <summary>Never attempted, because an earlier file failed and the set stops on failure.</summary>
    Skipped,

    /// <summary>Stored, then removed again because another file in the set failed.</summary>
    RolledBack,

    /// <summary>Abandoned because the upload was cancelled.</summary>
    Cancelled
}
