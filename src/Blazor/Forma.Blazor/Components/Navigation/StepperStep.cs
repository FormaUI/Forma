namespace Forma.Blazor.Components.Navigation;

/// <summary>A step a stepper shows: its label, the address it leads back to, and whether it is completed.</summary>
/// <param name="Label">The step's name.</param>
public sealed record StepperStep(string Label)
{
    /// <summary>The address the step leads to, drawn as a link on every step but the current one.</summary>
    public Uri? Url { get; init; }

    /// <summary>Whether the step is completed, drawn with a check in place of its number.</summary>
    public bool IsCompleted { get; init; }
}
