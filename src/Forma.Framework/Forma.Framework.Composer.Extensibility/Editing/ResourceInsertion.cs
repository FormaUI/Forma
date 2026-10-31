namespace Forma.Framework.Composer.Editing;

using System.Diagnostics.CodeAnalysis;

/// <summary>A stored file to put into the document, and how it is put there.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record ResourceInsertion : ComposerInsertion
{
    /// <summary>The source address of the file.</summary>
    [SuppressMessage("Design", "CA1056:URI properties should not be strings")]
    public required string Source { get; init; }

    /// <summary>The name of the file.</summary>
    public required string Name { get; init; }

    /// <summary>The kind of the file: <c>image</c>, <c>video</c>, <c>audio</c>, or <c>file</c>.</summary>
    public required string Kind { get; init; }

    /// <summary>Whether the file is embedded where it is inserted.</summary>
    public required bool Embed { get; init; }

    /// <summary>Whether a link to the file is inserted.</summary>
    public required bool Link { get; init; }
}
