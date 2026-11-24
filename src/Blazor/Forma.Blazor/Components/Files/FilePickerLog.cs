namespace Forma.Blazor.Components.Files;

using Microsoft.Extensions.Logging;

/// <summary>The messages the file picker writes for what it refuses.</summary>
internal static partial class FilePickerLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "A file the browser chose in {RuntimeId} carries no name, no type or a size no file has, and was left out.")]
    public static partial void FileRefused(ILogger logger, Guid runtimeId);
}
