namespace Forma.Blazor.Components.Bars;

using Forma.Framework.Css;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>A quiet strip of toolbar items, such as status readouts and commands.</summary>
public sealed partial class FormaStatusStrip : FormaElementBase
{
    /// <summary>The accessible name of the strip.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The items the strip renders.</summary>
    [Parameter] public IReadOnlyList<ToolbarItem> Items { get; set; } = [];

    /// <summary>The callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>Whether the strip's items are disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>What the strip renders while it has no items.</summary>
    [Parameter] public StatusStripEmptyMode EmptyMode { get; set; }

    /// <summary>Whether the strip is hidden while it stays mounted.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Whether the strip animates between hidden and shown.</summary>
    [Parameter] public bool Animated { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Items.Count > 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-status-strip");
}
