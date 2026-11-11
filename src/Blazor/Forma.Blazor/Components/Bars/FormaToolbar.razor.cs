namespace Forma.Blazor.Components.Bars;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Layout;
using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Focus;
using Forma.Framework.Css;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A toolbar of items and groups of items.</summary>
public sealed partial class FormaToolbar : FormaElementBase
{
    private static readonly ButtonOptions MenuTriggerOptions =
        new() { Variant = ButtonVariant.Ghost, Size = ButtonSize.Mini };

    /// <summary>The owner every toolbar action is qualified under, which names an action <c>toolbar.{command}</c>.</summary>
    private const string ActionOwner = "toolbar";

    private DotNetObjectReference<FormaToolbar>? _selfReference;
    private InputScope? _inputScope;
    private IReadOnlyList<(string Command, string Shortcut)> _registeredShortcuts = [];
    private string? _registeredTargetId;
    private static readonly FocusInteractionOptions RovingOptions = new() { RovingOrientation = FocusOrientation.Horizontal };

    private DomInteraction<FocusInteractionOptions>? _rovingFocus;

    /// <summary>The accessible name of the toolbar.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The items the toolbar renders.</summary>
    [Parameter] public IReadOnlyList<ToolbarItem> Items { get; set; } = [];

    /// <summary>Whether every control in the toolbar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether an item with an icon also shows its label.</summary>
    [Parameter] public bool ShowLabels { get; set; }

    /// <summary>The callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The element the actions' shortcuts answer on while it or a descendant has focus, where null leaves the shortcuts unbound.</summary>
    [Parameter] public ElementReference? Target { get; set; }

    /// <summary>The content pinned past the end of the row.</summary>
    [Parameter] public RenderFragment? TrailingContent { get; set; }

    /// <summary>Whether the items scroll behind arrows, rather than wrap, once they outgrow the width.</summary>
    [Parameter] public bool Scrollable { get; set; } = true;

    /// <summary>The options of the command bar that scrolls the items.</summary>
    [Parameter] public FreezePaneOptions CommandBarOptions { get; set; } = FreezePaneOptions.Arrows;

    /// <summary>Whether the toolbar is hidden while it stays mounted.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Whether the toolbar animates between hidden and shown.</summary>
    [Parameter] public bool Animated { get; set; }

    /// <summary>The states the host currently reports active, by id.</summary>
    [Parameter] public IReadOnlySet<string> ActiveStates { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private FormaCommandBar CommandBar { get; set; } = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Scrollable && CommandBar is not null ? CommandBar.RootElement : base.RootElement;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <summary>Every action that carries a shortcut, with the shortcut it carries.</summary>
    private IReadOnlyList<(string Command, string Shortcut)> Shortcuts
        => [.. Actions.Where(action => action.ShortcutAriaKeys is not null)
                      .Select(action => (action.Command, action.ShortcutAriaKeys!))];

    private IEnumerable<ToolbarAction> Actions
        => Items.SelectMany(GetLeaves).OfType<ToolbarAction>();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentNullException.ThrowIfNull(CommandBarOptions);

        foreach (var item in Items.SelectMany(GetLeaves))
        {
            if (item is ToolbarGroup nestedGroup)
            {
                throw new ArgumentException($"Toolbar group '{nestedGroup.Id}' is nested inside another group; groups do not nest.", nameof(Items));
            }

            if (item is ToolbarMenu { Options.Count: 0 } menu)
            {
                throw new ArgumentException($"Toolbar menu '{menu.Id}' must offer at least one option.", nameof(Items));
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await RoveAsync(firstRender);

        // Target lies outside the toolbar, so hiding the toolbar releases its shortcuts here.
        // An unrendered @ref still arrives as a non-null ElementReference carrying no id, and registering it fails silently.
        if (Hidden || Target is not { Id.Length: > 0 } target)
        {
            await ReleaseAsync();

            return;
        }

        var shortcuts = Shortcuts;

        if (target.Id == _registeredTargetId && shortcuts.SequenceEqual(_registeredShortcuts))
        {
            return;
        }

        var profile = new InputProfile { InputRoutes = ToRoutes(shortcuts) };

        if (_inputScope is { } inputScope && target.Id == _registeredTargetId)
        {
            if (await inputScope.UpdateAsync(profile))
            {
                _registeredShortcuts = shortcuts;
            }

            return;
        }

        await ReleaseAsync();
        _selfReference ??= DotNetObjectReference.Create(this);

        if (await InputRouter.RegisterAsync(target, profile, _selfReference) is { } registeredScope)
        {
            _inputScope = registeredScope;
            _registeredTargetId = target.Id;
            _registeredShortcuts = shortcuts;
        }
    }

    /// <summary>Runs the command of the action whose shortcut the router matched.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var command = Actions.Select(candidate => candidate.Command).FirstOrDefault(candidate => GetAction(candidate) == action);

        return command is null ? Task.CompletedTask : HandleCommandAsync(command);
    }

    /// <summary>Runs a command one of the toolbar's available actions carries.</summary>
    public async Task HandleCommandAsync(string command)
    {
        if (Disabled)
        {
            return;
        }

        var action = Actions.FirstOrDefault(a => a.Command == command);

        if (action is null || IsUnavailable(action))
        {
            return;
        }

        await OnCommand.InvokeAsync(command);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-toolbar");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await ReleaseAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }

    /// <summary>Makes the root one Tab stop whose arrow keys move among the controls.</summary>
    private async Task RoveAsync(bool firstRender)
    {
        if (firstRender)
        {
            _rovingFocus = await Context.DomEngine.GetElement(RootElement).Focus.AttachAsync(RovingOptions);
        }

        if (_rovingFocus is { } rovingFocus)
        {
            await rovingFocus.SyncAsync(RootElement, RovingOptions);
        }
    }

    /// <summary>Releases the current input scope and the shortcuts registered on it.</summary>
    private async Task ReleaseAsync()
    {
        _registeredShortcuts = [];
        _registeredTargetId = null;

        if (_inputScope is { } inputScope)
        {
            _inputScope = null;
            await inputScope.DisposeAsync();
        }
    }

    /// <summary>The items a top-level item contributes: a group's own items, or the item itself.</summary>
    private static IEnumerable<ToolbarItem> GetLeaves(ToolbarItem item)
        => item is ToolbarGroup group ? group.Items : [item];

    private static string GetAction(string command)
        => InputRoute.Qualify(ActionOwner, command);

    /// <summary>The input routes that bind each shortcut to its qualified action.</summary>
    private static IReadOnlyList<InputRoute> ToRoutes(IReadOnlyList<(string Command, string Shortcut)> shortcuts)
        => [.. shortcuts
            .Select(entry => (Action: GetAction(entry.Command), Binding: ParseShortcut(entry.Shortcut)))
            .Where(entry => entry.Binding is not null)
            .Select(entry => new InputRoute(entry.Action, entry.Binding!))];

    /// <summary>Parses a shortcut written as aria-keyshortcuts, such as "Control+Shift+B", into a key binding.</summary>
    /// <remarks>Control and Meta both stand for the platform's shortcut key.</remarks>
    private static KeyBinding? ParseShortcut(string shortcut)
    {
        var parts = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return null;
        }

        var modifiers = parts[..^1];

        return new KeyBinding(parts[^1])
        {
            Modifiers = new ModifierRequirement
            {
                Primary = modifiers.Any(m => m.Equals("Control", StringComparison.OrdinalIgnoreCase) || m.Equals("Meta", StringComparison.OrdinalIgnoreCase)),
                Alt = modifiers.Any(m => m.Equals("Alt", StringComparison.OrdinalIgnoreCase)),
                Shift = modifiers.Any(m => m.Equals("Shift", StringComparison.OrdinalIgnoreCase)),
            },
        };
    }

    private IEnumerable<Row> Rows
        => Items.Select(item => item is ToolbarGroup group
            ? new Row(group.Id, group, group.Items)
            : new Row(item.Id, null, [item]));

    /// <summary>The option the menu shows as chosen.</summary>
    private ToolbarOption? GetActiveOption(ToolbarMenu menu)
        => menu.Options.FirstOrDefault(IsOptionActive);

    /// <summary>Whether the menu chooses among swatches.</summary>
    /// <remarks>A swatch menu with an icon shows the icon alone while no swatch is chosen.</remarks>
    private static bool OffersSwatches(ToolbarMenu menu)
        => menu.Options.Any(option => option.Swatch is not null);

    /// <summary>Whether a toggle is on, because it says so itself or because the host reports its id active.</summary>
    private bool IsToggleActive(ToolbarAction action)
        => action is ToolbarToggle toggle && (toggle.IsActive || ActiveStates.Contains(toggle.Id));

    /// <summary>Whether a menu option is the chosen one, because it says so itself or because the host reports its id active.</summary>
    private bool IsOptionActive(ToolbarOption option)
        => option.IsActive || ActiveStates.Contains(option.Id);

    /// <summary>Whether an action cannot be run, because it is disabled outright or the state it needs is absent.</summary>
    private bool IsUnavailable(ToolbarAction action)
        => action.IsDisabled || (action.RequiresState is { } state && !ActiveStates.Contains(state));

    private string GetTooltip(ToolbarItem item)
    {
        var label = item.Tooltip ?? (item is ToolbarAction action ? GetEffectiveTitle(action) : item.Title);

        return item is ToolbarAction { ShortcutDisplay: { } shortcut }
            ? $"{label} ({shortcut})"
            : label;
    }

    /// <summary>The label an action currently shows: a toggle's <see cref="ToolbarToggle.AlternateTitle"/> while it is on, otherwise its own title.</summary>
    private string GetEffectiveTitle(ToolbarAction action)
        => action is ToolbarToggle { AlternateTitle: { } alternateTitle } && IsToggleActive(action) ? alternateTitle : action.Title;

    /// <summary>The icon an action currently shows: a toggle's <see cref="ToolbarToggle.AlternateIcon"/> while it is on, otherwise its own icon.</summary>
    private Icon? GetEffectiveIcon(ToolbarAction action)
        => action is ToolbarToggle { AlternateIcon: { } alternateIcon } && IsToggleActive(action) ? alternateIcon : action.Icon;

    /// <summary>The hover text for an action, where null means its visible label already says it.</summary>
    private string? GetActionTitle(ToolbarAction action, bool showsLabel)
    {
        var tooltip = GetTooltip(action);

        return showsLabel && tooltip == GetEffectiveTitle(action) ? null : tooltip;
    }

    // A custom item's component, rendered by its type with its parameters, keyed so a reordered toolbar keeps each one's state.
    private static RenderFragment RenderCustomItem(ToolbarCustomItem customItem)
        => builder =>
        {
            builder.OpenComponent(0, customItem.Component);
            builder.SetKey(customItem.Id);

            foreach (var (name, value) in customItem.Parameters ?? new Dictionary<string, object>())
            {
                builder.AddComponentParameter(1, name, value);
            }

            builder.CloseComponent();
        };

    /// <summary>One rendered run of items: a group and its contents, or a single ungrouped item.</summary>
    private sealed record Row(string Key, ToolbarGroup? Group, IReadOnlyList<ToolbarItem> Items);
}
