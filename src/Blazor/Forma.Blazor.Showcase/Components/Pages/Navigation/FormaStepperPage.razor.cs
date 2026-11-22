namespace Forma.Blazor.Showcase.Components.Pages.Navigation;

using System.Globalization;

using Forma.Blazor.Components.Navigation;
using Forma.Framework.Culture.Router;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaStepperPage
{
    private IReadOnlyList<StepperStep> _purchaseSteps = [];
    private IReadOnlyList<StepperStep> _indicatorSteps = [];
    private int _purchaseIndex = 1;

    [Inject] private CultureRouter Router { get; set; } = default!;

    [Inject] private CultureSwitcher Switcher { get; set; } = default!;

    // The purchase step a completed step's link names, read from the address it leads to.
    [SupplyParameterFromQuery(Name = "step")] private int? Step { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Step is { } step)
        {
            _purchaseIndex = Math.Clamp(step, 0, 3);
        }

        BuildSteps();
    }

    /// <inheritdoc />
    protected override Task OnCultureChangedAsync()
    {
        BuildSteps();
        return Task.CompletedTask;
    }

    private void GoBack()
    {
        _purchaseIndex--;
        BuildSteps();
    }

    private void GoForward()
    {
        _purchaseIndex++;
        BuildSteps();
    }

    // The steps' names follow the culture, and the purchase's steps left behind are completed, each linking back to itself.
    private void BuildSteps()
    {
        string[] names = [L["Find Your Event"], L["Choose Your Photos"], L["Pay"], L["Download without the Watermark"]];

        _purchaseSteps = [.. names.Select((name, index) => index < _purchaseIndex
            ? new StepperStep(name) { IsCompleted = true, Url = new Uri(Router.GetUrl(Switcher.CurrentRoute, string.Create(CultureInfo.InvariantCulture, $"stepper?step={index}")), UriKind.Relative) }
            : new StepperStep(name))];
        _indicatorSteps = [.. names.Select(name => new StepperStep(name))];
    }
}
