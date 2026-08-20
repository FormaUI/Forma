namespace Forma.UI.Blazor.Components.Input;

/// <summary>How resistant a password is judged to be, as shown by <see cref="FormaPasswordBox"/>.</summary>
public enum PasswordStrength
{
    /// <summary>Short, or drawn from a single character class.</summary>
    Weak,

    /// <summary>Long enough, but predictable.</summary>
    Fair,

    /// <summary>Mixes several character classes at a reasonable length.</summary>
    Good,

    /// <summary>Long and mixed enough to resist guessing.</summary>
    Strong,

    /// <summary>Long, mixed, and drawing on every character class including symbols.</summary>
    VeryStrong,
}
