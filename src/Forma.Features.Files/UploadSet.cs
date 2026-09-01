namespace Forma.Features.Files;

using System.Diagnostics;

/// <summary>The files of one upload run as a whole: its clock, its aggregates, and each file's place in it.</summary>
public sealed class UploadSet
{
    private readonly List<UploadFile> _files = [];
    private long _startedAt;
    private TimeSpan? _duration;

    /// <summary>The files in the set, in the order they upload.</summary>
    public IReadOnlyList<UploadFile> Files
        => _files;

    /// <summary>How many files the set holds.</summary>
    public int Count
        => _files.Count;

    /// <summary>The file at the given position.</summary>
    public UploadFile this[int index]
        => _files[index];

    /// <summary>Whether any file still waits its turn.</summary>
    public bool HasPending
        => _files.Exists(file => file.State == UploadState.Pending);

    /// <summary>How many files have begun, wherever they ended up.</summary>
    public int StartedCount
        => _files.Count(file => file.State > UploadState.Pending);

    /// <summary>How many files reached the store.</summary>
    public int CompletedCount
        => _files.Count(file => file.State == UploadState.Completed);

    /// <summary>How many bytes the whole set holds.</summary>
    public long TotalBytes
        => _files.Sum(file => file.Size);

    /// <summary>How many of the set's bytes have reached the store.</summary>
    public long TransferredBytes
        => _files.Sum(file => file.Transferred);

    /// <summary>How long the run has been going, or ran in total.</summary>
    public TimeSpan Elapsed
        => _duration ?? (_startedAt == 0 ? TimeSpan.Zero : Stopwatch.GetElapsedTime(_startedAt));

    /// <summary>How fast the set's bytes are moving, in bytes per second.</summary>
    public double Rate
        => Elapsed.TotalSeconds > 0 ? TransferredBytes / Elapsed.TotalSeconds : 0;

    /// <summary>How long the rest of the set needs at the current rate, or null while that cannot be said.</summary>
    public TimeSpan? Estimate
        => UploadEstimate.Of(TransferredBytes, TotalBytes, Elapsed);

    /// <summary>Empties the set and stops its clock, ready for the next run.</summary>
    public void Reset()
    {
        _files.Clear();
        _startedAt = 0;
        _duration = null;
    }

    /// <summary>Adds a file waiting to upload.</summary>
    public void Add(string name, long size)
        => _files.Add(new UploadFile(name, size));

    /// <summary>Starts the run's clock.</summary>
    public void Begin()
    {
        _startedAt = Stopwatch.GetTimestamp();
        _duration = null;
    }

    /// <summary>Stops the run's clock, freezing what it read.</summary>
    public void End()
        => _duration = _startedAt == 0 ? TimeSpan.Zero : Stopwatch.GetElapsedTime(_startedAt);

    /// <summary>Records how far one file's transfer has come.</summary>
    public void Report(int index, int percent, long transferred)
        => _files[index] = _files[index] with { Percent = percent, Transferred = transferred };

    /// <summary>Moves one file to a state, starting its clock on entry to uploading and freezing it on leaving.</summary>
    public void Transition(int index, UploadState state, int? percent = null)
    {
        var file = _files[index];
        var starting = state == UploadState.Uploading;

        _files[index] = file with
        {
            State = state,
            Percent = percent ?? file.Percent,
            StartedAt = starting ? Stopwatch.GetTimestamp() : file.StartedAt,
            Duration = starting || file.StartedAt == 0 ? null : Stopwatch.GetElapsedTime(file.StartedAt)
        };
    }

    /// <summary>Moves every file still pending, from the given position on, to a state.</summary>
    public void MarkPendingFrom(int startIndex, UploadState state)
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
