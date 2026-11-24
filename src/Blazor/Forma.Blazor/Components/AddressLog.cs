namespace Forma.Blazor.Components;

using Forma.Framework.Sanitization;

using Microsoft.Extensions.Logging;

/// <summary>The messages a component writes for an address it writes inert.</summary>
internal static partial class AddressLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "An address of the scheme {Scheme} was written inert for {RuntimeId}, since a {Kind} doesn't allow it.")]
    public static partial void AddressWrittenInert(ILogger logger, string? scheme, AddressKind kind, Guid runtimeId);
}
