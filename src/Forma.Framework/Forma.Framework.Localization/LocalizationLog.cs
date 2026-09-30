namespace Forma.Framework.Localization;

using Microsoft.Extensions.Logging;

/// <summary>The log messages localization writes.</summary>
internal static partial class LocalizationLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Plural argument {Argument} of type {ArgumentType} in {Format} is not numeric; using zero.")]
    public static partial void PluralArgumentNotNumeric(ILogger logger, string argument, string argumentType, string format);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Locale pack {Pack} could not be loaded, so what it localizes reads in the language its keys are written in: {Reason}")]
    public static partial void LocalePackNotLoaded(ILogger logger, string pack, string reason);
}
