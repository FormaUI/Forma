# Forma.Framework.Files

The types `Forma.Blazor`'s file components report and track: a file a picker declined and why, a
file an uploader could not store and the error that stopped it, and the progress of each file in
an upload. A host names them in the handlers it gives `FormaFilePicker`, `FormaImagePicker` and
`FormaFileUploader`.

The package comes with `formaui-net.Blazor`, and its types are in the
`Forma.Framework.Files` namespace. The store an uploader writes to and the resource each file
becomes are the contracts of
[`Forma.Framework.Resources`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Resources/README.md).

## What the components report

| Type | What it is |
| --- | --- |
| `RejectedFile` | The `Name` of a file a picker declined and the `Reason` it declined it. |
| `FileRejectionReason` | `DisallowedType`, a type outside the picker's `Accept`; `TooLarge`, over its `MaxFileSize`; `TooManyFiles`, beyond its `MaxFiles`. |
| `UploadFailure` | The `Name` of a file whose upload failed and the `Error` that stopped it, thrown by the store while saving it, or while removing it again in a rollback. |

A picker raises `OnFilesRejected` with its rejections and `OnFilesSelected` with the files it
accepted. `FormaFileUploader` raises `OnFailed` once for each failure and `OnCompleted` with the
`ResourceInfo` every stored file became.

```razor
@using Forma.Blazor.Components.Files
@using Forma.Framework.Files
@using Forma.Framework.Resources
@using Microsoft.AspNetCore.Components.Forms

<FormaFilePicker Options="_pickerOptions"
                 OnFilesSelected="HandleSelected"
                 OnFilesRejected="HandleRejected" />

<FormaFileUploader Files="_files"
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

## Upload progress

`FormaFileUploader` tracks each run in an `UploadBatch`, the files of one upload in the order they
upload, and draws a row for each `UploadFile` in it.

| Member | What it is |
| --- | --- |
| `UploadBatch.Files`, `Count`, the indexer | The files in the run. |
| `UploadBatch.HasPending`, `StartedCount`, `CompletedCount` | Whether any file still waits; how many have started; how many were stored. |
| `UploadBatch.TotalBytes`, `BytesTransferred` | The bytes in the run and the bytes stored so far. |
| `UploadBatch.Elapsed`, `BytesPerSecond`, `EstimatedTimeRemaining` | The run's duration so far, its rate, and the time the rest needs at that rate, or `null` while unknown. |
| `UploadBatch.Add(name, size)`, `Clear()` | Adds a waiting file; empties the run and resets its clock. |
| `UploadBatch.BeginUpload()`, `EndUpload()` | Starts and stops the run's clock. |
| `UploadBatch.SetFileState(index, state, percent)` | Moves one file to a state; entering `Uploading` starts its clock and a later state stops it. |
| `UploadBatch.UpdateFileProgress(index, percent, transferred)` | Records how far one file's transfer has come. |
| `UploadBatch.TransitionPendingFiles(startIndex, state)` | Moves every waiting file from a position on to a state. |
| `UploadFile` | One file's `Name`, `TotalBytes`, `State`, `ProgressPercentage`, `BytesTransferred`, `Elapsed`, `BytesPerSecond` and `EstimatedTimeRemaining`. |

| `UploadState` | What it means |
| --- | --- |
| `Pending` | Waiting its turn. |
| `Uploading` | Moving to the store. |
| `Completed` | Stored. |
| `Failed` | Stopped by an error. |
| `Skipped` | Passed over because an earlier file failed under `FileUploaderOptions.StopOnFailure`. |
| `RolledBack` | Stored, then removed because another file failed under `FileUploaderOptions.RollbackOnFailure`. |
| `Canceled` | Abandoned when the upload was canceled. |

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
