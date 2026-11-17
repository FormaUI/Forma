namespace Forma.Framework.Localization.Json;

using Microsoft.Extensions.Logging;

/// <summary>The log messages JSON localization writes.</summary>
internal static partial class JsonLocalizationLog
{
    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "{FileName} and {OtherFileName} give '{Key}' of {TypeName} different texts in {Culture}; the text of {FileName} is shown.")]
    public static partial void EntryConflict(ILogger logger, string fileName, string otherFileName, string key, string typeName, string culture);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "No locale entries are available for assembly {Assembly} in culture {Culture} or its applicable fallback cultures, found looking up '{Key}' of {Type}.")]
    public static partial void AssemblyNotLocalized(ILogger logger, string assembly, string culture, string key, string type);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "No locale entries are available for type {Type} of {Assembly} in culture {Culture} or its applicable fallback cultures, found looking up '{Key}'.")]
    public static partial void TypeNotLocalized(ILogger logger, string type, string assembly, string culture, string key);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "'{Key}' of {Type} in {Assembly} has no entry in culture {Culture} or its applicable fallback cultures, so the key is shown.")]
    public static partial void KeyNotLocalized(ILogger logger, string key, string type, string assembly, string culture);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "{Type} of {Assembly} is left unchecked in culture {Culture}, since a locale file it reads there could not be read: {Reason}")]
    public static partial void LocaleFileNotRead(ILogger logger, string type, string assembly, string culture, string reason);
}
