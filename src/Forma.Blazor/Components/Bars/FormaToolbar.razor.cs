namespace Forma.Blazor.Components.Bars;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Layout;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A toolbar of grouped, item-driven commands, built on <see cref="FormaCommandBar"/>.</summary>
public sealed partial class FormaToolbar : ComponentBase, IAsyncDisposable
{
    private static readonly ButtonOptions MenuTriggerOptions =
        new() { Variant = ButtonVariant.Ghost, Size = ButtonSize.Mini };

    /// <summary>The owner every toolbar action is qualified under, so a host remaps a toolbar shortcut by <c>toolbar.{command}</c>.</summary>
    private const string ActionOwner = "toolbar";

    private DotNetObjectReference<FormaToolbar>? _selfReference;
    private InputScope? _inputScope;
    private IReadOnlyList<(string Command, string Shortcut)> _registeredShortcuts = [];
    private string? _registeredTargetId;

    /// <summary>The accessible name of the toolbar.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The items the toolbar renders; a <see cref="FormaToolbarGroup"/> clusters the items it holds.</summary>
    [Parameter] public IReadOnlyList<FormaToolbarItem> Items { get; set; } = [];

    /// <summary>Whether every control in the toolbar is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether an item with an icon also shows its label; icon-only when false.</summary>
    [Parameter] public bool ShowLabels { get; set; }

    /// <summary>Callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The element a bound action's shortcut answers on while it or a descendant has focus. Shortcuts are inert until this is set — the toolbar never listens document-wide.</summary>
    [Parameter] public ElementReference? Target { get; set; }

    /// <summary>Content pinned past the end of the row, staying put while the commands scroll.</summary>
    [Parameter] public RenderFragment? TrailingContent { get; set; }

    /// <summary>Lays the items on a <see cref="FormaCommandBar"/> that scrolls them behind arrows once they outgrow the width; they wrap instead when false.</summary>
    [Parameter] public bool Scrollable { get; set; } = true;

    /// <summary>How the track carrying the items scrolls.</summary>
    [Parameter] public FreezePaneOptions CommandBarOptions { get; set; } = FreezePaneOptions.Arrows;

    /// <summary>Extra classes applied to the toolbar's root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Hides the toolbar while keeping it mounted, so a bound <see cref="Target"/>'s shortcuts come back across toggles.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Animates the transition between hidden and shown instead of snapping instantly.</summary>
    [Parameter] public bool Animated { get; set; }

    /// <summary>The states the host currently reports, by item id, which drive toggle state and action availability.</summary>
    [Parameter] public IReadOnlySet<string> ActiveStates { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-toolbar" : $"forma-toolbar {Class}";

    /// <summary>Every action that carries a shortcut, with the shortcut it carries.</summary>
    private IReadOnlyList<(string Command, string Shortcut)> Shortcuts
        => [.. Actions.Where(action => action.ShortcutAriaKeys is not null)
                      .Select(action => (action.Command, action.ShortcutAriaKeys!))];

    private IEnumerable<FormaToolbarAction> Actions
        => Items.SelectMany(Leaves).OfType<FormaToolbarAction>();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentNullException.ThrowIfNull(CommandBarOptions);

        foreach (var item in Items.SelectMany(Leaves))
        {
            if (item is FormaToolbarGroup nested)
            {
                throw new ArgumentException($"Toolbar group '{nested.Id}' is nested inside another group; groups do not nest.", nameof(Items));
            }

            if (item is FormaToolbarMenu { Options.Count: 0 } menu)
            {
                throw new ArgumentException($"Toolbar menu '{menu.Id}' must offer at least one option.", nameof(Items));
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Target is an element elsewhere, so hiding the toolbar does not stand its shortcuts down on its own.
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

        var profile = new InputProfile { Routes = ToRoutes(shortcuts) };

        // The same element keeps its scope and takes the new shortcuts; another element starts afresh.
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

        if (await InputRouter.RegisterAsync(target, profile, _selfReference) is { } registered)
        {
            _inputScope = registered;
            _registeredTargetId = target.Id;
            _registeredShortcuts = shortcuts;
        }
    }

    /// <summary>Runs the command whose shortcut the router matched, unless the toolbar or the matching action is disabled.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var command = Actions.Select(candidate => candidate.Command).FirstOrDefault(candidate => ActionOf(candidate) == action);

        return command is null ? Task.CompletedTask : HandleCommandAsync(command);
    }

    /// <summary>Runs a command, unless the toolbar or the action carrying it is disabled.</summary>
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
    public async ValueTask DisposeAsync()
    {
        await ReleaseAsync();
        _selfReference?.Dispose();
    }

    /// <summary>Releases the current scope, so a cleared target leaves no routes behind on the element it named.</summary>
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

    /// <summary>Every item a top-level entry contributes, so a group is checked through its own contents.</summary>
    private static IEnumerable<FormaToolbarItem> Leaves(FormaToolbarItem item)
        => item is FormaToolbarGroup group ? group.Items : [item];

    private static string ActionOf(string command)
        => InputRoute.Qualify(ActionOwner, command);

    /// <summary>One route per shortcut-bearing action, its command qualified as the action and its shortcut parsed into the binding.</summary>
    private static IReadOnlyList<InputRoute> ToRoutes(IReadOnlyList<(string Command, string Shortcut)> shortcuts)
        => [.. shortcuts
            .Select(entry => (Action: ActionOf(entry.Command), Binding: ParseShortcut(entry.Shortcut)))
            .Where(entry => entry.Binding is not null)
            .Select(entry => new InputRoute(entry.Action, entry.Binding!))];

    /// <summary>Parses a shortcut written as aria-keyshortcuts, such as "Control+Shift+B", into a binding, or null when it names no key; Control and Meta both stand for the platform's shortcut key.</summary>
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
        => Items.Select(item => item is FormaToolbarGroup group
            ? new Row(group.Id, group, group.Items)
            : new Row(item.Id, null, [item]));

    private FormaToolbarOption GetActiveOption(FormaToolbarMenu menu)
        => menu.Options.FirstOrDefault(IsOptionActive) ?? menu.Options[0];

    /// <summary>Whether a toggle is on, because it says so itself or because the host reports its id active.</summary>
    private bool IsToggleActive(FormaToolbarAction action)
        => action is FormaToolbarToggle toggle && (toggle.IsActive || ActiveStates.Contains(toggle.Id));

    /// <summary>Whether a menu option is the chosen one, by its own say-so or because the host reports its id active.</summary>
    private bool IsOptionActive(FormaToolbarOption option)
        => option.IsActive || ActiveStates.Contains(option.Id);

    /// <summary>Whether an action cannot be run, because it is disabled outright or the state it needs is absent.</summary>
    private bool IsUnavailable(FormaToolbarAction action)
        => action.IsDisabled || (action.RequiresState is { } state && !ActiveStates.Contains(state));

    private string GetTooltip(FormaToolbarItem item)
    {
        var label = item.Tooltip ?? (item is FormaToolbarAction action ? EffectiveTitle(action) : item.Title);

        return item is FormaToolbarAction { ShortcutDisplay: { } shortcut }
            ? $"{label} ({shortcut})"
            : label;
    }

    /// <summary>The label an action currently shows: a toggle's <see cref="FormaToolbarToggle.AlternateTitle"/> while it is on, otherwise its own title.</summary>
    private string EffectiveTitle(FormaToolbarAction action)
        => action is FormaToolbarToggle { AlternateTitle: { } alternateTitle } && IsToggleActive(action) ? alternateTitle : action.Title;

    /// <summary>The icon an action currently shows: a toggle's <see cref="FormaToolbarToggle.AlternateIcon"/> while it is on, otherwise its own icon.</summary>
    private Icon? EffectiveIcon(FormaToolbarAction action)
        => action is FormaToolbarToggle { AlternateIcon: { } alternateIcon } && IsToggleActive(action) ? alternateIcon : action.Icon;

    /// <summary>The hover text for an action, or null when its visible label already says the same thing.</summary>
    private string? GetActionTitle(FormaToolbarAction action, bool showsLabel)
    {
        var tooltip = GetTooltip(action);

        return showsLabel && tooltip == EffectiveTitle(action) ? null : tooltip;
    }

    private static Dictionary<string, object>? ToParameters(IReadOnlyDictionary<string, object>? parameters)
        => parameters is null ? null : new Dictionary<string, object>(parameters);

    /// <summary>One rendered run of items: a group and its contents, or a single ungrouped item.</summary>
    private sealed record Row(string Key, FormaToolbarGroup? Group, IReadOnlyList<FormaToolbarItem> Items);
}
