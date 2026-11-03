namespace Forma.Framework.Files;

/// <summary>Why a picker declined a file.</summary>
public enum FileRejectionReason
{
    /// <summary>The file's type is outside the types the picker accepts.</summary>
    DisallowedType,

    /// <summary>The file is larger than the picker's size limit.</summary>
    TooLarge,

    /// <summary>The selection carried more files than the picker allows at once.</summary>
    TooManyFiles
}
