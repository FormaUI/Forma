namespace Forma.Blazor.Components.Inputs;

/// <summary>How a password's strength is judged and shown: the meter, the advice, and the policy behind them.</summary>
public sealed record PasswordStrengthOptions
{
    /// <summary>Whether the field shows a strength meter under it.</summary>
    public bool ShowMeter { get; init; }

    /// <summary>The host's own rating of a typed value, where null rates it by the built-in policy.</summary>
    public Func<string, PasswordStrength>? Evaluator { get; init; }

    /// <summary>Whether the field lists under it what the typed value still needs to earn the top rating.</summary>
    public bool ShowAdvice { get; init; }

    /// <summary>The host's own advice naming what a typed value still needs, where null gives the built-in advice.</summary>
    public Func<string, IEnumerable<string>>? Advisor { get; init; }

    /// <summary>Terms the password must not draw on, such as the email address or phone number entered elsewhere on the form.</summary>
    public IEnumerable<string?>? ForbiddenTerms { get; init; }

    /// <summary>Options showing neither meter nor advice, judging by the built-in policy.</summary>
    public static PasswordStrengthOptions Default { get; } = new();
}
