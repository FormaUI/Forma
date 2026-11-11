namespace Forma.Blazor.Components.Interaction;

using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;

/// <summary>The keys a strip of tabs is walked by under one owner: the arrows along and against the reading direction, and Home and End.</summary>
/// <param name="owner">The owner the strip's actions are qualified under.</param>
internal sealed class TabStripNavigation(string owner)
{
    private readonly string _nextRoute = InputRoute.Qualify(owner, "next");
    private readonly string _previousRoute = InputRoute.Qualify(owner, "previous");
    private readonly string _firstRoute = InputRoute.Qualify(owner, "first");
    private readonly string _lastRoute = InputRoute.Qualify(owner, "last");

    /// <summary>The strip's routes for the reading direction.</summary>
    public InputProfile CreateProfile(bool isRightToLeft)
        => new()
        {
            InputRoutes =
            [
                new InputRoute(_nextRoute, new KeyBinding(isRightToLeft ? "ArrowLeft" : "ArrowRight")),
                new InputRoute(_previousRoute, new KeyBinding(isRightToLeft ? "ArrowRight" : "ArrowLeft")),
                new InputRoute(_firstRoute, new KeyBinding("Home")),
                new InputRoute(_lastRoute, new KeyBinding("End")),
            ],
        };

    /// <summary>The index <paramref name="action"/> moves to from <paramref name="currentIndex"/> among <paramref name="count"/> tabs.</summary>
    public int? FindTarget(string action, int currentIndex, int count)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (count == 0)
        {
            return null;
        }

        if (action == _nextRoute)
        {
            return (currentIndex + 1) % count;
        }

        if (action == _previousRoute)
        {
            return (currentIndex + count - 1) % count;
        }

        if (action == _firstRoute)
        {
            return 0;
        }

        return action == _lastRoute ? count - 1 : null;
    }
}
