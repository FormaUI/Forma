namespace Forma.UI.Blazor.Components.Files;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Style.Glyphs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Moves picked files into an <see cref="IFileStore"/>, reporting progress for each.</summary>
public sealed partial class FormaFileUploader : ComponentBase, IDisposable
{
    // A percentage alone would move a byte counter in steps of one percent, which is ten megabytes in a gigabyte.
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(200);

    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>The files to store; assigning a different list starts a new upload.</summary>
    [Parameter] public IReadOnlyList<IBrowserFile>? Files { get; set; }

    /// <summary>Raised once the set finishes, with the resources every stored file became.</summary>
    [Parameter] public EventCallback<IReadOnlyList<ResourceInfo>> OnCompleted { get; set; }

    /// <summary>Raised per file that could not be stored, leaving the rest of the set to continue.</summary>
    [Parameter] public EventCallback<UploadFailure> OnFailed { get; set; }

    /// <summary>How the set runs and which transfer measurements are shown beside each file.</summary>
    [Parameter] public FormaFileUploaderOptions Options { get; set; } = FormaFileUploaderOptions.Default;

    /// <summary>Where the bytes are written; defaults to the store the host registered.</summary>
    [Parameter] public IFileStore? Store { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [Inject] private IStringLocale<FormaFileUploader> L { get; set; } = default!;

    private IFileStore _activeStore = default!;

    private CancellationTokenSource? _cancellation;

    private IReadOnlyList<IBrowserFile>? _currentFiles;

    private bool _disposed;

    private long _lastProgressAt;

    private TimeSpan? _setDuration;

    private long _setStartedAt;

    private readonly List<UploadFile> _uploads = [];

    private Task _uploadTask = Task.CompletedTask;

    /// <summary>Whether a set is in flight right now.</summary>
    public bool IsUploading
        => _cancellation is not null;

    private bool CanStart
        => !IsUploading && _uploads.Exists(upload => upload.State == UploadState.Pending);

    private bool ShowsMetrics
        => Options.ShowByteCount || ShowsTimeMetrics;

    // Byte counts ride along with progress, while the rest only move as the clock does.
    private bool ShowsTimeMetrics
        => Options.ShowElapsed || Options.ShowTransferRate || Options.ShowEstimate;

    private TimeSpan SetElapsed
        => _setDuration ?? (_setStartedAt == 0 ? TimeSpan.Zero : Stopwatch.GetElapsedTime(_setStartedAt));

    private long TotalBytes
        => _uploads.Sum(upload => upload.Size);

    private long TransferredBytes
        => _uploads.Sum(upload => upload.Transferred);

    private TimeSpan? Estimate
        => EstimateOf(TransferredBytes, TotalBytes, SetElapsed);

    private bool ShowsDuration
        => Options.ShowElapsed || Options.ShowEstimate;

    private string TotalByteCount
        => ByteCountOf(TransferredBytes, TotalBytes);

    private string TotalRate
        => ResourceFormatter.FromTransferRate(RateOf(TransferredBytes, SetElapsed));

    private string TotalTime
    {
        get
        {
            if (Options.ShowEstimate && IsUploading && Estimate is { } estimate)
            {
                var remaining = ResourceFormatter.FromDuration(estimate);

                return L[$"{remaining} left"];
            }

            return Options.ShowElapsed ? ResourceFormatter.FromDuration(SetElapsed) : string.Empty;
        }
    }

    private string Summary
    {
        get
        {
            if (_uploads.Count == 0)
            {
                return string.Empty;
            }

            var total = _uploads.Count;

            if (IsUploading)
            {
                var started = _uploads.Count(upload => upload.State > UploadState.Pending);

                return L[$"Uploading {started} of {total}."];
            }

            var stored = _uploads.Count(upload => upload.State == UploadState.Completed);

            return L[$"Stored {stored} of {total}."];
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentNullException.ThrowIfNull(Options);

        _activeStore = Store
            ?? Services.GetService<IFileStore>()
            ?? throw new InvalidOperationException($"{nameof(FormaFileUploader)} needs an {nameof(IFileStore)}, either through its {nameof(Store)} parameter or registered with the host.");

        if (ReferenceEquals(Files, _currentFiles))
        {
            return;
        }

        _currentFiles = Files;

        // A set arriving mid-flight replaces the one running, so wait for that loop to unwind before reusing the list.
        Cancel();
        await _uploadTask;

        _uploads.Clear();
        _setStartedAt = 0;
        _setDuration = null;

        if (Files is not null)
        {
            _uploads.AddRange(Files.Select(file => new UploadFile(file.Name, file.Size)));
        }

        if (Options.AutoStart)
        {
            await StartAsync();
        }
    }

    /// <summary>Abandons the set in flight, leaving whatever already stored in place.</summary>
    public void Cancel()
        => _cancellation?.Cancel();

    /// <summary>Stores every pending file in turn; does nothing while a set is already in flight.</summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsUploading || Files is null || _uploads.Count == 0)
        {
            return Task.CompletedTask;
        }

        _uploadTask = UploadAsync(Files, cancellationToken);

        return _uploadTask;
    }

    private static Glyph StateGlyph(UploadState state)
        => state switch
        {
            UploadState.Completed => Glyph.Check,
            UploadState.Failed => Glyph.TriangleExclamation,
            UploadState.Skipped => Glyph.Forward,
            UploadState.RolledBack => Glyph.RotateLeft,
            UploadState.Cancelled => Glyph.Ban,
            UploadState.Uploading => Glyph.ArrowUp,
            _ => Glyph.Clock
        };

    private static double RateOf(long bytes, TimeSpan elapsed)
        => elapsed.TotalSeconds > 0 ? bytes / elapsed.TotalSeconds : 0;

    private static TimeSpan? EstimateOf(long transferred, long total, TimeSpan elapsed)
    {
        var rate = RateOf(transferred, elapsed);
        var remaining = total - transferred;

        return rate > 0 && remaining > 0 ? TimeSpan.FromSeconds(remaining / rate) : null;
    }

    private void ReportProgress(int index, long totalRead)
    {
        var upload = _uploads[index];
        var percent = upload.Size == 0 ? 100 : (int)Math.Min(totalRead * 100 / upload.Size, 100);

        if (percent == upload.Percent && !ByteCountDue())
        {
            return;
        }

        _lastProgressAt = Stopwatch.GetTimestamp();

        // Progress arrives on whichever thread the store's read continuation resumed on, and the renderer enumerates this list.
        _ = InvokeAsync(() =>
        {
            _uploads[index] = _uploads[index] with { Percent = percent, Transferred = totalRead };
            Rerender();
        });
    }

    private bool ByteCountDue()
        => Options.ShowByteCount && Stopwatch.GetElapsedTime(_lastProgressAt) >= ProgressInterval;

    /// <summary>How much of one file has reached the store, against its total.</summary>
    private string FileByteCount(UploadFile upload)
        => upload.State == UploadState.Pending
            ? ResourceFormatter.FromByteCount(upload.Size)
            : ByteCountOf(upload.Transferred, upload.Size);

    /// <summary>How fast one file is reaching the store, or nothing while it waits its turn.</summary>
    private static string FileRate(UploadFile upload)
        => upload.State == UploadState.Pending
            ? string.Empty
            : ResourceFormatter.FromTransferRate(RateOf(upload.Transferred, upload.Elapsed));

    /// <summary>What one file has left to run while it is moving, and how long it took once it stopped.</summary>
    private string FileTime(UploadFile upload)
    {
        if (upload.State == UploadState.Pending)
        {
            return string.Empty;
        }

        if (Options.ShowEstimate
            && upload.State == UploadState.Uploading
            && EstimateOf(upload.Transferred, upload.Size, upload.Elapsed) is { } estimate)
        {
            var remaining = ResourceFormatter.FromDuration(estimate);

            return L[$"{remaining} left"];
        }

        return Options.ShowElapsed ? ResourceFormatter.FromDuration(upload.Elapsed) : string.Empty;
    }

    // A transfer that reached its total is just that size; repeating it as "3 MB of 3 MB" says nothing.
    private string ByteCountOf(long transferred, long total)
    {
        if (transferred >= total)
        {
            return ResourceFormatter.FromByteCount(total);
        }

        var sent = ResourceFormatter.FromByteCount(transferred);
        var size = ResourceFormatter.FromByteCount(total);

        return L[$"{sent} of {size}"];
    }

    // Measurements move with the clock rather than with progress, so a stalled transfer keeps counting.
    private async Task TickAsync()
    {
        using var timer = new PeriodicTimer(TickInterval);

        try
        {
            while (!_disposed && IsUploading && await timer.WaitForNextTickAsync())
            {
                await InvokeAsync(Rerender);
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task UploadAsync(IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken)
    {
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _setStartedAt = Stopwatch.GetTimestamp();
        _setDuration = null;

        var token = _cancellation.Token;
        var failures = new List<UploadFailure>();
        var stored = new List<(int Index, ResourceInfo Resource)>();
        var anyFailed = false;

        if (ShowsTimeMetrics)
        {
            _ = TickAsync();
        }

        try
        {
            for (var index = 0; index < _uploads.Count && index < files.Count; index++)
            {
                if (token.IsCancellationRequested)
                {
                    MarkPendingFrom(index, UploadState.Cancelled);

                    break;
                }

                var outcome = await StoreFileAsync(index, files[index], failures, stored, token);

                if (outcome == StoreOutcome.Cancelled)
                {
                    MarkPendingFrom(index + 1, UploadState.Cancelled);

                    break;
                }

                if (outcome == StoreOutcome.Failed)
                {
                    anyFailed = true;

                    if (Options.StopOnFailure)
                    {
                        MarkPendingFrom(index + 1, UploadState.Skipped);

                        break;
                    }
                }
            }

            if (anyFailed && Options.RollbackOnFailure && stored.Count > 0)
            {
                await RollbackAsync(stored, failures);
                stored.Clear();
            }
        }
        finally
        {
            _setDuration = Stopwatch.GetElapsedTime(_setStartedAt);
            _cancellation?.Dispose();
            _cancellation = null;
            Rerender();
        }

        foreach (var failure in failures)
        {
            await OnFailed.InvokeAsync(failure);
        }

        if (stored.Count > 0)
        {
            await OnCompleted.InvokeAsync(stored.Select(entry => entry.Resource).ToList());
        }
    }

    // Rollback deliberately ignores the set's token: a half-undone set is worse than one that takes a moment longer.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task RollbackAsync(List<(int Index, ResourceInfo Resource)> stored, List<UploadFailure> failures)
    {
        foreach (var (index, resource) in stored)
        {
            try
            {
                if (!await _activeStore.DeleteAsync(resource, CancellationToken.None))
                {
                    throw new InvalidOperationException($"The store did not remove {resource.Name}.");
                }

                SetUploadState(index, UploadState.RolledBack);
            }
            catch (Exception error)
            {
                SetUploadState(index, UploadState.Failed);
                failures.Add(new UploadFailure(resource.Name, error));
            }
        }
    }

    // The set keeps unwinding after disposal, by which point the renderer is gone.
    private void Rerender()
    {
        if (!_disposed)
        {
            StateHasChanged();
        }
    }

    private void SetUploadState(int index, UploadState state, int? percent = null)
    {
        var upload = _uploads[index];
        var starting = state == UploadState.Uploading;

        _uploads[index] = upload with
        {
            State = state,
            Percent = percent ?? upload.Percent,
            StartedAt = starting ? Stopwatch.GetTimestamp() : upload.StartedAt,
            Duration = starting || upload.StartedAt == 0 ? null : Stopwatch.GetElapsedTime(upload.StartedAt)
        };

        Rerender();
    }

    private string StateText(UploadState state)
        => state switch
        {
            UploadState.Completed => L["Stored"],
            UploadState.Failed => L["Failed"],
            UploadState.Skipped => L["Skipped"],
            UploadState.RolledBack => L["Rolled Back"],
            UploadState.Cancelled => L["Cancelled"],
            UploadState.Uploading => L["Uploading"],
            _ => L["Waiting"]
        };

    private void MarkPendingFrom(int startIndex, UploadState state)
    {
        for (var index = startIndex; index < _uploads.Count; index++)
        {
            if (_uploads[index].State == UploadState.Pending)
            {
                _uploads[index] = _uploads[index] with { State = state };
            }
        }

        Rerender();
    }

    // A store runs host code that may throw anything, and one file failing must not abandon the rest.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task<StoreOutcome> StoreFileAsync(
        int index,
        IBrowserFile file,
        List<UploadFailure> failures,
        List<(int Index, ResourceInfo Resource)> stored,
        CancellationToken cancellationToken)
    {
        SetUploadState(index, UploadState.Uploading, 0);

        try
        {
            // The file's own size is the ceiling, so no caller has to supply one and none can be too small.
            await using var content = new UploadFileStream(file.OpenReadStream(file.Size, cancellationToken), read => ReportProgress(index, read));

            stored.Add((index, await _activeStore.SaveAsync(new FileContent(file.Name, file.ContentType, file.Size, content), cancellationToken)));
            SetUploadState(index, UploadState.Completed, 100);

            return StoreOutcome.Stored;
        }
        catch (OperationCanceledException)
        {
            SetUploadState(index, UploadState.Cancelled);

            return StoreOutcome.Cancelled;
        }
        catch (Exception error)
        {
            SetUploadState(index, UploadState.Failed);
            failures.Add(new UploadFailure(file.Name, error));

            return StoreOutcome.Failed;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
    }

    private enum StoreOutcome
    {
        Stored,
        Failed,
        Cancelled
    }

    private sealed record UploadFile(string Name, long Size)
    {
        /// <summary>How long the transfer ran, once it is no longer running.</summary>
        public TimeSpan? Duration { get; init; }

        /// <summary>How much of the file has reached the store, as a whole percentage.</summary>
        public int Percent { get; init; }

        /// <summary>When the transfer began, as a stopwatch timestamp.</summary>
        public long StartedAt { get; init; }

        /// <summary>Where this file has reached.</summary>
        public UploadState State { get; init; }

        /// <summary>How many of the file's bytes have reached the store.</summary>
        public long Transferred { get; init; }

        /// <summary>How long the transfer has been running, or ran in total.</summary>
        public TimeSpan Elapsed
            => Duration ?? (StartedAt == 0 ? TimeSpan.Zero : Stopwatch.GetElapsedTime(StartedAt));
    }
}
