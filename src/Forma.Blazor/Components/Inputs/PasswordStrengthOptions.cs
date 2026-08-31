namespace Forma.Blazor.Components.Inputs;

/// <summary>How a password's strength is judged and shown: the meter, the advice, and the policy behind them.</summary>
public sealed record PasswordStrengthOptions
{
    /// <summary>Shows a strength meter under the field once a value is typed.</summary>
    public bool ShowMeter { get; init; }

    /// <summary>Rates a typed value, replacing the built-in judgement with the host's own password policy.</summary>
    public Func<string, PasswordStrength>? Evaluator { get; init; }

    /// <summary>Lists under the field what the typed value still needs to earn the top rating.</summary>
    public bool ShowAdvice { get; init; }

    /// <summary>Names what a typed value still needs, replacing the built-in advice with the host's own.</summary>
    public Func<string, IEnumerable<string>>? Advisor { get; init; }

    /// <summary>Values the password must not draw on, such as the email address or phone number entered elsewhere on the form.</summary>
    public IEnumerable<string?>? ForbiddenTerms { get; init; }

    /// <summary>Options showing neither meter nor advice, judging by the built-in policy.</summary>
    public static PasswordStrengthOptions Default { get; } = new();
}
