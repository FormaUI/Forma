namespace Forma.Framework.Composer.Widgets;

using Forma.Framework.Icons;

/// <summary>Describes a widget available to a designer.</summary>
public sealed record ComposerWidget
{
    /// <summary>The unique identifier of the widget kind.</summary>
    public required string Id { get; init; }

    /// <summary>The widget's name, in English, used as the localization key.</summary>
    public required string Title { get; init; }

    /// <summary>The palette group the widget is listed under.</summary>
    public required string Category { get; init; }

    /// <summary>The icon shown in the palette.</summary>
    public required Icon Icon { get; init; }

    /// <summary>The component rendering the widget on the canvas.</summary>
    public required Type Component { get; init; }

    /// <summary>The component editing the widget's settings, or null when it has none.</summary>
    public Type? SettingsComponent { get; init; }

    /// <summary>The record the widget's settings deserialize to, or null when it has none.</summary>
    public Type? SettingsType { get; init; }

    /// <summary>Whether the widget may contain other widgets.</summary>
    public bool IsContainer { get; init; }
}
