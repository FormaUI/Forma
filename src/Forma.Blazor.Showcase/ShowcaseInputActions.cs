namespace Forma.Blazor.Showcase;

using Forma.Framework.Input;

/// <summary>The showcase's own answer to its host routes: it remembers the last action and tells the page showing it.</summary>
public sealed class ShowcaseInputActions : IInputActionHandler
{
    /// <summary>The last host action performed, or <see langword="null"/> before any.</summary>
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
