namespace Forma.Blazor.Components.Interaction;

using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;

/// <summary>The keys a strip of tabs is walked by, under one owner: the arrow along the reading direction to the next tab, the one against it to the previous, and Home and End to either end, wrapping around.</summary>
/// <param name="owner">The owner the strip's actions are qualified under.</param>
internal sealed class TabStripNavigation(string owner)
{
    private readonly string _next = InputRoute.Qualify(owner, "next");
    private readonly string _previous = InputRoute.Qualify(owner, "previous");
    private readonly string _first = InputRoute.Qualify(owner, "first");
    private readonly string _last = InputRoute.Qualify(owner, "last");

    /// <summary>The strip's routes for the reading direction.</summary>
    public InputProfile CreateProfile(bool isRightToLeft)
        => new()
        {
            InputRoutes =
            [
                new InputRoute(_next, new KeyBinding(isRightToLeft ? "ArrowLeft" : "ArrowRight")),
                new InputRoute(_previous, new KeyBinding(isRightToLeft ? "ArrowRight" : "ArrowLeft")),
                new InputRoute(_first, new KeyBinding("Home")),
                new InputRoute(_last, new KeyBinding("End")),
            ],
        };

    /// <summary>The index <paramref name="action"/> moves to from <paramref name="current"/> among <paramref name="count"/> tabs, or <see langword="null"/> when the strip is empty or the action is not one of the strip's.</summary>
    public int? FindTarget(string action, int current, int count)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (count == 0)
        {
            return null;
        }

        if (action == _next)
        {
            return (current + 1) % count;
        }

        if (action == _previous)
        {
            return (current + count - 1) % count;
        }

        if (action == _first)
        {
            return 0;
        }

        return action == _last ? count - 1 : null;
    }
}
