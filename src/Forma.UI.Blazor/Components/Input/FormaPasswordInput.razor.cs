namespace Forma.UI.Blazor.Components.Input;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

/// <summary>A password field with a toggle that reveals the typed value.</summary>
public sealed partial class FormaPasswordInput : InputBase<string?>, IAsyncDisposable
{
    private const string ModulePath = "./_content/Forma.UI/Components/Input/FormaInput.razor.js";

    // The lengths the built-in rating scores a point at; the longer one is also what the advice asks for.
    private const int ScoredLength = 8;
    private const int WellScoredLength = 12;

    private readonly string _inputId = $"fld-{Guid.NewGuid():N}";
    private ElementReference _inputRef;
    private JsModule _module = default!;
    private ValidationMessageStore _messages = default!;
    private IReadOnlyList<string> _advice = [];
    private bool _hasForbiddenTerm;
    private bool _isRevealed;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IStringLocale<FormaPasswordInput> L { get; set; } = default!;

    /// <summary>The floating label text.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Marks the field as required, showing a visual indicator and setting aria-required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Shows the button that reveals the typed value.</summary>
    [Parameter] public bool ShowToggle { get; set; } = true;

    /// <summary>The autocomplete token passed to the input, such as "current-password" or "new-password".</summary>
    [Parameter] public string AutoComplete { get; set; } = "current-password";

    /// <summary>Shows a strength meter under the field once a value is typed.</summary>
    [Parameter] public bool ShowStrengthMeter { get; set; }

    /// <summary>Rates a typed value, replacing the built-in judgement with the host's own password policy.</summary>
    [Parameter] public Func<string, PasswordStrength>? StrengthEvaluator { get; set; }

    /// <summary>Lists under the field what the typed value still needs to earn the top rating.</summary>
    [Parameter] public bool ShowStrengthAdvice { get; set; }

    /// <summary>Names what a typed value still needs, replacing the built-in advice with the host's own.</summary>
    [Parameter] public Func<string, IEnumerable<string>>? StrengthAdvisor { get; set; }

    /// <summary>Values the password must not draw on, such as the email address or phone number entered elsewhere on the form.</summary>
    [Parameter] public IEnumerable<string?>? ForbiddenTerms { get; set; }

    // Shorter runs than this match too much ordinary language to be worth forbidding.
    private const int MinimumFragmentLength = 4;

    // One segment per rating, so the meter cannot fall out of step with the scale.
    private static readonly int SegmentCount = Enum.GetValues<PasswordStrength>().Length;

    private PasswordStrength? Strength
        => !ShowStrengthMeter || string.IsNullOrEmpty(CurrentValue) ? null
            : _hasForbiddenTerm ? PasswordStrength.Weak
            : (StrengthEvaluator ?? Rate)(CurrentValue);

    // Length first, then how many character classes are drawn on; a host with a real policy
    // supplies its own evaluator rather than bending this one.
    private static PasswordStrength Rate(string value)
    {
        var score = 0;
        var effectiveLength = RepeatingUnitLength(value);

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
    private static int RepeatingUnitLength(string value)
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
        => $"{_inputId}-advice";

    // An explicit attribute overwrites one of the same name from AdditionalAttributes, so a
    // caller's own description has to be carried through rather than dropped.
    private string? DescribedBy
    {
        get
        {
            var caller = AdditionalAttributes?.GetValueOrDefault("aria-describedby")?.ToString();
            var own = _advice.Count > 0 ? AdviceId : null;

            return string.IsNullOrWhiteSpace(caller) ? own
                : string.IsNullOrEmpty(own) ? caller
                : $"{caller} {own}";
        }
    }

    // Every criterion the built-in rating still withholds, so the list empties as the value earns them.
    private IEnumerable<string> Advise(string value)
    {
        var effectiveLength = RepeatingUnitLength(value);

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

    private static string StrengthModifier(PasswordStrength strength)
        => strength switch
        {
            PasswordStrength.Fair => "strength-fair",
            PasswordStrength.Good => "strength-good",
            PasswordStrength.Strong => "strength-strong",
            PasswordStrength.VeryStrong => "strength-very-strong",
            _ => "strength-weak",
        };

    // Each rating is a whole phrase rather than an interpolated word, so a translator sees
    // the sentence it has to make read naturally.
    private string StrengthText(PasswordStrength strength)
        => strength switch
        {
            PasswordStrength.Fair => L["Password strength: Fair"],
            PasswordStrength.Good => L["Password strength: Good"],
            PasswordStrength.Strong => L["Password strength: Strong"],
            PasswordStrength.VeryStrong => L["Password strength: Very strong"],
            _ => L["Password strength: Weak"],
        };

    protected override void OnInitialized()
    {
        _module = new JsModule(JS, ModulePath);
        _messages = new ValidationMessageStore(EditContext);
        EditContext.OnValidationRequested += HandleValidationRequested;
    }

    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        Reassess();
    }

    private void HandleInput(ChangeEventArgs e)
    {
        CurrentValueAsString = e.Value?.ToString();
        Reassess();
    }

    // Advice reads the forbidden-term outcome, so the value has to be validated first.
    private void Reassess()
    {
        ValidateForbiddenTerms();

        _advice = !ShowStrengthAdvice || string.IsNullOrEmpty(CurrentValue) || _hasForbiddenTerm
            ? []
            : [.. (StrengthAdvisor ?? Advise)(CurrentValue)];
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
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
        if (ForbiddenTerms is null || string.IsNullOrEmpty(CurrentValue))
        {
            return false;
        }

        return ForbiddenTerms
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .SelectMany(term => Fragments(term!))
            .Any(fragment => CurrentValue.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    // A term is matched by its parts as well as whole, so a password may not reuse the local
    // part of an email or any one group of a phone number.
    private static IEnumerable<string> Fragments(string term)
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

    // Focus stays on the toggle so it can be pressed again to hide, which returning focus
    // to the input would prevent for a keyboard user.
    private void ToggleReveal()
        => _isRevealed = !_isRevealed;

    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await _module.TryInvokeVoidAsync("initAutofillDetection", _inputRef);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            EditContext.OnValidationRequested -= HandleValidationRequested;
        }

        base.Dispose(disposing);
    }

    public async ValueTask DisposeAsync()
        => await _module.DisposeAsync();
}
