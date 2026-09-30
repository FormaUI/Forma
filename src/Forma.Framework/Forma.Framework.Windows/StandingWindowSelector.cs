namespace Forma.Framework.Windows;

/// <summary>The selector of one of the workspace's standing selections, such as <see cref="WindowSelector.All"/>.</summary>
internal sealed record StandingWindowSelector(StandingWindowSelection Selection) : IWindowSelector;
