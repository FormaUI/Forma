namespace Forma.Framework.Browser.Interop;

/// <summary>A capability's registrations: the entry they are made through, the name the entry knows them by, and how their options reach the browser.</summary>
/// <typeparam name="TOptions">The options a registration of the capability takes.</typeparam>
/// <param name="Root">The entry the registrations are made through.</param>
/// <param name="Name">The capability's name in the entry.</param>
/// <param name="DescribeOptions">Turns options into the shape the browser reads.</param>
internal sealed record DomCapability<TOptions>(DomRoot Root, string Name, Func<TOptions, object> DescribeOptions)
    where TOptions : notnull
{
    /// <summary>Whether a registration with the given options declares routes on the input router.</summary>
    public Func<TOptions, bool> DeclaresRoutes { get; init; } = _ => false;
}
