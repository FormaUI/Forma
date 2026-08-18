namespace Forma.UI.Blazor.Components.Interactive;

using Microsoft.AspNetCore.Components;

/// <summary>A single command inside a <see cref="FormaDropDownMenu"/>.</summary>
public sealed partial class FormaDropDownMenuItem : IDisposable
{
    [CascadingParameter] private FormaDropDownMenu Menu { get; set; } = default!;

    /// <summary>Navigates here instead of raising <see cref="OnClick"/>, rendering the item as a link.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>Blocks activation and dims the item.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Marks the item as destroying data, colouring it as a warning.</summary>
    [Parameter] public bool IsDestructive { get; set; }

    /// <summary>Raised when the item is activated.</summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>Content rendered inside the item.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>Additional attributes applied to the item element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private ElementReference _element;

    private string CssClass
        => IsDestructive ? "forma-dropdown-menu-item is-destructive" : "forma-dropdown-menu-item";

    protected override void OnInitialized()
    {
        if (Menu is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDropDownMenuItem)} must be placed inside a {nameof(FormaDropDownMenu)} component.");
        }

        Menu.RegisterItem(this);
    }

    internal ValueTask FocusAsync()
        => _element.FocusAsync();

    private async Task HandleActivateAsync()
    {
        if (Disabled)
        {
            return;
        }

        await OnClick.InvokeAsync();
        await Menu.RequestActivateAsync();
    }

    public void Dispose()
        => Menu.UnregisterItem(this);
}
