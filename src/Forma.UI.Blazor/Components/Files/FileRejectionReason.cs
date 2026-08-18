namespace Forma.UI.Blazor.Components.Files;

/// <summary>Why <see cref="FormaFilePicker"/> declined a file.</summary>
public enum FileRejectionReason
{
    /// <summary>The file's type does not match <see cref="FormaFilePicker.Accept"/>.</summary>
    DisallowedType,

    /// <summary>The file is larger than <see cref="FormaFilePicker.MaxFileSize"/>.</summary>
    TooLarge,

    /// <summary>The selection carried more files than <see cref="FormaFilePicker.MaxSelectedFiles"/> allows.</summary>
    TooManyFiles
}
