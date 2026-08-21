namespace Forma.UI.Features.Composer.Widgets;

using System.Text.Json;

/// <summary>One placed widget, its content, its settings and its children.</summary>
public sealed record ComposerWidgetNode
{
    private readonly JsonElement _settings;

    /// <summary>This placement's identity, stable across edits.</summary>
    public required string InstanceId { get; init; }

    /// <summary>The widget identifier of the kind placed here.</summary>
    public required string WidgetId { get; init; }

    /// <summary>What the author edits on the canvas, or null when the widget has none.</summary>
    public string? Content { get; init; }

    /// <summary>What the settings panel edits.</summary>
    public JsonElement Settings
    {
        get => _settings;

        // Cloned because an element borrowed from a JsonDocument dies with it, and a node outlives the parse.
        init => _settings = value.ValueKind == JsonValueKind.Undefined ? value : value.Clone();
    }

    /// <summary>Nested widgets, empty unless the widget is a container.</summary>
    public IReadOnlyList<ComposerWidgetNode> Children { get; init; } = [];
}
