namespace Forma.UI.Components.Layout;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaTab : IDisposable
{
    [CascadingParameter] private FormaTabs Parent { get; set; } = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public FormaIcon? Icon { get; set; }
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    internal string Id { get; } = Guid.NewGuid().ToString("N")[..8];

    protected override void OnInitialized()
    {
        if (Parent is null)
        {
            throw new InvalidOperationException($"{nameof(FormaTab)} must be placed inside a {nameof(FormaTabs)} component.");
        }

        Parent.RegisterTab(this);
    }

    protected override void OnParametersSet() => ArgumentException.ThrowIfNullOrWhiteSpace(Title);

    public void Dispose() => Parent.UnregisterTab(this);
}
