namespace Forma.Framework.Localization.Json;

using Microsoft.Extensions.Logging;

/// <summary>The log messages JSON localization writes.</summary>
internal static partial class JsonLocalizationLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "{FileName} and {OtherFileName} give '{Key}' of {TypeName} different texts in {Culture}; the text of {FileName} is shown.")]
    public static partial void TranslationConflict(ILogger logger, string fileName, string otherFileName, string key, string typeName, string culture);
}
