namespace Forma.Blazor.Showcase.Components.Pages.Navigation;

using Forma.Blazor.Components.Navigation;
using Forma.Framework.Culture.Router;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaCommandPalettePage
{
    // The shortcut the Showcase routes ahead of every component, Ctrl+K.
    private const string PaletteAction = "app.palette";

    private IReadOnlyList<PaletteCommand> _commands = [];
    private bool _isOpen;
    private string? _lastAction;

    [Inject] private CultureRouter Router { get; set; } = default!;

    [Inject] private CultureSwitcher Switcher { get; set; } = default!;

    [Inject] private ShowcaseInputActions HostActions { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        HostActions.Changed += HandleHostAction;
        BuildCommands();
    }

    /// <inheritdoc />
    protected override Task OnCultureChangedAsync()
    {
        BuildCommands();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            HostActions.Changed -= HandleHostAction;
        }

        base.Dispose(disposing);
    }

    private void OpenPalette()
        => _isOpen = true;

    // The router performs the Showcase's shortcut off the renderer.
    private void HandleHostAction()
    {
        if (HostActions.LastAction == PaletteAction)
        {
            _ = InvokeAsync(() =>
            {
                _isOpen = true;
                StateHasChanged();
            });
        }
    }

    // Every page the sidebar lists goes to its address in the culture shown, and the two actions say they ran.
    private void BuildCommands()
    {
        var pages = ShowcaseCatalog.Sidebar.SelectMany(category => category.Select(page => new PaletteCommand(page.Title)
        {
            Place = category.Key,
            Url = new Uri(Router.GetUrl(Switcher.CurrentRoute, page.Route.TrimStart('/')), UriKind.Relative),
        }));

        PaletteCommand[] actions =
        [
            new(L["Say Hello"]) { Place = L["Actions"], Icon = Glyph.Message, Run = () => RecordAsync(L["Say Hello"]) },
            new(L["Count to Three"]) { Place = L["Actions"], Icon = Glyph.Refresh, Run = () => RecordAsync(L["Count to Three"]) },
        ];

        _commands = [.. actions, .. pages];
    }

    private Task RecordAsync(string action)
    {
        _lastAction = action;
        StateHasChanged();

        return Task.CompletedTask;
    }
}
