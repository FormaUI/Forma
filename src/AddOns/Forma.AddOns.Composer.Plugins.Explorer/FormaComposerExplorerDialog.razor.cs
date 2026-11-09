namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Blazor.Components.Explorer;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Composer.Dialogs;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>The dialog body for choosing one of the host's stored files to insert.</summary>
/// <param name="itemsProvider">The host's registered items provider, where null means there is nothing to browse.</param>
/// <param name="store">The host's registered file store, where null means there is nowhere to upload.</param>
public sealed partial class FormaComposerExplorerDialog(ICatalogProvider? itemsProvider = null, IFileStore? store = null)
    : FormaComponentBase, IComposerInsertionDialog
{
    private static readonly ExplorerOptions Library = new() { AsDialog = true, ShowFolderMenu = false, SelectOnOpen = true, MaxSelection = 1 };

    /// <summary>The options configuring what the dialog browses, takes in, and inserts.</summary>
    [Parameter] public ExplorerPluginOptions Options { get; set; } = ExplorerPluginOptions.Default;

    private readonly ICatalogProvider? _registeredItemsProvider = itemsProvider;
    private readonly IFileStore? _store = store;
    private readonly List<string> _messages = [];
    private ICatalogProvider? _itemsProvider;
    private ExplorerItemOptions _itemOptions = ExplorerItemOptions.ReadOnly;
    private CatalogConstraints _constraints = CatalogConstraints.None;
    private readonly ParameterState<ExplorerPluginOptions?> _optionsParameter = new();
    private IReadOnlyList<ICatalogItem> _selectedItems = [];

    string IComposerInsertionDialog.Title
        => L["Browse Files"].Value;

    string? IComposerInsertionDialog.Notice
        => _itemsProvider is null
            ? L["No stored files are available, because this application registered nowhere to browse."].Value
            : null;

    IReadOnlyList<string> IComposerInsertionDialog.Messages
        => _messages;

    private static ExplorerOptions LibraryOptions
        => Library;

    private ExplorerItemOptions LibraryItemOptions
        => _itemOptions;

    private CatalogConstraints UploadConstraints
        => _constraints;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ArgumentNullException.ThrowIfNull(Options);

        // The explorer reloads when handed a different provider object, so these are rebuilt only when the options change.
        if (!_optionsParameter.Update(Options))
        {
            return;
        }

        _itemsProvider = _registeredItemsProvider is null
            ? null
            : CatalogResourceFilter.Parse(Options.AcceptedResourceKinds) is { } filter
                ? new FilteredItems(_registeredItemsProvider, filter)
                : _registeredItemsProvider;

        _itemOptions = ExplorerItemOptions.ReadOnly with { AllowUpload = Options.AllowUpload && _store is not null };

        _constraints = new() { AcceptedResourceKinds = Options.AcceptedUploadKinds ?? Options.AcceptedResourceKinds };
    }

    ValueTask<ComposerInsertion?> IComposerInsertionDialog.TryConfirmAsync(CancellationToken cancellationToken)
    {
        _messages.Clear();

        if (_selectedItems.Count == 0)
        {
            _messages.Add(L["Choose a file before inserting."].Value);

            return ValueTask.FromResult<ComposerInsertion?>(null);
        }

        if (_selectedItems[0].Resource is not { } resource)
        {
            return ValueTask.FromResult<ComposerInsertion?>(null);
        }

        var kind = GetKind(resource);

        var canEmbed = kind is not "file";
        var embed = canEmbed && Options.InsertMode is MediaInsertMode.Embed or MediaInsertMode.Both;
        var link = !canEmbed || Options.InsertMode is MediaInsertMode.Link or MediaInsertMode.Both;

        return ValueTask.FromResult<ComposerInsertion?>(new ResourceInsertion { Source = resource.Source, Name = resource.Name, Kind = kind, Embed = embed, Link = link });
    }

    private static string GetKind(ResourceInfo resource)
        => resource switch
        {
            ImageResourceInfo => "image",
            VideoResourceInfo => "video",
            AudioResourceInfo => "audio",
            _ => "file",
        };

    private void HandleSelectedChanged(IReadOnlyList<ICatalogItem> selectedItems)
    {
        _selectedItems = selectedItems;

        if (selectedItems.Count > 0)
        {
            _messages.Clear();
        }
    }

    // The inner provider's items narrowed to the resources the filter accepts.
    private sealed class FilteredItems(ICatalogProvider innerProvider, CatalogResourceFilter filter) : ICatalogProvider
    {
        public async ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
        {
            var page = await innerProvider.GetItemsAsync(query, cancellationToken).ConfigureAwait(false);
            var acceptedItems = page.Items.Where(item => item.Resource is { } resource && filter.Accepts(resource)).ToList();

            // The inner total counts rejected items too, so it is kept only when the filter rejected none.
            return new CatalogPage(acceptedItems, acceptedItems.Count == page.Items.Count ? page.TotalCount : null);
        }
    }
}
