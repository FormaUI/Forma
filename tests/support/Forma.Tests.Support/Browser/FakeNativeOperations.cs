namespace Forma.Tests.Browser;

using Forma.Framework.Browser.Interop.Operations;

/// <summary>A host's native operations that take or decline what they're handed, as a test names, recording each file and address.</summary>
/// <param name="takes">Whether the host takes each file and address.</param>
/// <param name="canShare">Whether the host shares through its own share surface.</param>
/// <param name="canSave">Whether the host keeps a file through its own surface.</param>
public sealed class FakeNativeOperations(bool takes = true, bool canShare = true, bool canSave = true) : INativeOperations
{
    /// <summary>The files the host was handed, in order.</summary>
    public List<(string Name, string Content, string Type)> Saved { get; } = [];

    /// <summary>The addresses the host was handed, in order.</summary>
    public List<(string Title, string Address)> Shared { get; } = [];

    /// <inheritdoc />
    public bool CanSave
        => canSave;

    /// <inheritdoc />
    public bool CanShare
        => canShare;

    /// <inheritdoc />
    public Task<bool> TrySaveAsync(string name, string content, string type)
    {
        Saved.Add((name, content, type));

        return Task.FromResult(takes);
    }

    /// <inheritdoc />
    public Task<bool> TryShareAsync(string title, string address)
    {
        Shared.Add((title, address));

        return Task.FromResult(takes);
    }
}
