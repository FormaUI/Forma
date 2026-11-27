namespace Forma.Blazor.Components.Themes;

using Forma.Blazor.Components.Inputs;
using Forma.Framework.Css;
using Forma.Framework.Culture.Browser.Interop;
using Forma.Framework.Localization;
using Forma.Framework.Themes;
using Forma.Framework.Themes.Browser.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

/// <summary>The reader's choice of theme and color scheme: the registered themes the page can draw, and the System, Light and Dark preferences.</summary>
/// <remarks>
/// Interactive, it switches the page through the theme system's switch at each change, in place; statically rendered, it is a
/// plain form that posts the choice to the endpoint <c>MapFormaThemes()</c> maps, with an Apply button. It leaves the theme list out
/// where the host registered one theme, and, interactive, leaves out a theme whose stylesheet didn't load on the page.
/// </remarks>
public sealed partial class FormaThemePicker : FormaElementBase
{
    // The address the theme system's endpoint takes a switch at, relative to the document's base.
    private const string SwitchAddress = "_forma/themes";

    private IReadOnlyList<string>? _selectableThemes;

    [Inject] private ThemeRegistry Registry { get; set; } = default!;

    [Inject] private IThemeState ThemeState { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    private ThemeChoice Choice
        => ThemeState.Choice;

    private string SchemeValue
        => ThemeAttributes.GetSchemeValue(Choice.Scheme);

    // The page's own address in the reader's culture, which the endpoint sends the reader back to.
    private string ReturnAddress
    {
        get
        {
            var root = new Uri(Navigation.BaseUri).AbsolutePath;
            var relativeAddress = Navigation.ToBaseRelativePath(Navigation.Uri);

            return Services.GetService<ICultureAddressing>()?.GetAddress(root, relativeAddress, Context.CultureState.Culture, Context.CultureState.UICulture) ?? root + relativeAddress;
        }
    }

    private IReadOnlyList<SelectOption<string>> ThemeOptions
        => [.. Registry.Themes
            .Where(theme => _selectableThemes is null || _selectableThemes.Contains(theme.Name, StringComparer.Ordinal) || theme.Name == Choice.Theme)
            .Select(theme => new SelectOption<string>(theme.Name, Localize(theme.DisplayName)))];

    private IReadOnlyList<SelectOption<string>> SchemeOptions
        =>
        [
            new(ThemeAttributes.GetSchemeValue(ColorSchemePreference.System), L["System"]),
            new(ThemeAttributes.GetSchemeValue(ColorSchemePreference.Light), L["Light"]),
            new(ThemeAttributes.GetSchemeValue(ColorSchemePreference.Dark), L["Dark"]),
        ];

    /// <inheritdoc />
    protected override void OnInitialized()
        => ThemeState.Subscribe(RenderAgainAsync);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Services.GetService<ThemeCoordinatorRegistration>() is { } registration)
        {
            _selectableThemes = await registration.GetSelectableThemesAsync();

            if (_selectableThemes is not null)
            {
                StateHasChanged();
            }
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-theme-picker");

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        ThemeState.Unsubscribe(RenderAgainAsync);

        return base.DisposeAsyncCore();
    }

    private Task ChangeThemeAsync(string theme)
        => SwitchAsync(new ThemeChoice(theme, Choice.Scheme));

    private Task ChangeSchemeAsync(string scheme)
        => ThemeAttributes.TryParseScheme(scheme, out var preference) ? SwitchAsync(new ThemeChoice(Choice.Theme, preference)) : Task.CompletedTask;

    // A switch the page refuses leaves the choice in force, which the picker shows again.
    private async Task SwitchAsync(ThemeChoice choice)
    {
        if (Services.GetService<IThemeSwitch>() is not { } themeSwitch)
        {
            return;
        }

        try
        {
            await themeSwitch.SwitchAsync(choice);
        }
        catch (Exception exception) when (exception is InvalidOperationException or TaskCanceledException)
        {
            ThemesLog.SwitchRefused(Context.Logger, choice.Theme, exception);
            StateHasChanged();
        }
    }

    private string Localize(LocalizableText text)
        => Services.GetService<IStringLocaleFactory>() is { } factory ? factory.Localize(text, Context.CultureState) : text.Key;

    private Task RenderAgainAsync()
        => InvokeAsync(StateHasChanged);
}
