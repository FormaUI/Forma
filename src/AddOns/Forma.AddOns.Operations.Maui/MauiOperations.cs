namespace Forma.Framework.Browser.Interop.Operations.Maui;

using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

/// <summary>Saves a file and shares an address through the platform's share sheet, on Android, iOS and Mac Catalyst, whose web views do neither.</summary>
/// <param name="share">The platform's share sheet.</param>
/// <param name="fileSystem">The application's file system, whose cache holds a file while it is shared.</param>
/// <param name="deviceInfo">The device the application runs on.</param>
public sealed class MauiOperations(IShare share, IFileSystem fileSystem, IDeviceInfo deviceInfo) : INativeOperations
{
    // Windows' web view saves, prints and shares by itself.
    private bool IsNative
        => deviceInfo.Platform != DevicePlatform.WinUI;

    /// <inheritdoc />
    public bool CanSave
        => IsNative;

    /// <inheritdoc />
    public bool CanShare
        => IsNative;

    /// <inheritdoc />
    /// <remarks>
    /// The file is written to a folder of its own in the application's cache and handed to the share sheet, from which the reader keeps, sends or
    /// prints it. Each save first deletes the folders the saves before it wrote, so the cache holds the last file alone.
    /// </remarks>
    public async Task<bool> TrySaveAsync(string name, string content, string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        if (!IsNative)
        {
            return false;
        }

        var root = Path.Combine(fileSystem.CacheDirectory, "forma");
        DeleteSaves(root);

        var folder = Directory.CreateDirectory(Path.Combine(root, Guid.CreateVersion7().ToString("N")));
        var path = Path.Combine(folder.FullName, Path.GetFileName(name));

        // The share sheet is presented on the thread the web view's components run on, the application's main thread.
        await File.WriteAllTextAsync(path, content).ConfigureAwait(true);
        await share.RequestAsync(new ShareFileRequest { Title = name, File = new ShareFile(path, type) }).ConfigureAwait(true);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryShareAsync(string title, string address)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        if (!IsNative)
        {
            return false;
        }

        await share.RequestAsync(new ShareTextRequest { Title = title, Uri = address }).ConfigureAwait(true);

        return true;
    }

    // A folder a share target still holds open stays for a later save to delete.
    private static void DeleteSaves(string root)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var folder in Directory.EnumerateDirectories(root))
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
