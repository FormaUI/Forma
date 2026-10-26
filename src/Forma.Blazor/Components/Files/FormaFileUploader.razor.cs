namespace Forma.Blazor.Components.Files;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Css;
using Forma.Framework.Files;
using Forma.Framework.Icons;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>An uploader that stores picked files in an <see cref="IFileStore"/>, showing the progress of each.</summary>
/// <remarks>A <see cref="Files"/> list that is another instance than the last cancels the set in flight and replaces it.</remarks>
/// <param name="registeredStore">The store the host registered.</param>
public sealed partial class FormaFileUploader(IFileStore? registeredStore = null) : FormaElementBase
{
    // How often the byte count moves between one whole percent and the next.
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(200);

    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>The files to store.</summary>
    [Parameter] public IReadOnlyList<IBrowserFile>? Files { get; set; }

    /// <summary>The callback invoked once the set finishes, with the resources of the stored files.</summary>
    [Parameter] public EventCallback<IReadOnlyList<ResourceInfo>> OnCompleted { get; set; }

    /// <summary>The callback invoked with each file that failed to store.</summary>
    [Parameter] public EventCallback<UploadFailure> OnFailed { get; set; }

    /// <summary>How the set runs and which transfer measurements are shown beside each file.</summary>
    [Parameter] public FileUploaderOptions Options { get; set; } = FileUploaderOptions.Default;

    /// <summary>The store the files are written to, where null uses the store the host registered.</summary>
    [Parameter] public IFileStore? Store { get; set; }

    private readonly IFileStore? _registeredStore = registeredStore;

    private IFileStore _activeStore = default!;

    private CancellationTokenSource? _cancellation;

    private IReadOnlyList<IBrowserFile>? _currentFiles;

    private long _lastProgressTimestamp;

    private readonly UploadBatch _batch = new();

    private Task _uploadTask = Task.CompletedTask;

    /// <summary>Whether a set is in flight.</summary>
    public bool IsUploading
        => _cancellation is not null;

    private bool CanStart
        => !IsUploading && _batch.HasPending;

    private bool ShowsMetrics
        => Options.ShowByteCount || ShowsTimeMetrics;

    // Byte counts ride along with progress, while the rest only move as the clock does.
    private bool ShowsTimeMetrics
        => Options.ShowElapsed || Options.ShowTransferRate || Options.ShowEstimate;

    private bool ShowsDuration
        => Options.ShowElapsed || Options.ShowEstimate;

    private string TotalByteCount
        => GetByteCount(_batch.BytesTransferred, _batch.TotalBytes);

    private string TotalRate
        => ResourceFormatter.FromTransferRate(_batch.BytesPerSecond, CultureState.Culture);

    private string TotalTime
    {
        get
        {
            if (Options.ShowEstimate && IsUploading && _batch.EstimatedTimeRemaining is { } estimate)
            {
                var remaining = ResourceFormatter.FromDuration(estimate, CultureState.Culture);

                return L["{Remaining} left", remaining];
            }

            return Options.ShowElapsed ? ResourceFormatter.FromDuration(_batch.Elapsed, CultureState.Culture) : string.Empty;
        }
    }

    private string Summary
    {
        get
        {
            if (_batch.Count == 0)
            {
                return string.Empty;
            }

            if (IsUploading)
            {
                return L["Uploading {Started} of {Total}.", _batch.StartedCount, _batch.Count];
            }

            return L["Stored {Stored} of {Total}.", _batch.CompletedCount, _batch.Count];
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentNullException.ThrowIfNull(Options);

        _activeStore = Store
            ?? _registeredStore
            ?? throw new InvalidOperationException($"{nameof(FormaFileUploader)} needs an {nameof(IFileStore)}, either through its {nameof(Store)} parameter or registered with the host.");

        if (ReferenceEquals(Files, _currentFiles))
        {
            return;
        }

        _currentFiles = Files;

        // A set arriving mid-flight replaces the one running, so wait for that loop to unwind before reusing the list.
        Cancel();
        await _uploadTask;

        _batch.Clear();

        if (Files is not null)
        {
            foreach (var file in Files)
            {
                _batch.Add(file.Name, file.Size);
            }
        }

        if (Options.AutoStart)
        {
            await StartAsync();
        }
    }

    /// <summary>Cancels the set in flight.</summary>
    public void Cancel()
        => _cancellation?.Cancel();

    /// <summary>Stores every pending file in turn.</summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsUploading || Files is null || _batch.Count == 0)
        {
            return Task.CompletedTask;
        }

        _uploadTask = UploadAsync(Files, cancellationToken);

        return _uploadTask;
    }

    private static Glyph GetStateGlyph(UploadState state)
        => state switch
        {
            UploadState.Completed => Glyph.Check,
            UploadState.Failed => Glyph.Warning,
            UploadState.Skipped => Glyph.FastForward,
            UploadState.RolledBack => Glyph.Undo,
            UploadState.Canceled => Glyph.Prohibited,
            UploadState.Uploading => Glyph.ArrowUp,
            _ => Glyph.Clock
        };

    private void ReportProgress(int index, long totalRead)
    {
        var upload = _batch[index];
        var percent = upload.TotalBytes == 0 ? 100 : (int)Math.Min(totalRead * 100 / upload.TotalBytes, 100);

        if (percent == upload.ProgressPercentage && !IsByteCountDue())
        {
            return;
        }

        _lastProgressTimestamp = Stopwatch.GetTimestamp();

        // Progress arrives on whichever thread the store's read continuation resumed on, and the renderer enumerates the set.
        _ = InvokeAsync(() =>
        {
            _batch.UpdateFileProgress(index, percent, totalRead);
            Rerender();
        });
    }

    private bool IsByteCountDue()
        => Options.ShowByteCount && Stopwatch.GetElapsedTime(_lastProgressTimestamp) >= ProgressInterval;

    /// <summary>How much of one file has reached the store, against its total.</summary>
    private string FormatFileByteCount(UploadFile upload)
        => upload.State == UploadState.Pending
            ? ResourceFormatter.FromByteCount(upload.TotalBytes, CultureState.Culture)
            : GetByteCount(upload.BytesTransferred, upload.TotalBytes);

    /// <summary>How fast one file is reaching the store.</summary>
    private string FormatFileRate(UploadFile upload)
        => upload.State == UploadState.Pending
            ? string.Empty
            : ResourceFormatter.FromTransferRate(upload.BytesPerSecond, CultureState.Culture);

    /// <summary>What one file has left to run while it is moving, and how long it took once it stopped.</summary>
    private string FormatFileTime(UploadFile upload)
    {
        if (upload.State == UploadState.Pending)
        {
            return string.Empty;
        }

        if (Options.ShowEstimate
            && upload.State == UploadState.Uploading
            && upload.EstimatedTimeRemaining is { } estimate)
        {
            var remaining = ResourceFormatter.FromDuration(estimate, CultureState.Culture);

            return L["{Remaining} left", remaining];
        }

        return Options.ShowElapsed ? ResourceFormatter.FromDuration(upload.Elapsed, CultureState.Culture) : string.Empty;
    }

    private string GetByteCount(long transferred, long total)
    {
        if (transferred >= total)
        {
            return ResourceFormatter.FromByteCount(total, CultureState.Culture);
        }

        var sent = ResourceFormatter.FromByteCount(transferred, CultureState.Culture);
        var size = ResourceFormatter.FromByteCount(total, CultureState.Culture);

        return L["{Sent} of {Size}", sent, size];
    }

    // Measurements move with the clock rather than with progress, so a stalled transfer keeps counting.
    private async Task TickAsync()
    {
        using var timer = new PeriodicTimer(TickInterval);

        try
        {
            while (!IsDisposed && IsUploading && await timer.WaitForNextTickAsync())
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
        _batch.BeginUpload();

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
            for (var index = 0; index < _batch.Count && index < files.Count; index++)
            {
                if (token.IsCancellationRequested)
                {
                    MarkPendingFrom(index, UploadState.Canceled);

                    break;
                }

                var outcome = await StoreFileAsync(index, files[index], failures, stored, token);

                if (outcome == StoreOutcome.Canceled)
                {
                    MarkPendingFrom(index + 1, UploadState.Canceled);

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
            _batch.EndUpload();
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

    // Rollback runs to the end whatever the set's token, so no set is left half undone.
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
        if (!IsDisposed)
        {
            StateHasChanged();
        }
    }

    private void SetUploadState(int index, UploadState state, int? percent = null)
    {
        _batch.SetFileState(index, state, percent);
        Rerender();
    }

    private string GetStateText(UploadState state)
        => state switch
        {
            UploadState.Completed => L["Stored"],
            UploadState.Failed => L["Failed"],
            UploadState.Skipped => L["Skipped"],
            UploadState.RolledBack => L["Rolled Back"],
            UploadState.Canceled => L["Canceled"],
            UploadState.Uploading => L["Uploading"],
            _ => L["Waiting"]
        };

    private void MarkPendingFrom(int startIndex, UploadState state)
    {
        _batch.TransitionPendingFiles(startIndex, state);
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
            // The file's own size is the read ceiling.
            await using var content = new UploadFileStream(file.OpenReadStream(file.Size, cancellationToken), read => ReportProgress(index, read));

            stored.Add((index, await _activeStore.SaveAsync(new FileContent(file.Name, file.ContentType, file.Size, content), cancellationToken)));
            SetUploadState(index, UploadState.Completed, 100);

            return StoreOutcome.Stored;
        }
        catch (OperationCanceledException)
        {
            SetUploadState(index, UploadState.Canceled);

            return StoreOutcome.Canceled;
        }
        catch (Exception error)
        {
            SetUploadState(index, UploadState.Failed);
            failures.Add(new UploadFailure(file.Name, error));

            return StoreOutcome.Failed;
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-file-uploader");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_cancellation is not null)
        {
            await _cancellation.CancelAsync();
        }

        _cancellation?.Dispose();
        _cancellation = null;
        await base.DisposeAsyncCore();
    }

    private enum StoreOutcome
    {
        Stored,
        Failed,
        Canceled
    }
}
