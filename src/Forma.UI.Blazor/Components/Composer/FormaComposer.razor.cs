namespace Forma.UI.Blazor.Components.Composer;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Resources;
using Forma.Framework.Sanitization;
using Forma.UI.Blazor.Components.Composer.Assets;
using Forma.UI.Blazor.Components.Composer.PlainTextEditor;
using Forma.UI.Blazor.Components.Composer.Plugins;
using Forma.UI.Blazor.Interop.Behaviours;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Composer.Syntax;
using Forma.UI.Features.Sanitization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

/// <summary>The composer engine, hosting rich text, Markdown, code, or page composition through extensible editor plugins.</summary>
public sealed partial class FormaComposer : InputBase<ComposerDocument>, IAsyncDisposable
{
    private readonly string _generatedId = $"composer-{Guid.NewGuid():N}";
    private readonly ComposerCommandRegistry _commands = new();
    private readonly ComposerRegionRegistry _regions = new();
    private ComposerSelection _selection = ComposerSelection.Empty;

    private ComposerPluginCatalog _catalog = default!;
    private ComposerKeyMap _keyMap = ComposerKeyMap.Empty;
    private ElementReference _composerElement;
    private DotNetObjectReference<FormaComposer>? _selfReference;
    private KeyBindings _keyBindings = default!;
    private Focus _focus = default!;
    private JsComposerAssetLoader _assetLoader = default!;
    private IComposerHistory _history = default!;
    private IHtmlSanitizer _sanitizer = default!;
    private ComposerContext _context = default!;
    private IComposerEditorPlugin _activeEditor = default!;
    private IComposerViewPlugin? _activeView;
    private bool _isInitialized;
    private bool _isJsStarted;
    private bool _isMaximized;
    private bool _isSelectionBarShown = true;
    private bool _isAttachOpen;

    /// <summary>Options configuring toolbar display, editor switcher, and default editor.</summary>
    [Parameter] public FormaComposerOptions Options { get; set; } = FormaComposerOptions.Default;

    /// <summary>Plugins supplied directly to this instance in addition to any registered in dependency injection.</summary>
    [Parameter] public IEnumerable<IComposerPlugin>? Plugins { get; set; }

    /// <summary>The identifier of the active editor plugin.</summary>
    [Parameter] public string? ActiveEditorId { get; set; }

    /// <summary>Callback invoked when the active editor plugin changes.</summary>
    [Parameter] public EventCallback<string> ActiveEditorIdChanged { get; set; }

    /// <summary>The optional HTML element identifier.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Whether the composer is disabled and ignores user interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the composer is in a read-only state.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;

    private string ElementId
        => Id ?? _generatedId;

    // A read-only composer offers no editing commands, so the toolbar would carry nothing actionable.
    private bool ShowToolbar
        => Options.ShowToolbar && !ReadOnly;

    private bool ShowTabs
        => Options.ShowEditorSwitcher && (_catalog.Editors.Count > 1 || ActiveViews.Count > 0);

    private IReadOnlyList<IComposerViewPlugin> ActiveViews
        => _catalog.ViewsOf(_activeEditor.Id);

    private string ActiveTabId
        => _activeView?.Id ?? _activeEditor.Id;

    private string ComposerClass
        => AdditionalAttributes.MergeClass(
            "forma-composer",
            CssClass,
            Disabled ? "forma-composer-disabled" : null,
            ReadOnly ? "forma-composer-readonly" : null,
            Options.IsResizable && !Disabled ? "forma-composer-resizable" : null,
            _isMaximized ? "forma-composer-maximized" : null);

    private ComposerDocument CurrentDocument
        => Value ?? new ComposerDocument(ComposerDocumentFormat.Html, string.Empty);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _keyBindings = new KeyBindings(JS);
        _focus = new Focus(JS);
        _assetLoader = new JsComposerAssetLoader(JS);
        _selfReference = DotNetObjectReference.Create(this);

        var injectedHistory = (IComposerHistory?)Services.GetService(typeof(IComposerHistory));
        var injectedSanitizer = (IHtmlSanitizer?)Services.GetService(typeof(IHtmlSanitizer));

        _history = injectedHistory ?? new ComposerHistory(Options.HistoryCapacity);
        _sanitizer = injectedSanitizer ?? new AllowlistHtmlSanitizer();

        _commands.RegisterEngineCommand(ComposerCommands.Undo, HandleUndoAsync);
        _commands.RegisterEngineCommand(ComposerCommands.Redo, HandleRedoAsync);
        _commands.RegisterEngineCommand(ComposerCommands.Maximize, HandleMaximizeAsync);
        _commands.RegisterEngineCommand(ComposerCommands.SelectionBar, HandleSelectionBarAsync);
        _commands.RegisterEngineCommand(ComposerCommands.Attach, HandleAttachAsync);
        _regions.Changed += HandleRegionsChanged;

        await LoadPluginsAsync();
        RebuildContext();

        _history.Record(CurrentDocument, _selection);
        _isInitialized = true;
    }

    private Task HandleMaximizeAsync()
    {
        _isMaximized = !_isMaximized;
        RebuildContext();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task ShowViewAsync(IComposerViewPlugin? view)
    {
        _activeView = view;

        if (view is not null)
        {
            await _assetLoader.LoadAssetsAsync(view.Assets);
        }

        RebuildContext();
        StateHasChanged();
    }

    // A tab names either an editor or one of the active editor's views.
    private Task SelectTabAsync(string tabId)
    {
        var view = ActiveViews.FirstOrDefault(candidate => candidate.Id == tabId);

        return view is not null ? ShowViewAsync(view) : SwitchEditorAsync(tabId);
    }

    private Task HandleSelectionBarAsync()
    {
        _isSelectionBarShown = !_isSelectionBarShown;
        RebuildContext();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task HandleAttachAsync()
    {
        _isAttachOpen = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    // Attaching is the engine's to run, so the gates come from whichever plugin contributed the command.
    private FileUploadPluginOptions AttachOptions
        => _catalog.FeaturePlugins.OfType<FileUploadPlugin>().FirstOrDefault()?.Options ?? FileUploadPluginOptions.Default;
    private async Task HandleAttachedAsync(IReadOnlyList<ResourceInfo> attached)
    {
        _isAttachOpen = false;

        // The dialog reports everything it has uploaded so far, so what it hands back replaces rather than adds to itself.
        var kept = CurrentDocument.Attachments.Where(held => !attached.Contains(held)).ToList();

        await HandleDocumentChangedAsync(CurrentDocument with { Attachments = [.. kept, .. attached] });
    }

    private async Task LoadPluginsAsync()
    {
        var injected = (IEnumerable<IComposerPlugin>?)Services.GetService(typeof(IEnumerable<IComposerPlugin>)) ?? [];
        var candidates = (Plugins ?? []).Concat(injected);

        _catalog = await ComposerPluginCatalog.CreateAsync(candidates, new PlainTextEditorPlugin());
        _activeEditor = _catalog.Resolve(ActiveEditorId, Options.DefaultEditorId, CurrentDocument.Format);

        _commands.RegisterPlugins(_catalog.All);
        _keyMap = new ComposerKeyMap([ComposerKeyBindingSource.Default, .. _catalog.All]);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentNullException.ThrowIfNull(Options);

        if (_isInitialized)
        {
            if (ActiveEditorId is not null && ActiveEditorId != _activeEditor.Id)
            {
                await SwitchEditorAsync(ActiveEditorId);
            }

            RebuildContext();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The first render can precede plugin loading, so interop waits for the render that follows it.
        if (_isJsStarted || !_isInitialized)
        {
            return;
        }

        _isJsStarted = true;

        await _keyBindings.AttachAsync(_composerElement, _selfReference!, ToKeyBindings(_keyMap.Bindings));
        // The canvas is where focus lands first; Tab then reaches the composer's own commands before leaving it.
        await _focus.AttachAsync(_composerElement, order: [".forma-composer-canvas"]);
        await _assetLoader.LoadAssetsAsync(_activeEditor.Assets);
    }

    private static IReadOnlyList<KeyBinding> ToKeyBindings(IReadOnlyList<ComposerKeyBinding> bindings)
        => [.. bindings.Select(b => new KeyBinding { Key = b.Key, Command = b.Command, Control = b.Control, Alt = b.Alt, Shift = b.Shift })];

    /// <summary>Runs a composer command by name, whether the engine or a plugin owns it.</summary>
    [JSInvokable]
    public async Task HandleCommandAsync(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        if (Disabled || ReadOnly)
        {
            return;
        }

        await InvokeAsync(async () =>
        {
            if (await _commands.ExecuteAsync(command, _context) is ComposerCommandResult.Plugin)
            {
                RebuildContext();
                StateHasChanged();
            }
        });
    }

    /// <summary>Switches the active editor plugin, converting the document format when needed.</summary>
    public async Task SwitchEditorAsync(string targetEditorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEditorId);

        var target = _catalog.Editors.FirstOrDefault(e => e.Id == targetEditorId);

        if (target is null)
        {
            return;
        }

        if (target.Id == _activeEditor.Id)
        {
            if (_activeView is not null)
            {
                await ShowViewAsync(null);
            }

            return;
        }

        if (target.Format != CurrentDocument.Format && !TryConvertTo(target.Format))
        {
            return;
        }

        _activeEditor = target;
        _activeView = null;

        await _assetLoader.LoadAssetsAsync(target.Assets);

        if (ActiveEditorIdChanged.HasDelegate)
        {
            await ActiveEditorIdChanged.InvokeAsync(target.Id);
        }

        RebuildContext();
        StateHasChanged();
    }

    private bool TryConvertTo(ComposerDocumentFormat format)
    {
        var converters = (IEnumerable<IComposerDocumentConverter>?)Services.GetService(typeof(IEnumerable<IComposerDocumentConverter>)) ?? [];
        var converter = converters.FirstOrDefault(c => c.Source == CurrentDocument.Format && c.Target == format);

        if (converter is null)
        {
            return false;
        }

        var conversion = converter.Convert(CurrentDocument.Content);

        if (!conversion.Succeeded)
        {
            return false;
        }

        var converted = new ComposerDocument(format, conversion.Content ?? string.Empty);
        _history.Record(converted, _selection);
        CurrentValue = converted;

        return true;
    }

    private void RebuildContext()
    {
        var injectedHighlighter = (IComposerSyntaxHighlighter?)Services.GetService(typeof(IComposerSyntaxHighlighter));

        _context = new ComposerContext(
            _activeEditor.Id,
            CurrentDocument,
            _selection,
            _history,
            _sanitizer,
            _assetLoader,
            _regions,
            HandleDocumentChangedAsync,
            HandleRequestRenderAsync,
            HandleSelectionChangedAsync,
            HandleCommandAsync,
            injectedHighlighter)
        {
            IsDisabled = Disabled,
            IsReadOnly = ReadOnly,
            IsMaximized = _isMaximized,
            IsSelectionBarShown = _isSelectionBarShown,
        };
    }

    private Task HandleDocumentChangedAsync(ComposerDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var sanitized = Sanitize(document);

        _history.Record(sanitized, _selection);
        CurrentValue = sanitized;
        RebuildContext();
        StateHasChanged();

        return Task.CompletedTask;
    }

    private Task HandleRequestRenderAsync()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    // A status item or gutter marker changing does not touch the document, so only a re-render is needed.
    private void HandleRegionsChanged()
        => _ = InvokeAsync(StateHasChanged);

    private Task HandleSelectionChangedAsync(ComposerSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (selection == _selection)
        {
            return Task.CompletedTask;
        }

        _selection = selection;
        RebuildContext();
        StateHasChanged();

        return Task.CompletedTask;
    }

    // Sanitisation is mandatory on every crossing to the host, and applies to markup rather than to a format that carries none.
    private ComposerDocument Sanitize(ComposerDocument document)
        => document.Format == ComposerDocumentFormat.Html
            ? document with { Content = _sanitizer.Sanitize(document.Content) }
            : document;

    private Task HandleUndoAsync()
    {
        var undone = _history.Undo();

        if (undone is not null)
        {
            CurrentValue = undone;
            RebuildContext();
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private Task HandleRedoAsync()
    {
        var redone = _history.Redo();

        if (redone is not null)
        {
            CurrentValue = redone;
            RebuildContext();
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        [MaybeNullWhen(false)] out ComposerDocument result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = new ComposerDocument(ComposerDocumentFormat.Html, string.Empty);
            validationErrorMessage = null;
            return true;
        }

        var newlineIndex = value.IndexOf('\n', StringComparison.Ordinal);

        if (newlineIndex < 0)
        {
            result = new ComposerDocument(ComposerDocumentFormat.Html, value);
            validationErrorMessage = null;
            return true;
        }

        var formatId = value[..newlineIndex].Trim();
        var content = value[(newlineIndex + 1)..];

        if (string.IsNullOrWhiteSpace(formatId))
        {
            result = new ComposerDocument(ComposerDocumentFormat.Html, content);
            validationErrorMessage = null;
            return true;
        }

        result = new ComposerDocument(new ComposerDocumentFormat(formatId), content);
        validationErrorMessage = null;
        return true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _regions.Changed -= HandleRegionsChanged;

        _selfReference?.Dispose();
        await _assetLoader.DisposeAsync();
        await _focus.DisposeAsync();
        await _keyBindings.DisposeAsync();
    }
}
