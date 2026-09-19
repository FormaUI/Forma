namespace Forma.Blazor.Components.Bars;

using Forma.Framework.Css;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>A quiet strip of status readouts and occasional commands, meant to sit beneath a canvas or at the foot of a panel.</summary>
public sealed partial class FormaStatusStrip : FormaElementBase
{
    /// <summary>The accessible name of the strip; required only when Items is not empty.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The readouts and commands the strip shows.</summary>
    [Parameter] public IReadOnlyList<ToolbarItem> Items { get; set; } = [];

    /// <summary>Callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>Blocks interaction and dims the strip's items.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the strip renders an empty bar or nothing at all while it has no items.</summary>
    [Parameter] public StatusStripEmptyMode EmptyMode { get; set; }

    /// <summary>Hides the strip while keeping it mounted, independently of <see cref="EmptyMode"/> — which only ever concerns having no items.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Animates the transition between hidden and shown instead of snapping instantly.</summary>
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
