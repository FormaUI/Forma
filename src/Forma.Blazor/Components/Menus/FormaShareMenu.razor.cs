namespace Forma.Blazor.Components.Menus;

using Forma.Blazor.Components.Buttons;
using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Icons;
using Forma.Framework.Share;

using Microsoft.AspNetCore.Components;

/// <summary>A menu of the ways to share one item: the social networks on a scrolling row, the built-in commands below them, and the consumer's own actions behind More.</summary>
/// <param name="connections">The accounts the user has connected for the targets that share through one.</param>
public sealed partial class FormaShareMenu(IShareConnections? connections = null) : FormaElementBase
{
    /// <summary>The item the menu shares.</summary>
    [Parameter] public ShareItem? Item { get; set; }

    /// <summary>The actions the consumer adds to the menu.</summary>
    [Parameter] public IReadOnlyList<ShareAction> Actions { get; set; } = [];

    /// <summary>Raised with the command of the chosen action.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The content of the trigger button, in place of the share icon and label.</summary>
    [Parameter] public RenderFragment? Trigger { get; set; }

    /// <summary>The appearance of the trigger button.</summary>
    [Parameter] public ButtonOptions TriggerOptions { get; set; } = ButtonOptions.Default;

    private FormaDropDownMenu Menu { get; set; } = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Menu is null ? default : Menu.RootElement;

    [Inject] private IEnumerable<IShareTarget> ShareTargets { get; set; } = [];

    private Share _share = default!;
    private Clipboard _clipboard = default!;
    private Print _print = default!;
    private Func<Task>? _refresh;
    private bool _canShareNatively;
    private bool _isExpanded;
    private string? _notice;
    private ShareItem? _noticedItem;

    private IReadOnlyList<IShareTarget> _supportedTargets = [];

    private bool HasSocialNetworks
        => _supportedTargets.Count > 0 || (_canShareNatively && Item is { Address: not null });

    private bool OffersEmail
        => Item is { Name: not null } && (Item.Kind is ShareItemKind.Contact || (Item.Kind is ShareItemKind.Link or ShareItemKind.Image && Item.Address is not null));

    private bool OffersCopyLink
        => Item is { Address: not null } && Item.Kind is not ShareItemKind.Contact;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _supportedTargets = Item is null ? [] : [.. ShareTargets.Where(target => target.Supports(Item))];

        // The notice belongs to the item it was made for, so only a change of item clears it.
        if (!ReferenceEquals(Item, _noticedItem))
        {
            _notice = null;
            _noticedItem = Item;
        }
    }

    private string EmailAddress
        => $"mailto:?subject={Uri.EscapeDataString(Item?.Name ?? string.Empty)}&body={Uri.EscapeDataString(Item?.Address ?? string.Empty)}";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _share = new Share(Context.JS);
        _clipboard = new Clipboard(Context.JS);
        _print = new Print(Context.JS);

        if (connections is not null)
        {
            _refresh = () => InvokeAsync(StateHasChanged);
            connections.Subscribe(_refresh);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && await _share.IsAvailableAsync())
        {
            _canShareNatively = true;
            StateHasChanged();
        }
    }

    /// <summary>Whether the target shares through an account the host reports the user has not connected.</summary>
    private bool NeedsConnection(IShareTarget target)
        => target.RequiresAccount && connections is { } known && !known.IsConnected(target.Id);

    private Task ConnectAsync(IShareTarget target)
        => connections!.ConnectAsync(target.Id);

    /// <summary>Shares through a target that needs a call, and reports how it went.</summary>
    /// <remarks>A failed call to a target that requires an account reports the account as missing, whatever the host's connection status said.</remarks>
    private async Task ShareAsync(IShareTarget target)
    {
        _notice = await target.ShareAsync(Item!)
            ? L["Shared to {Target}.", target.Title].Value
            : target.RequiresAccount ? L["{Target} needs a connected account.", target.Title].Value : null;
    }

    private void ToggleExpanded()
        => _isExpanded = !_isExpanded;

    private async Task ShareNativelyAsync()
        => await _share.ShareAsync(Item?.Name ?? string.Empty, Item?.Address ?? string.Empty);

    private async Task CopyLinkAsync()
        => await _clipboard.WriteAsync(Item?.Address ?? string.Empty);

    private async Task PrintAsync()
        => await _print.PrintPageAsync();

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_refresh is not null)
        {
            connections?.Unsubscribe(_refresh);
        }

        await _share.DisposeAsync();
        await _clipboard.DisposeAsync();
        await _print.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
