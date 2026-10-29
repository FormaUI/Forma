namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>A password field with a toggle that reveals the typed value, and a strength meter and advice it can show.</summary>
public sealed partial class FormaPasswordBox : FormaInputBase<string?>
{
    // The lengths the built-in rating scores a point at; the longer one is also what the advice asks for.
    private const int ScoredLength = 8;
    private const int WellScoredLength = 12;

    private AutofillTracker _autofill = default!;
    private ValidationMessageStore _messages = default!;
    private IReadOnlyList<string> _advice = [];
    private bool _hasForbiddenTerm;
    private bool _isRevealed;

    /// <summary>The floating label text.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether the field is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the field shows the button that reveals the typed value.</summary>
    [Parameter] public bool ShowToggle { get; set; } = true;

    /// <summary>The autocomplete token passed to the input, such as "current-password" or "new-password".</summary>
    [Parameter] public string AutoComplete { get; set; } = "current-password";

    /// <summary>How the typed value's strength is judged and shown.</summary>
    [Parameter] public PasswordStrengthOptions StrengthOptions { get; set; } = PasswordStrengthOptions.Default;

    // The shortest part of a forbidden term that is matched on its own.
    private const int MinimumFragmentLength = 4;

    // One meter segment per rating.
    private static readonly int SegmentCount = Enum.GetValues<PasswordStrength>().Length;

    private PasswordStrength? Strength
        => !StrengthOptions.ShowMeter || string.IsNullOrEmpty(CurrentValue) ? null
            : _hasForbiddenTerm ? PasswordStrength.Weak
            : (StrengthOptions.Evaluator ?? Rate)(CurrentValue);

    // A point for each scored length and each character class drawn on, less one for low variety.
    private static PasswordStrength Rate(string value)
    {
        var score = 0;
        var effectiveLength = GetRepeatingUnitLength(value);

        if (effectiveLength >= ScoredLength)
        {
            score++;
        }

        if (effectiveLength >= WellScoredLength)
        {
            score++;
        }

        if (HasMixedCase(value))
        {
            score++;
        }

        if (HasDigit(value))
        {
            score++;
        }

        if (HasSymbol(value))
        {
            score++;
        }

        if (HasLowVariety(value))
        {
            score--;
        }

        return score switch
        {
            <= 1 => PasswordStrength.Weak,
            2 => PasswordStrength.Fair,
            3 => PasswordStrength.Good,
            4 => PasswordStrength.Strong,
            _ => PasswordStrength.VeryStrong,
        };
    }

    private static bool HasMixedCase(string value)
        => value.Any(char.IsLower) && value.Any(char.IsUpper);

    private static bool HasDigit(string value)
        => value.Any(char.IsDigit);

    private static bool HasSymbol(string value)
        => value.Any(c => !char.IsLetterOrDigit(c));

    // At least half the characters duplicate one another.
    private static bool HasLowVariety(string value)
        => value.Distinct().Count() * 2 <= value.Length;

    // The shortest block the whole value repeats, so "abcabcabc" counts for its length as three.
    private static int GetRepeatingUnitLength(string value)
    {
        for (var unit = 1; unit < value.Length; unit++)
        {
            if (value.Length % unit != 0)
            {
                continue;
            }

            var repeats = true;

            for (var i = unit; i < value.Length && repeats; i++)
            {
                repeats = value[i] == value[i - unit];
            }

            if (repeats)
            {
                return unit;
            }
        }

        return value.Length;
    }

    private string AdviceId
        => $"{ElementId}-advice";

    // Every criterion the built-in rating still withholds.
    private IEnumerable<string> Advise(string value)
    {
        var effectiveLength = GetRepeatingUnitLength(value);

        if (effectiveLength < value.Length || HasLowVariety(value))
        {
            yield return L["Avoid repeating the same characters."];
        }

        if (effectiveLength < WellScoredLength)
        {
            yield return L["[$ plural; one: Use at least # character.; other: Use at least # characters.; $]", WellScoredLength];
        }

        if (!HasMixedCase(value))
        {
            yield return L["Mix uppercase and lowercase letters."];
        }

        if (!HasDigit(value))
        {
            yield return L["Add a number."];
        }

        if (!HasSymbol(value))
        {
            yield return L["Add a symbol."];
        }
    }

    private static string GetStrengthModifier(PasswordStrength strength)
        => strength switch
        {
            PasswordStrength.Fair => "strength-fair",
            PasswordStrength.Good => "strength-good",
            PasswordStrength.Strong => "strength-strong",
            PasswordStrength.VeryStrong => "strength-very-strong",
            _ => "strength-weak",
        };

    // Each rating is a whole phrase, so localization sees the sentence it has to make read naturally.
    private string GetStrengthText(PasswordStrength strength)
        => strength switch
        {
            PasswordStrength.Fair => L["Password strength: Fair"],
            PasswordStrength.Good => L["Password strength: Good"],
            PasswordStrength.Strong => L["Password strength: Strong"],
            PasswordStrength.VeryStrong => L["Password strength: Very strong"],
            _ => L["Password strength: Weak"],
        };

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _autofill = new AutofillTracker(Context.JS);
        _messages = new ValidationMessageStore(EditContext);
        EditContext.OnValidationRequested += HandleValidationRequested;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentNullException.ThrowIfNull(StrengthOptions);
        Reassess();
    }

    private void HandleInput(string? text)
    {
        CurrentValueAsString = text;
        Reassess();
    }

    // Forbidden terms are validated first, since the advice reads their outcome.
    private void Reassess()
    {
        ValidateForbiddenTerms();

        _advice = !StrengthOptions.ShowAdvice || string.IsNullOrEmpty(CurrentValue) || _hasForbiddenTerm
            ? []
            : [.. (StrengthOptions.Advisor ?? Advise)(CurrentValue)];
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args)
        => ValidateForbiddenTerms();

    private void ValidateForbiddenTerms()
    {
        var forbidden = ContainsForbiddenTerm();

        if (forbidden == _hasForbiddenTerm)
        {
            return;
        }

        _hasForbiddenTerm = forbidden;
        _messages.Clear(FieldIdentifier);

        if (forbidden)
        {
            _messages.Add(FieldIdentifier, L["The password must not contain your other details."]);
        }

        EditContext.NotifyValidationStateChanged();
    }

    private bool ContainsForbiddenTerm()
    {
        if (StrengthOptions.ForbiddenTerms is null || string.IsNullOrEmpty(CurrentValue))
        {
            return false;
        }

        return StrengthOptions.ForbiddenTerms
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .SelectMany(term => GetFragments(term!))
            .Any(fragment => CurrentValue.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    // The term's runs of letters and digits long enough to match, such as an email's local part or one group of a phone number.
    private static IEnumerable<string> GetFragments(string term)
    {
        var start = 0;

        for (var i = 0; i <= term.Length; i++)
        {
            if (i < term.Length && char.IsLetterOrDigit(term[i]))
            {
                continue;
            }

            if (i - start >= MinimumFragmentLength)
            {
                yield return term[start..i];
            }

            start = i + 1;
        }
    }

    // Focus stays on the toggle, so a keyboard user can press it again to hide the value.
    private void ToggleReveal()
        => _isRevealed = !_isRevealed;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await _autofill.AttachAsync(RootElement);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input")
            .Add("forma-password-box");

    /// <inheritdoc />
    protected override void BuildDescribedBy(ICollection<string> ids)
    {
        if (_advice.Count > 0)
        {
            ids.Add(AdviceId);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            EditContext.OnValidationRequested -= HandleValidationRequested;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _autofill.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
