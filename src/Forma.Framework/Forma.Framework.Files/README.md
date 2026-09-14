# Forma.Framework.Files

What `Forma.Blazor`'s file components report and track: the file a picker declined and why,
the file an uploader could not store and what stopped it, and the state of each file in an
upload run — how far it is, how fast it moves, how long it has left. `FormaFilePicker` and
`FormaImagePicker` hand a host the `IBrowserFile`s they accepted and a `RejectedFile` for each
they declined; `FormaFileUploader` moves accepted files into the host's `IFileStore`, tracking
them in an `UploadBatch`, and reports the `ResourceInfo` each one became or an `UploadFailure`
for each it could not store. The store and the resource are the contracts of
[`Forma.Framework.Resources`](../Forma.Framework.Resources/README.md).

The assembly ships inside `2Site.Forma.Blazor`; nothing is installed or registered for it. Its
types live in the `Forma.Framework.Files` namespace.

## Rejections

| Type | What it is |
| --- | --- |
| `RejectedFile` | The `Name` of a file the picker declined and the `Reason` it was. |
| `FileRejectionReason` | `DisallowedType`, a type not among those the picker accepts; `TooLarge`, larger than its size limit; `TooManyFiles`, a selection carrying more files than it allows at once. |

A picker's own `FilePickerOptions` — `Accept`, `MaxFileSize`, `MaxFiles` and `AllowMultipleFiles`
— set those limits. The picker lists each rejection beneath its drop zone and raises
`OnFilesRejected` with them; the files that passed arrive through `OnFilesSelected`.

## Failures

`UploadFailure` carries the `Name` of a file whose upload did not complete and the `Error` that
stopped it: what the store threw while saving it, or, when the uploader rolls back, what went
wrong removing a file already stored. `FormaFileUploader` raises `OnFailed` once per failure and
`OnCompleted` once with the resources every stored file became; its own `FileUploaderOptions` say
whether a failure stops the rest of the set (`StopOnFailure`) and whether it undoes what was
stored before it (`RollbackOnFailure`).

## An upload run

`UploadBatch` is the set of files of one upload run, in the order they upload, with the run's
clock and its aggregates:

| Member | What it is |
| --- | --- |
| `Files`, `Count`, the indexer | The `UploadFile`s in the set. |
| `HasPending`, `StartedCount`, `CompletedCount` | Whether any file still waits its turn; how many have begun; how many reached the store. |
| `TotalBytes`, `BytesTransferred` | How many bytes the set holds and how many have reached the store. |
| `Elapsed`, `BytesPerSecond`, `EstimatedTimeRemaining` | How long the run has been going or ran; its rate; how long the rest needs at that rate, or null while that cannot be said. |
| `Add(name, size)`, `Clear()` | Adds a file waiting to upload; empties the set and stops its clock. |
| `BeginUpload()`, `EndUpload()` | Starts the run's clock; stops it, freezing what it read. |
| `UpdateFileProgress(index, percent, transferred)` | Records how far one file's transfer has come. |
| `SetFileState(index, state, percent)` | Moves one file to a state, starting its clock on entering `Uploading` and freezing it on leaving. |
| `TransitionPendingFiles(startIndex, state)` | Moves every file still pending, from a position on, to a state. |

Each `UploadFile` is a record of `Name` and `TotalBytes` carrying its `State`,
`ProgressPercentage`, `BytesTransferred`, `StartTimestamp` as a stopwatch timestamp, its
`Duration` once it stopped, and its own `Elapsed`, `BytesPerSecond` and
`EstimatedTimeRemaining`.

`UploadState` is where one file has reached:

| State | What it means |
| --- | --- |
| `Pending` | Queued, with nothing sent yet. |
| `Uploading` | Crossing to the store. |
| `Completed` | Stored. |
| `Failed` | Stopped by an error, leaving the rest of the set to continue. |
| `Skipped` | Never attempted, because an earlier file failed and the set stops on failure. |
| `RolledBack` | Stored, then removed again because another file in the set failed. |
| `Canceled` | Abandoned because the upload was canceled. |

`FormaFileUploader` keeps one batch per run and draws it as a row per file — state, progress
bar, and whichever measurements its `FileUploaderOptions` turn on — with totals beneath the
list and a summary in a live region.

## Example

A picker feeding an uploader, with every outcome handled. The uploader writes through the
`IFileStore` the host registered, or the one its `Store` parameter names.

```razor
@using Forma.Blazor.Components.Files
@using Forma.Framework.Files
@using Forma.Framework.Resources
@using Microsoft.AspNetCore.Components.Forms

<FormaFilePicker Options="_pickerOptions"
                 OnFilesSelected="HandleSelected"
                 OnFilesRejected="HandleRejected" />

<FormaFileUploader Files="_files"
                   Options="FileUploaderOptions.Detailed"
                   OnCompleted="HandleCompleted"
                   OnFailed="HandleFailed" />

<ul role="status">
    @foreach (var note in _notes)
    {
        <li>@note</li>
    }
</ul>

@code {
    private readonly FilePickerOptions _pickerOptions = new()
    {
        Accept = ".pdf",
        MaxFileSize = 10 * 1024 * 1024,
        MaxFiles = 5,
    };

    private readonly List<string> _notes = [];
    private IReadOnlyList<IBrowserFile>? _files;

    private void HandleSelected(IReadOnlyList<IBrowserFile> files)
    {
        _notes.Clear();
        _files = files;
    }

    private void HandleRejected(IReadOnlyList<RejectedFile> rejections)
    {
        foreach (var rejection in rejections)
        {
            _notes.Add(rejection.Reason switch
            {
                FileRejectionReason.DisallowedType => $"{rejection.Name} is not a PDF.",
                FileRejectionReason.TooLarge => $"{rejection.Name} is over 10 MB.",
                _ => $"{rejection.Name} was left out; five at a time.",
            });
        }
    }

    private void HandleCompleted(IReadOnlyList<ResourceInfo> resources)
    {
        foreach (var resource in resources)
        {
            _notes.Add($"Stored at {resource.Source}.");
        }
    }

    private void HandleFailed(UploadFailure failure)
        => _notes.Add($"{failure.Name} could not be stored: {failure.Error.Message}");
}
```

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
