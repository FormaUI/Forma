namespace Forma.Blazor.Components.Interaction;

using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;

/// <summary>The keys a strip of tabs is walked by: the arrow along the reading direction to the next tab, the one against it to the previous, and Home and End to either end, wrapping around.</summary>
internal static class TabStripNavigation
{
    /// <summary>The strip's routes under <paramref name="owner"/>: <c>{owner}.next</c>, <c>{owner}.previous</c>, <c>{owner}.first</c> and <c>{owner}.last</c>.</summary>
    public static InputProfile CreateProfile(string owner, bool isRightToLeft)
        => new()
        {
            InputRoutes =
            [
                new InputRoute(InputRoute.Qualify(owner, "next"), new KeyBinding(isRightToLeft ? "ArrowLeft" : "ArrowRight")),
                new InputRoute(InputRoute.Qualify(owner, "previous"), new KeyBinding(isRightToLeft ? "ArrowRight" : "ArrowLeft")),
                new InputRoute(InputRoute.Qualify(owner, "first"), new KeyBinding("Home")),
                new InputRoute(InputRoute.Qualify(owner, "last"), new KeyBinding("End")),
            ],
        };

    /// <summary>The index <paramref name="action"/> moves to from <paramref name="current"/> among <paramref name="count"/> tabs, or <see langword="null"/> when the strip is empty or the action is not one of the strip's.</summary>
    public static int? FindTarget(string owner, string action, int current, int count)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (count == 0 || !action.StartsWith(owner + ".", StringComparison.Ordinal))
        {
            return null;
        }

        return action[(owner.Length + 1)..] switch
        {
            "next" => (current + 1) % count,
            "previous" => (current + count - 1) % count,
            "first" => 0,
            "last" => count - 1,
            _ => null,
        };
    }
}
