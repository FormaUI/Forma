namespace Forma.Blazor.Components.Explorer;

using Microsoft.AspNetCore.Components.Forms;

/// <summary>Whether the upload panel is open, and the files picked there awaiting upload.</summary>
internal sealed class ExplorerUploadPanel
{
    /// <summary>Whether the panel is shown.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>The files picked for upload, or null until a selection is made.</summary>
    public IReadOnlyList<IBrowserFile>? PickedFiles { get; private set; }

    /// <summary>Shows or hides the panel.</summary>
    public void Toggle()
        => IsOpen = !IsOpen;

    /// <summary>Records the files picked for upload.</summary>
    public void Pick(IReadOnlyList<IBrowserFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        PickedFiles = files;
    }

    /// <summary>Hides the panel and forgets any picked files.</summary>
    public void Close()
    {
        IsOpen = false;
        PickedFiles = null;
    }
}
