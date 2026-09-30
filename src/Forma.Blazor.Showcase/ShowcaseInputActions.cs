namespace Forma.Blazor.Showcase;

using Forma.Framework.Input;

/// <summary>The Showcase's handler for its host routes, holding the last action performed.</summary>
public sealed class ShowcaseInputActions : IInputActionHandler
{
    /// <summary>The last host action performed.</summary>
    public string? LastAction { get; private set; }

    /// <summary>The input that raised <see cref="LastAction"/>.</summary>
    public InputInfo? LastInput { get; private set; }

    /// <summary>Raised after each host action.</summary>
    public event Action? Changed;

    /// <inheritdoc />
    public Task HandleAsync(string action, InputInfo input)
    {
        LastAction = action;
        LastInput = input;
        Changed?.Invoke();

        return Task.CompletedTask;
    }
}
