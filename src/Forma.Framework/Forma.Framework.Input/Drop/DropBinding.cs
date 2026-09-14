namespace Forma.Framework.Input.Drop;

/// <summary>A drop binding, matching by the kinds of data the drag carries.</summary>
public sealed record DropBinding : InputBinding
{
    /// <summary>Any drop, whatever it carries.</summary>
    public static DropBinding Any { get; } = new();

    /// <summary>A drop of files.</summary>
    public static DropBinding Files { get; } = new() { Data = ["Files"] };

    /// <summary>The data kinds of which the drag must carry at least one, as the drag's data transfer names them; <see langword="null"/> matches any.</summary>
    public IReadOnlyList<string>? Data { get; init; }

    /// <summary>Whether the binding matches the drag passing over rather than the drop.</summary>
    public bool Over { get; init; }

    /// <inheritdoc />
    public override string Type
        => Over ? "dragover" : "drop";
}
