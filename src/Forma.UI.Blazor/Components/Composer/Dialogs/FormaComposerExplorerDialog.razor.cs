namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.UI.Blazor.Components.Explorer;
using Forma.UI.Blazor.Components.Feedback;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for choosing one of the host's stored files and inserting it.</summary>
public sealed partial class FormaComposerExplorerDialog : ComponentBase
{
    // Confirming has to be able to fail, so the modal is not allowed to close itself before a choice is judged.
    private static readonly FormaModalOptions StayOpenOnConfirm = new() { CloseOnConfirm = false };

    private static readonly FormaExplorerOptions Library = new() { AsDialog = true, ShowFolderMenu = false, SelectOnOpen = true };

    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>What the dialog browses, takes in, and inserts.</summary>
    [Parameter] public ExplorerPluginOptions Options { get; set; } = ExplorerPluginOptions.Default;

    /// <summary>Callback invoked when a file is chosen.</summary>
    [Parameter] public EventCallback<ResourceDialogResult> OnInsert { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    private IExplorerResourcesProvider? _resources;
    private IFileStore? _store;
    private FormaExplorerFileOptions _fileOptions = FormaExplorerFileOptions.ReadOnly;
    private FormaExplorerConstraints _constraints = FormaExplorerConstraints.None;
    private ExplorerPluginOptions? _syncedOptions;
    private IReadOnlyList<ResourceInfo> _selected = [];
    private bool _showSelectionRequired;

    private static FormaExplorerOptions LibraryOptions
        => Library;

    private FormaExplorerFileOptions LibraryFileOptions
        => _fileOptions;

    private FormaExplorerConstraints UploadConstraints
        => _constraints;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);

        // The explorer reloads whenever the provider it was handed is a different object, so everything
        // passed to it is built once per distinct set of options rather than fresh on every render.
        if (ReferenceEquals(_syncedOptions, Options))
        {
            return;
        }

        _syncedOptions = Options;

        var provider = (IExplorerResourcesProvider?)Services.GetService(typeof(IExplorerResourcesProvider));

        _store = (IFileStore?)Services.GetService(typeof(IFileStore));
        _resources = provider is null
            ? null
            : ExplorerResourceFilter.Parse(Options.AcceptedResourceKinds) is { } filter
                ? new FilteredResources(provider, filter)
                : provider;

        _fileOptions = FormaExplorerFileOptions.ReadOnly with { AllowUpload = Options.AllowUpload && _store is not null };

        // What may be added falls back to what is listed, so a caller naming one kind is not asked to name it twice.
        _constraints = new() { AcceptedResourceKinds = Options.AcceptedUploadKinds ?? Options.AcceptedResourceKinds };
    }

    private static string KindOf(ResourceInfo resource)
        => resource switch
        {
            ImageResourceInfo => "image",
            VideoResourceInfo => "video",
            AudioResourceInfo => "audio",
            _ => "file",
        };

    private void HandleSelectedChanged(IReadOnlyList<ResourceInfo> selected)
    {
        _selected = selected;

        if (selected.Count > 0)
        {
            _showSelectionRequired = false;
        }
    }

    private async Task HandleConfirmAsync()
    {
        if (_selected.Count == 0)
        {
            _showSelectionRequired = true;

            return;
        }

        var chosen = _selected[0];
        var kind = KindOf(chosen);

        // Only what the browser can render answers to the embed modes; everything else is a link whatever the mode says.
        var renderable = kind is not "file";
        var embed = renderable && Options.InsertMode is MediaInsertMode.Embed or MediaInsertMode.Both;
        var link = !renderable || Options.InsertMode is MediaInsertMode.Link or MediaInsertMode.Both;

        await OnInsert.InvokeAsync(new ResourceDialogResult(chosen.Source, chosen.Name, kind, embed, link));
    }

    // The explorer lists whatever its provider answers, so limiting what may be browsed means limiting the answer.
    private sealed class FilteredResources(IExplorerResourcesProvider inner, ExplorerResourceFilter filter) : IExplorerResourcesProvider
    {
        public async ValueTask<ExplorerResourcePage> GetResourcesAsync(ExplorerResourceQuery query, CancellationToken cancellationToken)
        {
            var page = await inner.GetResourcesAsync(query, cancellationToken).ConfigureAwait(false);
            var accepted = page.Resources.Where(filter.Accepts).ToList();

            // A count taken before filtering would page past the end, so it is dropped rather than reported wrongly.
            return new ExplorerResourcePage(accepted, accepted.Count == page.Resources.Count ? page.TotalCount : null);
        }
    }
}
