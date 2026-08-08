namespace Forma.Blazor.Components.Media;

/// <summary>What a player loads, and the frame it loads into.</summary>
internal abstract record PlayerFrame(string Source);

/// <summary>A player page supplied by a hosting service.</summary>
internal sealed record EmbeddedPlayerFrame(string Source) : PlayerFrame(Source);

/// <summary>A file the browser plays itself.</summary>
internal sealed record NativePlayerFrame(string Source) : PlayerFrame(Source);

/// <summary>Markup a hosting service's own script turns into a player, sized by the service rather than the frame.</summary>
internal sealed record HtmlPlayerFrame(string Source, string Html, string ScriptUrl) : PlayerFrame(Source);
