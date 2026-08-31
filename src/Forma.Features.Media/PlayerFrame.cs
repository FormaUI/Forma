namespace Forma.Features.Media;

using System.Diagnostics.CodeAnalysis;

/// <summary>What a player loads, and the frame it loads into.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public abstract record PlayerFrame(string Source);

/// <summary>A player page supplied by a hosting service.</summary>
public sealed record EmbeddedPlayerFrame(string Source) : PlayerFrame(Source);

/// <summary>A file the browser plays itself.</summary>
public sealed record NativePlayerFrame(string Source) : PlayerFrame(Source);

/// <summary>Markup a hosting service's own script turns into a player, sized by the service rather than the frame.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public sealed record HtmlPlayerFrame(string Source, string Html, string ScriptUrl) : PlayerFrame(Source);
