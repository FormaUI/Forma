namespace Forma.Framework.Windows;

/// <summary>The selector behind <see cref="WindowSelector.All"/> and its siblings.</summary>
internal sealed record StandingWindowSelector(StandingWindowSelection Selection) : IWindowSelector;
