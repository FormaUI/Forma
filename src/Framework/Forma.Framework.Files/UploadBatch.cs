namespace Forma.Framework.Files;

using System.Diagnostics;

/// <summary>The files of one upload run as a whole: its clock, its aggregates, and each file's place in it.</summary>
public sealed class UploadBatch
{
    private readonly List<UploadFile> _files = [];
    private long _startTimestamp;
    private TimeSpan? _duration;

    /// <summary>The files in the batch, in the order they upload.</summary>
    public IReadOnlyList<UploadFile> Files
        => _files;

    /// <summary>How many files the batch holds.</summary>
    public int Count
        => _files.Count;

    /// <summary>The file at the given position.</summary>
    public UploadFile this[int index]
        => _files[index];

    /// <summary>Whether any file still waits its turn.</summary>
    public bool HasPending
        => _files.Exists(file => file.State == UploadState.Pending);

    /// <summary>How many files have started.</summary>
    public int StartedCount
        => _files.Count(file => file.State > UploadState.Pending);

    /// <summary>How many files reached the store.</summary>
    public int CompletedCount
        => _files.Count(file => file.State == UploadState.Completed);

    /// <summary>How many bytes the whole batch holds.</summary>
    public long TotalBytes
        => _files.Sum(file => file.TotalBytes);

    /// <summary>How many of the batch's bytes have reached the store.</summary>
    public long BytesTransferred
        => _files.Sum(file => file.BytesTransferred);

    /// <summary>How long the run has been going, or ran in total.</summary>
    public TimeSpan Elapsed
        => _duration ?? (_startTimestamp == 0 ? TimeSpan.Zero : Stopwatch.GetElapsedTime(_startTimestamp));

    /// <summary>How fast the batch's bytes are moving, in bytes per second.</summary>
    public double BytesPerSecond
        => Elapsed.TotalSeconds > 0 ? BytesTransferred / Elapsed.TotalSeconds : 0;

    /// <summary>How long the rest of the batch needs at the current rate.</summary>
    public TimeSpan? EstimatedTimeRemaining
        => TransferTimeEstimator.EstimateRemainingTime(BytesTransferred, TotalBytes, Elapsed);

    /// <summary>Empties the batch and resets its clock.</summary>
    public void Clear()
    {
        _files.Clear();
        _startTimestamp = 0;
        _duration = null;
    }

    /// <summary>Adds a file waiting to upload.</summary>
    public void Add(string name, long size)
        => _files.Add(new UploadFile(name, size));

    /// <summary>Starts the run's clock.</summary>
    public void BeginUpload()
    {
        _startTimestamp = Stopwatch.GetTimestamp();
        _duration = null;
    }

    /// <summary>Stops the run's clock.</summary>
    public void EndUpload()
        => _duration = _startTimestamp == 0 ? TimeSpan.Zero : Stopwatch.GetElapsedTime(_startTimestamp);

    /// <summary>Records how far one file's transfer has come.</summary>
    public void UpdateFileProgress(int index, int percent, long bytesTransferred)
        => _files[index] = _files[index] with { ProgressPercentage = percent, BytesTransferred = bytesTransferred };

    /// <summary>Moves one file to a state.</summary>
    /// <remarks>Entering <see cref="UploadState.Uploading"/> starts the file's clock, and a later state stops it.</remarks>
    public void SetFileState(int index, UploadState state, int? percent = null)
    {
        var file = _files[index];
        var isStarting = state == UploadState.Uploading;

        _files[index] = file with
        {
            State = state,
            ProgressPercentage = percent ?? file.ProgressPercentage,
            StartTimestamp = isStarting ? Stopwatch.GetTimestamp() : file.StartTimestamp,
            Duration = isStarting || file.StartTimestamp == 0 ? null : Stopwatch.GetElapsedTime(file.StartTimestamp)
        };
    }

    /// <summary>Moves every file still pending, from the given position on, to a state.</summary>
    public void TransitionPendingFiles(int startIndex, UploadState state)
    {
        for (var index = startIndex; index < _files.Count; index++)
        {
            if (_files[index].State == UploadState.Pending)
            {
                _files[index] = _files[index] with { State = state };
            }
        }
    }
}
