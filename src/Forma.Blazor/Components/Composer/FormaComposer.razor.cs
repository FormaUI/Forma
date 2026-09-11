namespace Forma.Blazor.Components.Composer;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Composer.Assets;
using Forma.Blazor.Components.Composer.PlainTextEditor;
using Forma.Blazor.Components.Composer.Plugins;
using Forma.Framework.Common.Interop.Behaviours;
using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Composer.Syntax;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Resources;
using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The composer engine, hosting rich text, Markdown, code, or page composition through extensible editor plugins.</summary>
/// <param name="syntaxHighlighter">The host's registered highlighter, or null to classify no code.</param>
public sealed partial class FormaComposer(IComposerSyntaxHighlighter? syntaxHighlighter = null)
    : FormaInputBase<ComposerDocument>, IComposerInteraction
{
    private readonly ComposerCommandRegistry _commandRegistry = new();
    private readonly ComposerRegionRegistry _regionRegistry = new();
    private ComposerSelection _selection = ComposerSelection.Empty;

    private ComposerPluginCatalog _pluginCatalog = default!;
    private ComposerKeyBindingCatalog _keyBindingCatalog = ComposerKeyBindingCatalog.Empty;
    private DotNetObjectReference<FormaComposer>? _selfReference;
    private InputScope? _inputScope;
    private Focus _focus = default!;
    private JSComposerAssetLoader _assetLoader = default!;
    private IComposerHistory _history = default!;
    private readonly IComposerSyntaxHighlighter _syntaxHighlighter = syntaxHighlighter ?? NullSyntaxHighlighter.Instance;
    private IComposerEditorPlugin _activeEditor = default!;
    private IReadOnlyList<IComposerViewPlugin>? _activeViews;
    private IComposerViewPlugin? _activeView;
    private bool _isInitialized;
    private bool _isJsStarted;
    private readonly ComposerChrome _chrome = new();
    private bool _isAttachOpen;

    /// <summary>Options configuring toolbar display, editor switcher, and default editor.</summary>
    [Parameter] public ComposerOptions Options { get; set; } = ComposerOptions.Default;

    /// <summary>Plugins supplied directly to this instance in addition to any registered in dependency injection.</summary>
    [Parameter] public IEnumerable<IComposerPlugin>? Plugins { get; set; }

    /// <summary>The identifier of the active editor plugin.</summary>
    [Parameter] public string? ActiveEditorId { get; set; }

    /// <summary>Callback invoked when the active editor plugin changes.</summary>
    [Parameter] public EventCallback<string> ActiveEditorIdChanged { get; set; }

    /// <summary>Whether the composer is disabled and ignores user interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the composer is in a read-only state.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The undo and redo stack this composer uses, constructed from <see cref="ComposerOptions.HistoryCapacity"/> when unset.</summary>
    [Parameter] public IComposerHistory? History { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;
    [Inject] private IHtmlSanitizer Sanitizer { get; set; } = default!;
    [Inject] private IEnumerable<IComposerPlugin> RegisteredPlugins { get; set; } = default!;
    [Inject] private IEnumerable<IComposerDocumentConverter> RegisteredConverters { get; set; } = default!;

    private string ElementId
        => Id ?? $"composer-{RuntimeId:N}";

    // A read-only composer offers no editing commands, so the toolbar would carry nothing actionable.
    private bool ShowToolbar
        => Options.ShowToolbar && !ReadOnly;

    private bool ShowTabs
        => Options.ShowEditorSwitcher && (_pluginCatalog.Editors.Count > 1 || ActiveViews.Count > 0);

    private IReadOnlyList<IComposerViewPlugin> ActiveViews
        => _activeViews ??= _pluginCatalog.GetViewsForEditor(_activeEditor.Id);

    private string ActiveTabId
        => _activeView?.Id ?? _activeEditor.Id;

    private ComposerDocument CurrentDocument
        => Value ?? new ComposerDocument(ComposerDocumentFormat.Html, string.Empty);

    private ComposerContext? _context;

    // The same instance comes back until a member actually changes.
    private ComposerContext? ComposerContext
    {
        get
        {
            if (!_isInitialized)
            {
                return null;
            }

            var built = BuildContext();

            if (built != _context)
            {
                _context = built;
            }

            return _context;
        }
    }

    private ComposerContext BuildContext()
        => new()
        {
            ActiveEditorId = _activeEditor.Id,
            Document = CurrentDocument,
            Selection = _selection,
            IsDisabled = Disabled,
            IsReadOnly = ReadOnly,
            IsMaximized = _chrome.IsMaximized,
            IsSelectionBarShown = _chrome.IsSelectionBarShown,
            History = _history,
            Sanitizer = Sanitizer,
            AssetLoader = _assetLoader,
            Regions = _regionRegistry,
            SyntaxHighlighter = _syntaxHighlighter,
            KeyBindings = _keyBindingCatalog,
        };

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _focus = new Focus(JS);
        _assetLoader = new JSComposerAssetLoader(JS);
        _selfReference = DotNetObjectReference.Create(this);

        _history = History ?? new ComposerHistory(Options.HistoryCapacity);

        _commandRegistry.RegisterEngineCommand(ComposerCommands.Undo, HandleUndoAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.Redo, HandleRedoAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.Maximize, HandleMaximizeAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.SelectionBar, HandleSelectionBarAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.Attach, HandleAttachAsync);
        _regionRegistry.Changed += HandleRegionsChanged;

        await LoadPluginsAsync();

        _history.Record(CurrentDocument, _selection);
        _isInitialized = true;
    }

    private Task HandleMaximizeAsync()
    {
        _chrome.ToggleMaximized();
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
        _chrome.ToggleSelectionBar();
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
    private FileUploadPluginOptions _attachOptions = FileUploadPluginOptions.Default;
    private async Task HandleAttachedAsync(IReadOnlyList<ResourceInfo> attached)
    {
        _isAttachOpen = false;

        // The dialog reports everything it has uploaded so far, so what it hands back replaces rather than adds to itself.
        var kept = CurrentDocument.Attachments.Where(held => !attached.Contains(held)).ToList();

        await HandleDocumentChangedAsync(CurrentDocument with { Attachments = [.. kept, .. attached] });
    }

    private async Task LoadPluginsAsync()
    {
        var candidates = (Plugins ?? []).Concat(RegisteredPlugins);

        _pluginCatalog = await ComposerPluginCatalog.CreateAsync(candidates, new PlainTextEditorPlugin());
        _activeEditor = _pluginCatalog.ResolveEditor(ActiveEditorId, Options.DefaultEditorId, CurrentDocument.Format);
        _activeViews = null;

        _commandRegistry.RegisterPlugins(_pluginCatalog.Plugins);
        _keyBindingCatalog = new ComposerKeyBindingCatalog([ComposerKeyBindingSource.Default, .. _pluginCatalog.Plugins]);
        _attachOptions = _pluginCatalog.FeaturePlugins.OfType<FileUploadPlugin>().FirstOrDefault()?.Options ?? FileUploadPluginOptions.Default;
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

        // The composer's shortcuts route through its scope wherever focus rests inside it; the active editor registers them on its editable element as well, where a native control would otherwise claim them.
        _inputScope = await InputRouter.RegisterAsync(RootElement, new InputProfile { Routes = _keyBindingCatalog.ToInputRoutes() }, _selfReference);
        // The canvas is where focus lands first; Tab then reaches the composer's own commands before leaving it.
        await _focus.AttachAsync(RootElement, order: [".forma-composer-canvas"]);
        await _assetLoader.LoadAssetsAsync(_activeEditor.Assets);
    }

    /// <summary>Runs the command whose shortcut the router matched; a composer command is its own action.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return HandleCommandAsync(action);
    }

    /// <summary>Runs a composer command by name, whether the engine or a plugin owns it.</summary>
    public async Task HandleCommandAsync(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        if (Disabled || ReadOnly)
        {
            return;
        }

        if (ComposerContext is not { } context)
        {
            return;
        }

        await InvokeAsync(async () =>
        {
            if (await _commandRegistry.ExecuteAsync(command, context, this) is ComposerCommandResult.Plugin)
            {
                StateHasChanged();
            }
        });
    }

    /// <summary>Switches the active editor plugin, converting the document format when needed.</summary>
    public async Task SwitchEditorAsync(string targetEditorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEditorId);

        var target = _pluginCatalog.Editors.FirstOrDefault(e => e.Id == targetEditorId);

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
        _activeViews = null;

        await _assetLoader.LoadAssetsAsync(target.Assets);

        if (ActiveEditorIdChanged.HasDelegate)
        {
            await ActiveEditorIdChanged.InvokeAsync(target.Id);
        }

        StateHasChanged();
    }

    private bool TryConvertTo(ComposerDocumentFormat format)
    {
        var converter = RegisteredConverters.FirstOrDefault(c => c.Source == CurrentDocument.Format && c.Target == format);

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

    Task IComposerInteraction.ChangeDocumentAsync(ComposerDocument document)
        => HandleDocumentChangedAsync(document);

    Task IComposerInteraction.RequestRenderAsync()
        => HandleRequestRenderAsync();

    Task IComposerInteraction.ReportSelectionAsync(ComposerSelection selection)
        => HandleSelectionChangedAsync(selection);

    Task IComposerInteraction.ExecuteCommandAsync(string command)
        => HandleCommandAsync(command);

    private Task HandleDocumentChangedAsync(ComposerDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var sanitized = Sanitize(document);

        _history.Record(sanitized, _selection);
        CurrentValue = sanitized;
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
        StateHasChanged();

        return Task.CompletedTask;
    }

    // Sanitisation is mandatory on every crossing to the host, and applies to markup rather than to a format that carries none.
    private ComposerDocument Sanitize(ComposerDocument document)
        => document.Format == ComposerDocumentFormat.Html
            ? document with { Content = Sanitizer.Sanitize(document.Content) }
            : document;

    private Task HandleUndoAsync()
    {
        var undone = _history.Undo();

        if (undone is not null)
        {
            CurrentValue = undone;
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
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-composer")
            .Add("forma-composer-disabled", Disabled)
            .Add("forma-composer-readonly", ReadOnly)
            .Add("forma-composer-resizable", Options.IsResizable && !Disabled)
            .Add("forma-composer-maximized", _chrome.IsMaximized);

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
    protected override async ValueTask DisposeAsyncCore()
    {
        _regionRegistry.Changed -= HandleRegionsChanged;

        if (_inputScope is { } inputScope)
        {
            _inputScope = null;
            await inputScope.DisposeAsync();
        }

        _selfReference?.Dispose();
        await _assetLoader.DisposeAsync();
        await _focus.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
