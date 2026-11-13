namespace Forma.Framework.Files;

/// <summary>Where one file has reached in an upload.</summary>
public enum UploadState
{
    /// <summary>Queued, with nothing sent yet.</summary>
    Pending,

    /// <summary>Currently crossing to the store.</summary>
    Uploading,

    /// <summary>Stored successfully.</summary>
    Completed,

    /// <summary>Stopped by an error.</summary>
    Failed,

    /// <summary>Turned away by the store, which said why.</summary>
    Refused,

    /// <summary>Passed over after an earlier file in the batch failed or was refused.</summary>
    Skipped,

    /// <summary>Stored, then removed after another file in the batch failed or was refused.</summary>
    RolledBack,

    /// <summary>Abandoned when the upload was canceled.</summary>
    Canceled
}
