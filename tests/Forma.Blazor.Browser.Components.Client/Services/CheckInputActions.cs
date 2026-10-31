namespace Forma.Blazor.Browser.Components.Client.Services;

using Forma.Framework.Input;

/// <summary>The host's handler for its own shortcuts, holding the action it last performed.</summary>
public sealed class CheckInputActions : IInputActionHandler
{
    /// <summary>The action last performed.</summary>
    public string? LastAction { get; private set; }

    /// <summary>Raised once an action is performed.</summary>
    public event Action? Performed;

    /// <inheritdoc />
    public Task HandleAsync(string action, InputInfo input)
    {
        LastAction = action;
        Performed?.Invoke();

        return Task.CompletedTask;
    }
}
