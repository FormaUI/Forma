namespace Forma.Blazor.Components.Composer;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Composer.Assets;
using Forma.Blazor.Components.Composer.PlainTextEditor;
using Forma.Blazor.Components.Composer.Plugins;
using Forma.Blazor.Components.Feedback;
using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Focus;
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

/// <summary>The composer, an input that edits a document through the editor plugins it hosts.</summary>
/// <param name="syntaxHighlighter">The host's syntax highlighter, where null classifies no code.</param>
/// <param name="definition">The composer definition the application registered, where null means only the fallback editor.</param>
public sealed partial class FormaComposer(
    IComposerSyntaxHighlighter? syntaxHighlighter = null,
    ComposerDefinition? definition = null)
    : FormaInputBase<ComposerDocument>, IComposerInteraction, IComposerDialogInteraction
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
    private static readonly FocusInteractionOptions TabOrderOptions = new() { RegionSelectors = [".forma-composer-canvas"] };
    private JSComposerAssetLoader _assetLoader = default!;
    private IComposerHistory _history = default!;
    private readonly IComposerSyntaxHighlighter _syntaxHighlighter = syntaxHighlighter ?? NullSyntaxHighlighter.Instance;
    private IComposerEditorPlugin _activeEditor = default!;
    private Func<Task<bool>>? _settleEditorEditsAsync;
    private IReadOnlyList<IComposerViewPlugin>? _activeViews;
    private IComposerViewPlugin? _activeView;
    private IReadOnlyList<IComposerPlugin> _surfacePlugins = [];
    private bool _isInitialized;
    private bool _isJsStarted;
    private readonly ComposerChrome _chrome = new();
    private bool _isAttachOpen;
    private bool _isShortcutsOpen;
    private ComposerInsertionRequest? _dialog;
    private Dictionary<string, object>? _dialogParameters;
    private object? _dialogInstance;
    private bool _isDialogBodyReady;
    private string? _dialogRefusal;

    // The room the last dialog opened was given, which its shell keeps once it has closed.
    private ComposerDialogSize? _dialogShellSize;

    // The confirmation of the open dialog under way, which a confirmation asked for before it ends is.
    private Task? _dialogConfirmation;

    // Cancels what the open dialog's body does to answer a confirmation once the dialog closes.
    private CancellationTokenSource? _dialogCancellation;

    /// <summary>The options of the composer.</summary>
    [Parameter] public ComposerOptions Options { get; set; } = ComposerOptions.Default;

    /// <summary>The definition of this composer alone, in place of the one the application registered.</summary>
    [Parameter] public ComposerDefinition? Plugins { get; set; }

    /// <summary>The identifier of the active editor plugin.</summary>
    [Parameter] public string? ActiveEditorId { get; set; }

    /// <summary>Callback invoked when the active editor plugin changes.</summary>
    [Parameter] public EventCallback<string> ActiveEditorIdChanged { get; set; }

    /// <summary>Whether the composer is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the composer is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The undo and redo history of this composer, where null builds one holding <see cref="ComposerOptions.HistoryCapacity"/> steps.</summary>
    [Parameter] public IComposerHistory? History { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;
    [Inject] private IHtmlSanitizer Sanitizer { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private IEnumerable<IComposerDocumentConverter> RegisteredConverters { get; set; } = default!;

    private bool ShowToolbar
        => Options.ShowToolbar && !ReadOnly;

    private bool ShowTabs
        => Options.ShowEditorSwitcher && (_pluginCatalog.Editors.Count > 1 || ActiveViews.Count > 0);

    private IReadOnlyList<IComposerViewPlugin> ActiveViews
        => _activeViews ??= _pluginCatalog.GetViews(_activeEditor);

    private string ActiveTabId
        => _activeView?.Id ?? _activeEditor.Id;

    private ComposerPluginLocalizer Localizer
        => field ??= new ComposerPluginLocalizer(Services.GetStringLocaleFactory(), L);

    private ComposerDocument CurrentDocument
        => Value ?? new ComposerDocument(ComposerDocumentFormat.Html, string.Empty);

    private ComposerContext? _context;
    private ComposerDocument? _sanitizedValue;
    private DocumentConverterRegistry _converterRegistry = new();

    // The composer's own scope answers the shortcuts of every plugin in play.
    private InputProfile RootProfile
        => new() { InputRoutes = _keyBindingCatalog.ToInputRoutes() };

    // The same instance comes back until a member changes.
    private ComposerContext? Composer
    {
        get
        {
            if (!_isInitialized)
            {
                return null;
            }

            var builtContext = BuildComposer();

            if (builtContext != _context)
            {
                _context = builtContext;
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
        JoinForm(SettleEditorAsync);
        _assetLoader = new JSComposerAssetLoader(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);

        _history = History ?? new ComposerHistory(Options.HistoryCapacity);

        _commandRegistry.RegisterEngineCommand(ComposerCommands.Undo, HandleUndoAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.Redo, HandleRedoAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.Maximize, HandleMaximizeAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.SelectionBar, HandleSelectionBarAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.Attach, HandleAttachAsync);
        _commandRegistry.RegisterEngineCommand(ComposerCommands.Shortcuts, HandleShortcutsAsync);
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

    // A view shows the document as the reader left it, and takes the editor's surface, whose edits go with it.
    private async Task ShowViewAsync(IComposerViewPlugin? view)
    {
        if (view is not null && !await SettleEditorAsync())
        {
            return;
        }

        _activeView = view;

        await RescopeAsync();

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

    private Task HandleShortcutsAsync()
    {
        _isShortcutsOpen = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    // The attach dialog's options, from the file upload plugin in play.
    private FileUploadPluginOptions _attachOptions = FileUploadPluginOptions.Default;
    private async Task HandleAttachedAsync(IReadOnlyList<ResourceInfo> attachedResources)
    {
        _isAttachOpen = false;

        // The dialog hands back everything it has uploaded, which replaces any earlier copy of the same attachment.
        var keptAttachments = CurrentDocument.Attachments.Where(held => !attachedResources.Contains(held)).ToList();

        await HandleDocumentChangedAsync(CurrentDocument with { Attachments = [.. keptAttachments, .. attachedResources] });
    }

    private async Task LoadPluginsAsync()
    {
        // Each composer activates its own plugins, since an editor plugin carries events its canvas subscribes to.
        _ownPlugins = (Plugins ?? _definition ?? ComposerDefinition.Empty).Activate(Services);

        _pluginCatalog = await ComposerPluginCatalog.CreateAsync(_ownPlugins.Instances, new PlainTextEditorPlugin());
        _activeEditor = _pluginCatalog.ResolveEditor(ActiveEditorId, Options.DefaultEditorId, CurrentDocument.Format);
        _activeViews = null;

        RebuildScope();
    }

    // The plugins coming into play also answer the composer's shortcuts and have their assets loaded.
    private async Task RescopeAsync()
    {
        RebuildScope();

        if (!_isJsStarted)
        {
            return;
        }

        if (_inputScope is { } inputScope)
        {
            await inputScope.UpdateAsync(RootProfile);
        }

        await LoadSurfaceAssetsAsync();
    }

    // Every plugin in play on the active surface has its assets, those loaded eagerly at start among them.
    private ValueTask LoadSurfaceAssetsAsync()
        => _assetLoader.LoadAssetsAsync(_pluginCatalog.GetSurfacePlugins(_activeEditor, _activeView).SelectMany(plugin => plugin.Assets));

    // A command is answered by the plugins in play for the active editor and view.
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

        // Interop starts on the first render after the plugins have loaded.
        if (_isJsStarted || !_isInitialized)
        {
            return;
        }

        _isJsStarted = true;

        // The shortcuts route through the composer's scope; the active editor also registers them on its editable element, ahead of the native control.
        _inputScope = await InputRouter.RegisterAsync(RootElement, RootProfile, _selfReference);

        // Taken away while its shortcuts registered, it has no context left to arrange its tab order through.
        if (IsDisposed)
        {
            return;
        }

        // The canvas is where focus lands first; Tab then reaches the composer's own commands before leaving it.
        await Context.DomEngine.GetElement(RootElement).Focus.AttachAsync(TabOrderOptions);
        await _assetLoader.LoadAssetsAsync(_pluginCatalog.Plugins.SelectMany(plugin => plugin.Assets).Where(asset => asset.Loading == ComposerAssetLoading.Eager));
        await LoadSurfaceAssetsAsync();
    }

    /// <summary>Runs the composer command named by the action the router matched.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return HandleCommandAsync(action);
    }

    /// <summary>Runs a composer command by name.</summary>
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
            if (!await SettleEditorAsync() || Composer is not { } settledComposer)
            {
                return;
            }

            if (await _commandRegistry.ExecuteAsync(command, settledComposer, this) is ComposerCommandResult.Plugin)
            {
                StateHasChanged();
            }
        });
    }

    /// <summary>Switches the active editor plugin, converting the document to its format.</summary>
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

        // The document converts as the reader left it.
        if (!await SettleEditorAsync() || (target.Format != CurrentDocument.Format && !TryConvertTo(target.Format)))
        {
            return;
        }

        _activeEditor = target;
        _activeView = null;
        _activeViews = null;

        await RescopeAsync();

        if (ActiveEditorIdChanged.HasDelegate)
        {
            await ActiveEditorIdChanged.InvokeAsync(target.Id);
        }

        StateHasChanged();
    }

    // The editor in the surface brings the document in step with what the reader did in it, before anything reads it.
    private Task<bool> SettleEditorAsync()
        => _settleEditorEditsAsync?.Invoke() ?? Task.FromResult(true);

    private bool TryConvertTo(ComposerDocumentFormat format)
    {
        var conversion = _converterRegistry.Convert(CurrentDocument, format);

        if (!conversion.Succeeded)
        {
            return false;
        }

        var convertedDocument = new ComposerDocument(format, conversion.Content ?? string.Empty);
        _history.Record(convertedDocument, _selection);
        CurrentValue = convertedDocument;

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

        CloseDialog();

        _dialog = request;
        _dialogShellSize = request.Size;
        _dialogParameters = request.Parameters.ToDictionary(parameter => parameter.Key, parameter => parameter.Value!, StringComparer.Ordinal);
        _dialogCancellation = new CancellationTokenSource();

        return InvokeAsync(StateHasChanged);
    }

    IDisposable IComposerInteraction.JoinEdits(Func<Task<bool>> settleEditsAsync)
    {
        ArgumentNullException.ThrowIfNull(settleEditsAsync);

        _settleEditorEditsAsync = settleEditsAsync;

        return new EditsRegistration(this, settleEditsAsync);
    }

    void IComposerInteraction.ReportTextFault(bool isFaulted)
        => ReportTextFault(isFaulted, L["Your text didn't arrive in full. Edit it to try again."]);

    ValueTask<bool> IComposerInteraction.TryInsertAsync(ComposerInsertion insertion)
    {
        ArgumentNullException.ThrowIfNull(insertion);

        return Disabled || ReadOnly
            ? ValueTask.FromResult(false)
            : _activeEditor.TryInsertAsync(insertion);
    }

    // The dialog's body, rendered by its type with its parameters, its instance held so the composer reaches it.
    private RenderFragment RenderDialogBody(ComposerInsertionRequest dialog)
        => builder =>
        {
            builder.OpenComponent(0, dialog.Body);

            foreach (var (name, value) in _dialogParameters ?? [])
            {
                builder.AddComponentParameter(1, name, value);
            }

            builder.AddComponentReferenceCapture(2, instance => _dialogInstance = instance);
            builder.CloseComponent();
        };

    private IComposerInsertionDialog? DialogBody
        => _dialogInstance as IComposerInsertionDialog;

    // The body's messages, followed by the composer's refusal of an insertion no editor took.
    private IReadOnlyList<string> DialogMessages
    {
        get
        {
            var refusedMessages = DialogBody?.Messages ?? [];

            return _dialogRefusal is null ? refusedMessages : [.. refusedMessages, _dialogRefusal];
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

    Task IComposerDialogInteraction.ConfirmAsync()
        => ConfirmDialogAsync();

    private Task HandleDialogConfirmAsync()
        => ConfirmDialogAsync();

    // One confirmation of the open dialog runs at a time, whichever of its confirming action and its body asks for it.
    private Task ConfirmDialogAsync()
    {
        if (_dialogConfirmation is { IsCompleted: false } pending)
        {
            return pending;
        }

        return _dialogConfirmation = RunDialogConfirmationAsync();
    }

    // The body's answer goes into the document only while the dialog that asked for it is still open, and closes it once inserted.
    private async Task RunDialogConfirmationAsync()
    {
        if (_dialog is not { } dialog || DialogBody is not { } body || _dialogCancellation is not { } cancellation)
        {
            return;
        }

        _dialogRefusal = null;
        ComposerInsertion? insertion;

        try
        {
            insertion = await body.TryConfirmAsync(cancellation.Token);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return;
        }

        if (!ReferenceEquals(_dialog, dialog))
        {
            return;
        }

        if (insertion is not null)
        {
            var isInserted = await ((IComposerInteraction)this).TryInsertAsync(insertion);

            if (!ReferenceEquals(_dialog, dialog))
            {
                return;
            }

            if (isInserted)
            {
                CloseDialog();
            }
            else
            {
                _dialogRefusal = L["This could not be inserted into the document."].Value;
            }
        }

        StateHasChanged();
    }

    private void CloseDialog()
    {
        _dialog = null;
        _dialogParameters = null;
        _dialogInstance = null;
        _isDialogBodyReady = false;
        _dialogRefusal = null;
        _dialogConfirmation = null;

        _dialogCancellation?.Cancel();
        _dialogCancellation?.Dispose();
        _dialogCancellation = null;
    }

    private Task HandleDocumentChangedAsync(ComposerDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var sanitizedDocument = Sanitize(document);

        _history.Record(sanitizedDocument, _selection);
        CurrentValue = sanitizedDocument;
        StateHasChanged();

        return Task.CompletedTask;
    }

    private Task HandleRequestRenderAsync()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

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

    /// <summary>Sanitizes the document the host bound.</summary>
    private void SanitizeValue()
    {
        if (Value is null || Value == _sanitizedValue)
        {
            return;
        }

        var sanitizedValue = Sanitize(Value);
        _sanitizedValue = sanitizedValue;

        if (sanitizedValue != Value)
        {
            CurrentValue = sanitizedValue;
        }
    }

    // Every document crossing to the host is sanitized; only an HTML document carries markup.
    private ComposerDocument Sanitize(ComposerDocument document)
        => document.Format == ComposerDocumentFormat.Html
            ? document with { Content = Sanitizer.Sanitize(document.Content) }
            : document;

    private Task HandleUndoAsync()
    {
        var undoneDocument = _history.Undo();

        if (undoneDocument is not null)
        {
            CurrentValue = undoneDocument;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private Task HandleRedoAsync()
    {
        var redoneDocument = _history.Redo();

        if (redoneDocument is not null)
        {
            CurrentValue = redoneDocument;
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

        CloseDialog();
        _selfReference?.Dispose();
        await _assetLoader.DisposeAsync();
        await _ownPlugins.DisposeAsync();
        await base.DisposeAsyncCore();
    }

    /// <summary>The edits of the editor in the surface, joined to the composer until the editor leaves.</summary>
    private sealed class EditsRegistration(FormaComposer composer, Func<Task<bool>> settleEditsAsync) : IDisposable
    {
        public void Dispose()
        {
            if (composer._settleEditorEditsAsync == settleEditsAsync)
            {
                composer._settleEditorEditsAsync = null;
            }
        }
    }
}
