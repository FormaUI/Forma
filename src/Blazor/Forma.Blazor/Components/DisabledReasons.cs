namespace Forma.Blazor.Components;

/// <summary>What an <see cref="IDisableable"/> component draws for its reason for being disabled.</summary>
public static class DisabledReasons
{
    /// <summary>Whether <paramref name="component"/> is disabled with a reason, which it shows as its tooltip and stays reachable for.</summary>
    /// <param name="component">The component.</param>
    public static bool IsShown(IDisableable component)
    {
        ArgumentNullException.ThrowIfNull(component);

        return component.IsDisabled && !string.IsNullOrWhiteSpace(component.DisabledReason);
    }

    /// <summary>The id of the tooltip showing the reason of the component whose element's id is <paramref name="elementId"/>.</summary>
    /// <param name="elementId">The id of the component's element.</param>
    public static string GetTooltipId(string elementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);

        return $"{elementId}-disabled-reason";
    }

    /// <summary>The ids the element of <paramref name="component"/> is described by: <paramref name="describedBy"/>, then its reason's tooltip where it shows one.</summary>
    /// <param name="component">The component.</param>
    /// <param name="elementId">The id of the component's element.</param>
    /// <param name="describedBy">The ids the element is described by already, such as its host's own.</param>
    public static string? AddTooltipId(IDisableable component, string elementId, string? describedBy)
    {
        if (!IsShown(component))
        {
            return describedBy;
        }

        var tooltipId = GetTooltipId(elementId);

        return string.IsNullOrWhiteSpace(describedBy) ? tooltipId : $"{describedBy} {tooltipId}";
    }
}
