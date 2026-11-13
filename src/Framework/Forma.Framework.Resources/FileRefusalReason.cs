namespace Forma.Framework.Resources;

/// <summary>Why a store refused a file.</summary>
public enum FileRefusalReason
{
    /// <summary>The file's type is outside the types the store accepts.</summary>
    DisallowedType,

    /// <summary>The file is larger than the most bytes the store takes in one file.</summary>
    TooLarge,

    /// <summary>The image is wider than the most pixels the store takes across.</summary>
    TooWide,

    /// <summary>The image is taller than the most pixels the store takes down.</summary>
    TooTall
}
