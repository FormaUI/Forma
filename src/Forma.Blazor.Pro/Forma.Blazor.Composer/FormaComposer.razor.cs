namespace Forma.Blazor.Components.Composer;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Composer.Assets;
using Forma.Blazor.Components.Composer.PlainTextEditor;
using Forma.Blazor.Components.Composer.Plugins;
using Forma.Blazor.Components.Feedback;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Dialogs;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Composer.Syntax;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Localization;
using Forma.Framework.Plugins;
using Forma.Framework.Resources;
using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The composer engine, hosting rich text, Markdown, code, or page composition through extensible editor plugins.</summary>
/// <param name="syntaxHighlighter">The host's registered highlighter, or null to classify no code.</param>
/// <param name="definition">What the application registered every composer to be made of, or null to have only the editor a composer falls back to.</param>
public sealed partial class FormaComposer(
    IComposerSyntaxHighlighter? syntaxHighlighter = null,
    ComposerDefinition? definition = null)
    : FormaInputBase<ComposerDocument>, IComposerInteraction
{
    private readonly ComposerDefinition? _definition = definition;
    private PluginScope<IComposerPlugin> _ownPlugins = PluginScope<IComposerPlugin>.Empty;
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
    private IReadOnlyList<IComposerPlugin> _surfacePlugins = [];
    private bool _isInitialized;
    private bool _isJsStarted;
    private readonly ComposerChrome _chrome = new();
    private bool _isAttachOpen;
    private ComposerInsertionRequest? _dialog;
    private Dictionary<string, object>? _dialogParameters;
    private DynamicComponent? _dialogComponent;
    private bool _isDialogBodyReady;
    private string? _dialogRefusal;

    /// <summary>Options configuring toolbar display, editor switcher, and default editor.</summary>
    [Parameter] public ComposerOptions Options { get; set; } = ComposerOptions.Default;

    /// <summary>What this composer alone is made of, in place of whatever the application registered.</summary>
    [Parameter] public ComposerDefinition? Plugins { get; set; }

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
    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private IEnumerable<IComposerDocumentConverter> RegisteredConverters { get; set; } = default!;
    [Inject] private IStringLocaleFactory StringLocaleFactory { get; set; } = default!;

    private string ElementId
        => Id ?? $"composer-{Context.RuntimeId:N}";

    // A read-only composer offers no editing commands, so the toolbar would carry nothing actionable.
    private bool ShowToolbar
        => Options.ShowToolbar && !ReadOnly;

    private bool ShowTabs
        => Options.ShowEditorSwitcher && (_pluginCatalog.Editors.Count > 1 || ActiveViews.Count > 0);

    private IReadOnlyList<IComposerViewPlugin> ActiveViews
        => _activeViews ??= _pluginCatalog.GetViews(_activeEditor);

    private string ActiveTabId
        => _activeView?.Id ?? _activeEditor.Id;

    private ComposerPluginLocalizer Localizer
        => field ??= new ComposerPluginLocalizer(StringLocaleFactory, L);

    private ComposerDocument CurrentDocument
        => Value ?? new ComposerDocument(ComposerDocumentFormat.Html, string.Empty);

    private ComposerContext? _context;
    private ComposerDocument? _sanitizedValue;
    private DocumentConverterRegistry _converterRegistry = new();

    // The same instance comes back until a member actually changes.
    private ComposerContext? Composer
    {
        get
        {
            if (!_isInitialized)
            {
                return null;
            }

            var built = BuildComposer();

            if (built != _context)
            {
                _context = built;
            }

            return _context;
        }
    }

    private ComposerContext BuildComposer()
        => new()
        {
            ActiveEditor = _activeEditor,
            ActiveView = _activeView,
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
        _focus = new Focus(Context.JS);
        _assetLoader = new JSComposerAssetLoader(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);

        _history = History ?? new ComposerHistory(Options.HistoryCapacity);

        _commandRegistry.RegisterEngineCommand(ComposerCommands.Undo, HandleUndoAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.Redo, HandleRedoAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.Maximize, HandleMaximizeAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.SelectionBar, HandleSelectionBarAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.Attach, HandleAttachAsync);
        _regionRegistry.Changed += HandleRegionsChanged;

        await LoadPluginsAsync();
        _converterRegistry = new DocumentConverterRegistry(RegisteredConverters);
        SanitizeValue();

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

        RebuildScope();

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
        // The plugins are this composer's own: an editor plugin carries events its canvas subscribes to,
        // so a second composer sharing one would answer this one's commands.
        _ownPlugins = (Plugins ?? _definition ?? ComposerDefinition.Empty).Activate(Services);

        _pluginCatalog = await ComposerPluginCatalog.CreateAsync(_ownPlugins.Instances, new PlainTextEditorPlugin());
        _activeEditor = _pluginCatalog.ResolveEditor(ActiveEditorId, Options.DefaultEditorId, CurrentDocument.Format);
        _activeViews = null;

        RebuildScope();
    }

    // Two editors may each own a plugin whose command is the same, so what answers a command is the scope in play.
    private void RebuildScope()
    {
        _surfacePlugins = _pluginCatalog.GetFeaturePlugins(_activeEditor, _activeView);

        var inPlay = _pluginCatalog.GetSurfacePlugins(_activeEditor, _activeView);

        _commandRegistry.RegisterPlugins(inPlay);
        _keyBindingCatalog = new ComposerKeyBindingCatalog([ComposerKeyBindingSource.Default, .. inPlay]);
        _attachOptions = _surfacePlugins.OfType<FileUploadPlugin>().FirstOrDefault()?.Options ?? FileUploadPluginOptions.Default;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentNullException.ThrowIfNull(Options);
        SanitizeValue();

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
        // The shell reads its title, notice and messages off the body, which the renderer creates as the shell renders.
        if (_dialog is not null && !_isDialogBodyReady && DialogBody is not null)
        {
            _isDialogBodyReady = true;
            StateHasChanged();
        }

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

        if (Composer is not { } context)
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

        RebuildScope();

        await _assetLoader.LoadAssetsAsync(target.Assets);

        if (ActiveEditorIdChanged.HasDelegate)
        {
            await ActiveEditorIdChanged.InvokeAsync(target.Id);
        }

        StateHasChanged();
    }

    private bool TryConvertTo(ComposerDocumentFormat format)
    {
        var conversion = _converterRegistry.Convert(CurrentDocument, format);

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

    Task IComposerInteraction.OpenDialogAsync(ComposerInsertionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!typeof(IComposerInsertionDialog).IsAssignableFrom(request.Body))
        {
            throw new ArgumentException(
                $"{request.Body.Name} cannot be opened as a composer dialog, because it does not implement {nameof(IComposerInsertionDialog)}.",
                nameof(request));
        }

        _dialog = request;
        _dialogParameters = request.Parameters.ToDictionary(parameter => parameter.Key, parameter => parameter.Value!, StringComparer.Ordinal);
        _dialogComponent = null;
        _isDialogBodyReady = false;
        _dialogRefusal = null;

        return InvokeAsync(StateHasChanged);
    }

    ValueTask<bool> IComposerInteraction.TryInsertAsync(ComposerInsertion insertion)
    {
        ArgumentNullException.ThrowIfNull(insertion);

        return Disabled || ReadOnly
            ? ValueTask.FromResult(false)
            : _activeEditor.TryInsertAsync(insertion);
    }

    private IComposerInsertionDialog? DialogBody
        => _dialogComponent?.Instance as IComposerInsertionDialog;

    // The body reports what confirming refused; the composer adds its own refusal when no editor took the insertion.
    private IReadOnlyList<string> DialogMessages
    {
        get
        {
            var refused = DialogBody?.Messages ?? [];

            return _dialogRefusal is null ? refused : [.. refused, _dialogRefusal];
        }
    }

    private static DialogSize GetDialogSize(ComposerDialogSize size)
        => size switch
        {
            ComposerDialogSize.Small => DialogSize.Small,
            ComposerDialogSize.Large => DialogSize.Large,
            ComposerDialogSize.ExtraLarge => DialogSize.ExtraLarge,
            _ => DialogSize.Medium,
        };

    private async Task HandleDialogConfirmAsync()
    {
        if (DialogBody is not { } body)
        {
            return;
        }

        _dialogRefusal = null;

        if (await body.TryConfirmAsync() is not { } insertion)
        {
            return;
        }

        if (await ((IComposerInteraction)this).TryInsertAsync(insertion))
        {
            CloseDialog();

            return;
        }

        _dialogRefusal = L["This could not be inserted into the document."].Value;
    }

    private void CloseDialog()
    {
        _dialog = null;
        _dialogParameters = null;
        _dialogComponent = null;
        _isDialogBodyReady = false;
        _dialogRefusal = null;
    }

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

    /// <summary>Sanitizes the document the host bound, once per document, and hands the host the sanitized one when it differs.</summary>
    private void SanitizeValue()
    {
        if (Value is null || Value == _sanitizedValue)
        {
            return;
        }

        var sanitized = Sanitize(Value);
        _sanitizedValue = sanitized;

        if (sanitized != Value)
        {
            CurrentValue = sanitized;
        }
    }

    // Sanitization is mandatory on every crossing to the host, and applies to markup rather than to a format that carries none.
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
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Add("forma-composer")
            .Add("forma-composer-disabled", Disabled)
            .Add("forma-composer-readonly", ReadOnly)
            .Add("forma-composer-resizable", Options.IsResizable && !Disabled)
            .Add("forma-composer-maximized", _chrome.IsMaximized);
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
        await _ownPlugins.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
