namespace Forma.Framework.Composer.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

/// <summary>One placed widget, its content, its settings and its children.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The page designer this belongs to is not built yet, so nothing constructs it.")]
internal sealed record ComposerWidgetNode
{
    private readonly JsonElement _settings;

    /// <summary>This placement's identity, stable across edits.</summary>
    public required string InstanceId { get; init; }

    /// <summary>The widget identifier of the kind placed here.</summary>
    public required string WidgetId { get; init; }

    /// <summary>The content the author edits on the canvas.</summary>
    public string? Content { get; init; }

    /// <summary>The settings the settings panel edits.</summary>
    public JsonElement Settings
    {
        get => _settings;

        // Cloned so the element outlives the JsonDocument it was borrowed from.
        init => _settings = value.ValueKind == JsonValueKind.Undefined ? value : value.Clone();
    }

    /// <summary>The widgets nested in this one.</summary>
    public IReadOnlyList<ComposerWidgetNode> Children { get; init; } = [];
}
