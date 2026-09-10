namespace Forma.Blazor.Components.Menus;

using Forma.Blazor.Components.Buttons;
using Forma.Framework.Common.Interop.Operations;
using Forma.Framework.Icons;
using Forma.Framework.Share;

using Microsoft.AspNetCore.Components;

/// <summary>A share menu over one item: the social networks and download baseline on a scrolling row, a consumer's own actions beside them, and the rest behind More.</summary>
public sealed partial class FormaShareMenu : FormaElementBase
{
    /// <summary>What the menu acts on.</summary>
    [Parameter] public ShareItem? Item { get; set; }

    /// <summary>Actions the consumer adds beside the baseline.</summary>
    [Parameter] public IReadOnlyList<ShareAction> Actions { get; set; } = [];

    /// <summary>Callback invoked with the command of a consumer action that was chosen.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>Content rendered inside the trigger button, standing in for the share icon and label.</summary>
    [Parameter] public RenderFragment? Trigger { get; set; }

    /// <summary>Appearance of the trigger button.</summary>
    [Parameter] public ButtonOptions TriggerOptions { get; set; } = ButtonOptions.Default;

    private FormaDropDownMenu Menu { get; set; } = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Menu is null ? default : Menu.RootElement;

    [Inject] private IEnumerable<IShareTarget> ShareTargets { get; set; } = [];

    private Share _share = default!;
    private Clipboard _clipboard = default!;
    private Print _print = default!;
    private bool _canShareNatively;
    private bool _isExpanded;

    private IReadOnlyList<IShareTarget> _supportedTargets = [];

    private bool HasSocialNetworks
        => _supportedTargets.Count > 0 || (_canShareNatively && Item is { Address: not null });

    // A file is not posted to a network; a contact is reached by email rather than a copied address.
    private bool OffersEmail
        => Item is { Name: not null } && (Item.Kind is ShareItemKind.Contact || (Item.Kind is ShareItemKind.Link or ShareItemKind.Image && Item.Address is not null));

    private bool OffersCopyLink
        => Item is { Address: not null } && Item.Kind is not ShareItemKind.Contact;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _supportedTargets = Item is null ? [] : [.. ShareTargets.Where(target => target.Supports(Item))];

    private string EmailAddress
        => $"mailto:?subject={Uri.EscapeDataString(Item?.Name ?? string.Empty)}&body={Uri.EscapeDataString(Item?.Address ?? string.Empty)}";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _share = new Share(JS);
        _clipboard = new Clipboard(JS);
        _print = new Print(JS);
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
        await _share.DisposeAsync();
        await _clipboard.DisposeAsync();
        await _print.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
