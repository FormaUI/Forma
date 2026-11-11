namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.Explorer.Interop;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Explorer;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The explorer's header: an address bar over a toolbar where the width allows, and one compact row where it does not.</summary>
public sealed partial class FormaExplorerCommandArea : FormaComponentBase
{
    private const string LayoutProperty = "--forma-explorer-header-layout";

    private Breakpoint _breakpoint = default!;
    private DotNetObjectReference<FormaExplorerCommandArea>? _selfReference;
    private ElementReference _rows;
    private ExplorerHeaderLayout? _layout;

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    private string SelectionAnnouncement
        => View.Selection.Count > 0 ? L["{SelectedCount} selected", View.Selection.Count].Value : string.Empty;

    /// <summary>Takes the layout the stylesheet names for the header's width.</summary>
    [JSInvokable]
    public Task HandleBreakpointAsync(string layout)
    {
        var nextLayout = layout switch
        {
            "compact" => ExplorerHeaderLayout.Compact,
            "medium" => ExplorerHeaderLayout.Medium,
            _ => ExplorerHeaderLayout.Wide,
        };

        if (_layout == nextLayout)
        {
            return Task.CompletedTask;
        }

        _layout = nextLayout;

        return InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerCommandArea)} must be rendered inside a {nameof(FormaExplorer)}.");
        }

        _breakpoint = new Breakpoint(Context.JS);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _selfReference = DotNetObjectReference.Create(this);
        await _breakpoint.AttachAsync(_rows, LayoutProperty, _selfReference);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _breakpoint.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }
}
