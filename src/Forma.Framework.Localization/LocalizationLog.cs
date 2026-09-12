namespace Forma.Framework.Localization;

using Microsoft.Extensions.Logging;

/// <summary>The messages localization writes for the fallbacks it takes.</summary>
internal static partial class LocalizationLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Plural argument {Argument} of type {ArgumentType} in {Format} is not numeric; using zero.")]
    public static partial void PluralArgumentNotNumeric(ILogger logger, string argument, string argumentType, string format);
}
