namespace Forma.Tests.Resources;

using System.Globalization;

using Forma.Framework.Resources;

/// <summary>A file store that records the files it saves and deletes, and can fail one named file, refuse the files it is given refusals for, or decline every delete.</summary>
public sealed class FakeFileStore(string? failOn = null, bool deletes = true) : IFileStore
{
    private readonly List<string> _deleted = [];

    private readonly List<string> _saved = [];

    /// <summary>The content read back from every file the store accepted, keyed by name.</summary>
    public Dictionary<string, string> Contents { get; } = [];

    /// <summary>The refusal the store answers each named file with.</summary>
    public Dictionary<string, FileRefusal> Refusals { get; } = [];

    /// <summary>The sources the store deleted, in the order it deleted them.</summary>
    public IReadOnlyList<string> Deleted
        => _deleted;

    /// <summary>The names of the files the store accepted, in the order it accepted them.</summary>
    public IReadOnlyList<string> Saved
        => _saved;

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(ResourceInfo resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (!deletes)
        {
            return ValueTask.FromResult(false);
        }

        _deleted.Add(resource.Source);

        return ValueTask.FromResult(true);
    }

    /// <inheritdoc />
    public async ValueTask<FileSaveResult> SaveAsync(FileContent content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Name == failOn)
        {
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"The store could not write {content.Name}."));
        }

        if (Refusals.TryGetValue(content.Name, out var refusal))
        {
            return FileSaveResult.Refused(refusal);
        }

        using var reader = new StreamReader(content.Stream);

        Contents[content.Name] = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        _saved.Add(content.Name);

        return FileSaveResult.Saved(new ResourceInfo($"/stored/{content.Name}") { Size = content.Size });
    }
}
