namespace Forma.Blazor.Components.Navigation;

using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Interaction;
using Forma.Blazor.Components.Virtualization;
using Forma.Framework.Browser;
using Forma.Framework.Browser.Interop.Text;
using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;

/// <summary>A dialog searching the commands a host declares, running the one the reader picks.</summary>
/// <remarks>
/// The host opens the palette, from a button or from a shortcut it routes through the input router, and hands it the commands the
/// reader may run. Every word typed narrows them to those whose name or place holds it, a name starting with the query first; the
/// arrows move through them, Enter or a press runs one, and Escape closes the palette, which returns the focus to where it was opened
/// from. Running a command closes the palette first, then goes to its address or runs its action.
/// </remarks>
public sealed partial class FormaCommandPalette : FormaElementBase
{
    private const string NextAction = "command-palette.next";
    private const string PreviousAction = "command-palette.previous";
    private const string RunAction = "command-palette.run";

    // The arrows move through the results and Enter runs the one they rest on; Escape is the dialog's, which closes the palette.
    private static readonly InputProfile SearchProfile = new()
    {
        InputRoutes =
        [
            new InputRoute(NextAction, new KeyBinding("ArrowDown")),
            new InputRoute(PreviousAction, new KeyBinding("ArrowUp")),
            new InputRoute(RunAction, new KeyBinding("Enter")),
        ],
    };

    private static readonly TextEditOptions EditOptions = new() { SendsOnInput = true };

    private static readonly DialogOptions PaletteDialogOptions = new() { ShowCancelAction = false, ShowConfirmAction = false };

    /// <summary>The commands the reader may run, listed in this order before anything is typed.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<PaletteCommand> Commands { get; set; } = [];

    /// <summary>Whether the palette is open.</summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>Invoked as the palette closes itself, on Escape, a press away or a command run.</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>The accessible name of the palette.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>The hint the search field shows while it is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private InputRegistration _inputRegistration = default!;
    private DomTextEdits _edits = default!;
    private ElementReference _search;
    private string _query = string.Empty;
    private IReadOnlyList<PaletteCommand>? _rankedCommands;
    private IReadOnlyList<PaletteEntry> _results = [];
    private int _activeIndex;
    private bool _isOpen;
    private string? _announcement;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Dialog is null ? default : Dialog.RootElement;

    private FormaDialog Dialog { get; set; } = default!;

    private FormaVirtualize<PaletteEntry>? Virtualizer { get; set; }

    private string EffectiveAriaLabel
        => AriaLabel ?? L["Command Palette"].Value;

    private string EffectivePlaceholder
        => Placeholder ?? L["Type a command or where it lives"].Value;

    private string? ShownQuery
        => _edits.GetShownText(_query);

    private string SearchId
        => $"{ElementId}-search";

    private string ListId
        => $"{ElementId}-list";

    private string? ActiveOptionId
        => _activeIndex < _results.Count ? GetOptionId(_activeIndex) : null;

    // The active result stays drawn wherever the list scrolls, so the field always names an element that is there.
    private object[] PinnedIndexes
        => _activeIndex < _results.Count ? [_activeIndex] : [];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _inputRegistration = new InputRegistration(InputRouter, Context.JS, HandleActionAsync);
        _edits = new DomTextEdits(Context.DomEngine, _query, HandleQueryChangedAsync);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Commands);

        foreach (var command in Commands)
        {
            ArgumentNullException.ThrowIfNull(command);

            if ((command.Url is null) == (command.Run is null))
            {
                throw new ArgumentException($"The command '{command.Name}' must have exactly one of {nameof(PaletteCommand.Url)} and {nameof(PaletteCommand.Run)}.", nameof(Commands));
            }
        }

        // Each opening starts from an empty search over every command; a render of the host's that hands the same commands keeps the
        // result the arrows rest on.
        var isOpening = Open && !_isOpen;

        if (isOpening)
        {
            _query = string.Empty;
            _announcement = null;
        }

        _isOpen = Open;

        if (isOpening || !ReferenceEquals(Commands, _rankedCommands))
        {
            Rank();
        }

        base.OnParametersSet();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _edits.SyncAsync(_search, _query, EditOptions);
        await _inputRegistration.SyncAsync(_search, SearchProfile, Open);
    }

    private async Task HandleQueryChangedAsync(TextChange change)
    {
        if (change.Text == _query)
        {
            return;
        }

        _query = change.Text;
        Rank();
        _announcement = _results.Count == 0
            ? L["No commands match."].Value
            : L["[$ plural; one: # command; other: # commands; $]", _results.Count].Value;
        StateHasChanged();

        if (Virtualizer is { } virtualizer && _results.Count > 0)
        {
            await virtualizer.ScrollToIndexAsync(0);
        }
    }

    private Task HandleActionAsync(string action, InputInfo input)
        => action switch
        {
            NextAction => MoveAsync(1),
            PreviousAction => MoveAsync(-1),
            RunAction when _activeIndex < _results.Count => RunAsync(_results[_activeIndex].Command),
            _ => Task.CompletedTask,
        };

    // The arrows go round the results, from the last to the first and back.
    private async Task MoveAsync(int step)
    {
        if (_results.Count == 0)
        {
            return;
        }

        _activeIndex = (_activeIndex + step + _results.Count) % _results.Count;
        StateHasChanged();

        if (Virtualizer is { } virtualizer)
        {
            await virtualizer.ScrollToIndexAsync(_activeIndex);
        }
    }

    private async Task RunAsync(PaletteCommand command)
    {
        await CloseAsync();

        if (command.Url is { } url)
        {
            Navigation.NavigateTo(url.OriginalString);
        }
        else if (command.Run is { } run)
        {
            await run();
        }
    }

    private async Task CloseAsync()
    {
        _isOpen = false;
        await OpenChanged.InvokeAsync(false);
    }

    private void Rank()
    {
        _rankedCommands = Commands;
        var ranked = QueryRanking.Rank(Commands, command => command.Name, command => command.Place, _query, Context.CultureState.UICulture);
        _results = [.. ranked.Select((command, index) => new PaletteEntry(index, command))];
        _activeIndex = 0;
    }

    private string GetOptionId(int index)
        => string.Create(CultureInfo.InvariantCulture, $"{ElementId}-option-{index}");

    private string GetOptionClass(int index)
        => new CssClassBuilder()
            .Add("forma-command-palette-option")
            .Add("is-active", index == _activeIndex)
            .ToString();

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-command-palette");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _inputRegistration.DisposeAsync();
        await _edits.DisposeAsync();
        await base.DisposeAsyncCore();
    }

    /// <summary>A command among the results, at its place in them.</summary>
    private sealed record PaletteEntry(int Index, PaletteCommand Command);
}
