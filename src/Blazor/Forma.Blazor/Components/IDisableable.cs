namespace Forma.Blazor.Components;

/// <summary>A component that can be disabled, and say why.</summary>
/// <remarks>
/// A component disabled with a reason stays in the tab order and the pointer's reach: it writes <c>aria-disabled</c> in place
/// of the <c>disabled</c> attribute, does nothing when activated, and shows the reason as its tooltip, a
/// <see cref="Feedback.FormaTooltip"/> it names in its <c>aria-describedby</c>. One disabled without a reason is disabled
/// as the browser disables a control, and a reason on a component that isn't disabled shows nothing.
/// </remarks>
public interface IDisableable
{
    /// <summary>Whether the component is disabled.</summary>
    bool IsDisabled { get; }

    /// <summary>Why the component is disabled.</summary>
    string? DisabledReason { get; }
}
