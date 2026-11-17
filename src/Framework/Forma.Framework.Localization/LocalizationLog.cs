namespace Forma.Framework.Localization;

using Microsoft.Extensions.Logging;

/// <summary>The log messages localization writes.</summary>
internal static partial class LocalizationLog
{
    [LoggerMessage(EventId = 1, Message = "Plural argument {Argument} of type {ArgumentType} in {Format} is not numeric; using zero.")]
    public static partial void PluralArgumentNotNumeric(ILogger logger, LogLevel level, string argument, string argumentType, string format);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Locale pack {Pack} could not be loaded, so what it localizes reads in the language its keys are written in: {Reason}")]
    public static partial void LocalePackNotLoaded(ILogger logger, string pack, string reason);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Host locale assembly {Assembly} could not be loaded, so the host's strings it carries are left out: {Reason}")]
    public static partial void HostLocaleAssemblyNotLoaded(ILogger logger, string assembly, string reason);
}
