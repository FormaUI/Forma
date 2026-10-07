namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Css;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Input.Keyboard;

using Microsoft.AspNetCore.Components;

/// <summary>A field that edits a number, typed as the formatting culture writes it and stepped with the vertical arrows.</summary>
/// <typeparam name="TValue">The number the field binds: an <see cref="int"/>, a <see cref="long"/>, a <see cref="short"/>, a <see cref="float"/>, a <see cref="double"/> or a <see cref="decimal"/>, or a nullable one.</typeparam>
public sealed partial class FormaNumberInput<TValue> : FormaInputBase<TValue>
{
    private const string IncreaseAction = "number-input.increase";
    private const string DecreaseAction = "number-input.decrease";

    // The vertical arrows step the number through routes on the field itself, which a host can remap.
    private static readonly InputProfile StepProfile = new()
    {
        InputRoutes =
        [
            new InputRoute(IncreaseAction, new KeyBinding("ArrowUp")),
            new InputRoute(DecreaseAction, new KeyBinding("ArrowDown")),
        ],
    };

    /// <summary>The floating label of the field the input sits in.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The placeholder text shown when the input is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Whether a number that can be empty is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the input is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the input is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The smallest number the field accepts.</summary>
    [Parameter] public decimal? Min { get; set; }

    /// <summary>The largest number the field accepts.</summary>
    [Parameter] public decimal? Max { get; set; }

    /// <summary>How far the vertical arrows move the number.</summary>
    [Parameter] public decimal Step { get; set; } = 1;

    /// <summary>How wide the input stands, where null fills the space it is given.</summary>
    [Parameter] public CssLength? Width { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private static readonly Type NumberType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    private static readonly bool IsNullable = Nullable.GetUnderlyingType(typeof(TValue)) is not null;

    private static readonly bool IsWhole = NumberType == typeof(int) || NumberType == typeof(long) || NumberType == typeof(short);

    private static readonly bool IsBinaryFraction = NumberType == typeof(double) || NumberType == typeof(float);

    // The numbers a whole type holds, which a step stops at; a fractional type holds more than any decimal can.
    private static readonly (decimal Lowest, decimal Highest) TypeRange =
        NumberType == typeof(int) ? (int.MinValue, int.MaxValue)
        : NumberType == typeof(long) ? (long.MinValue, long.MaxValue)
        : NumberType == typeof(short) ? (short.MinValue, short.MaxValue)
        : (decimal.MinValue, decimal.MaxValue);

    private InputRegistration _inputRegistration = default!;

    // Text the field could not read stays its value, so a later number the field draws replaces it even where that number is the one it held.
    private string? _unreadText;
    private TValue? _valueWhenUnread;

    private bool HasLabel
        => !string.IsNullOrWhiteSpace(Label);

    /// <inheritdoc />
    protected override bool HasFieldContainer
        => HasLabel;

    // A number that cannot be empty is required whatever the host says, since clearing it reports the field required.
    private bool IsRequired
        => Required || !IsNullable;

    // The floating label takes the placeholder's place over a labeled input.
    private string? EffectivePlaceholder
        => HasLabel ? " " : Placeholder;

    // A keyboard of digits where the number can be neither negative nor, for a whole number, fractional; the full keyboard otherwise, since a
    // phone's keypad of digits offers no minus sign.
    private string InputModeValue
        => Min is not { } min || min < 0 ? "text" : IsWhole ? "numeric" : "decimal";

    private string? ValueNow
        => CurrentValue is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private string? ValueText
        => CurrentValue is not null ? CurrentValueAsString : null;

    private string FieldName
        => DisplayName ?? (HasLabel ? Label! : FieldIdentifier.FieldName);

    // A labeled input's width sizes the field wrapper, which is what occupies the row.
    private string? FieldStyle
        => HasLabel && Width is { } width ? $"inline-size: {width}" : null;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!IsWhole && !IsBinaryFraction && NumberType != typeof(decimal))
        {
            throw new InvalidOperationException($"{nameof(FormaNumberInput<TValue>)} binds an int, a long, a short, a float, a double or a decimal, or a nullable one, not a {typeof(TValue).Name}.");
        }

        _inputRegistration = new InputRegistration(InputRouter, HandleActionAsync);
        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Step);

        if (IsWhole && decimal.Truncate(Step) != Step)
        {
            throw new ArgumentException($"{nameof(Step)} ({Step}) must be a whole number to step a {NumberType.Name}.", nameof(Step));
        }

        if (Min.HasValue && Max.HasValue && Min.Value > Max.Value)
        {
            throw new ArgumentException($"{nameof(Min)} ({Min}) must not be greater than {nameof(Max)} ({Max}).");
        }

        if (_unreadText is not null && !EqualityComparer<TValue>.Default.Equals(Value, _valueWhenUnread))
        {
            _unreadText = null;
        }
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
        => _inputRegistration.SyncAsync(RootElement, StepProfile);

    private void HandleChange(ChangeEventArgs args)
    {
        _unreadText = null;
        CurrentValueAsString = args.Value?.ToString();
    }

    // A step starts from the number shown, or from the limit it moves away from, or from zero, and stops at the limits.
    private Task HandleActionAsync(string action, InputInfo input)
    {
        var direction = action switch
        {
            IncreaseAction => 1,
            DecreaseAction => -1,
            _ => 0,
        };

        if (direction == 0 || Disabled || ReadOnly)
        {
            return Task.CompletedTask;
        }

        decimal next;

        try
        {
            next = CurrentValue is { } value
                ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) + (direction * Step)
                : (direction > 0 ? Min : Max) ?? 0;
        }
        catch (OverflowException)
        {
            return Task.CompletedTask;
        }

        var lowest = Math.Max(Min ?? TypeRange.Lowest, TypeRange.Lowest);
        var highest = Math.Min(Max ?? TypeRange.Highest, TypeRange.Highest);

        if (lowest > highest)
        {
            return Task.CompletedTask;
        }

        // Dividing by one written to the most decimal places drops the trailing zeros a sum keeps, so 12.5 and a half step shows 13.
        next = Math.Clamp(next, lowest, highest) / 1.0000000000000000000000000000m;
        _unreadText = null;
        CurrentValueAsString = next.ToString(CultureInfo.CurrentCulture);

        // A routed key is no event of the field's own, so the field draws the number it stepped to itself.
        StateHasChanged();

        return Task.CompletedTask;
    }

    // A sign, digits and, for a fractional number, the culture's decimal separator, with the whole part grouped as the culture groups it or
    // not grouped at all.
    private static bool TryParseNumber(string text, out TValue result)
    {
        const NumberStyles Whole = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign;
        const NumberStyles Fractional = Whole | NumberStyles.AllowDecimalPoint;
        var culture = CultureInfo.CurrentCulture;
        object? number = null;
        result = default!;

        if (Ungroup(text, culture.NumberFormat) is not { } ungrouped)
        {
            return false;
        }

        text = ungrouped;

        if (NumberType == typeof(int) && int.TryParse(text, Whole, culture, out var whole))
        {
            number = whole;
        }
        else if (NumberType == typeof(long) && long.TryParse(text, Whole, culture, out var wide))
        {
            number = wide;
        }
        else if (NumberType == typeof(short) && short.TryParse(text, Whole, culture, out var narrow))
        {
            number = narrow;
        }
        else if (NumberType == typeof(decimal) && decimal.TryParse(text, Fractional, culture, out var exact))
        {
            number = exact;
        }
        else if (NumberType == typeof(double) && double.TryParse(text, Fractional, culture, out var binary) && double.IsFinite(binary))
        {
            number = binary;
        }
        else if (NumberType == typeof(float) && float.TryParse(text, Fractional, culture, out var single) && float.IsFinite(single))
        {
            number = single;
        }

        result = number is null ? default! : (TValue)number;

        return number is not null;
    }

    // The text without its group separators, or null where they do not group the whole part as the culture does: every group but the
    // first holds exactly the culture's count of digits for its place, the first holds no more than that, and no separator falls past the
    // decimal one. So in English 1,234 is a thousand and more, while 1,5 is refused rather than taken for fifteen.
    private static string? Ungroup(string text, NumberFormatInfo format)
    {
        var trimmed = text.Trim();
        var decimalAt = trimmed.IndexOf(format.NumberDecimalSeparator, StringComparison.Ordinal);
        var whole = decimalAt < 0 ? trimmed : trimmed[..decimalAt];
        var fraction = decimalAt < 0 ? string.Empty : trimmed[decimalAt..];
        var separators = GetGroupSeparators(format);

        if (separators.Any(separator => fraction.Contains(separator, StringComparison.Ordinal)))
        {
            return null;
        }

        var groups = whole.Split(separators, StringSplitOptions.None);

        if (groups.Length == 1)
        {
            return trimmed;
        }

        var sizes = format.NumberGroupSizes;

        // Read from the decimal separator leftward, each place taking its size from the culture and the last size repeating.
        for (var place = 0; place < groups.Length; place++)
        {
            var group = groups[^(place + 1)];
            var size = sizes.Length == 0 ? 0 : sizes[Math.Min(place, sizes.Length - 1)];
            var isFirst = place == groups.Length - 1;
            var digits = isFirst ? group.Count(char.IsAsciiDigit) : group.Length;

            if (size == 0
                || (!isFirst && (group.Length != size || !group.All(char.IsAsciiDigit)))
                || (isFirst && (digits == 0 || digits > size)))
            {
                return null;
            }
        }

        return string.Concat(groups) + fraction;
    }

    // The culture's group separator, and where it is a space of any kind, every space a reader may type for it.
    private static string[] GetGroupSeparators(NumberFormatInfo format)
        => format.NumberGroupSeparator.All(char.IsWhiteSpace)
            ? [format.NumberGroupSeparator, " ", " ", " "]
            : [format.NumberGroupSeparator];

    private static string? FormatLimit(decimal? limit)
        => limit?.ToString(CultureInfo.InvariantCulture);

    // Where the number stands against a limit, compared in the number's own kind, so a decimal is never rounded through a double.
    private static int CompareToLimit(TValue value, decimal limit)
        => IsBinaryFraction
            ? Convert.ToDouble(value, CultureInfo.InvariantCulture).CompareTo((double)limit)
            : Convert.ToDecimal(value, CultureInfo.InvariantCulture).CompareTo(limit);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input", HasLabel)
            .Add("forma-text-box", !HasLabel)
            .Add("forma-number-input");

    /// <inheritdoc />
    protected override void BuildRootStyle(CssStyleBuilder builder)
    {
        if (!HasLabel)
        {
            builder.Add("inline-size", Width);
        }
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value)
        => BindConverter.FormatValue(value, CultureInfo.CurrentCulture)?.ToString();

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        validationErrorMessage = Read(value, out result);

        if (validationErrorMessage is null)
        {
            return true;
        }

        _unreadText = value ?? string.Empty;
        _valueWhenUnread = Value;

        return false;
    }

    // The number the text writes, or why the field cannot take it.
    private string? Read(string? value, out TValue result)
    {
        result = default!;

        if (string.IsNullOrWhiteSpace(value))
        {
            return IsNullable ? null : L["The {Field} field is required.", FieldName];
        }

        if (!TryParseNumber(value, out var parsed))
        {
            return L["The {Field} field must be a number.", FieldName];
        }

        if (Min is { } min && CompareToLimit(parsed, min) < 0)
        {
            return L["The {Field} field must be at least {Minimum}.", FieldName, min.ToString(CultureInfo.CurrentCulture)];
        }

        if (Max is { } max && CompareToLimit(parsed, max) > 0)
        {
            return L["The {Field} field must be at most {Maximum}.", FieldName, max.ToString(CultureInfo.CurrentCulture)];
        }

        result = parsed;

        return null;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _inputRegistration.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
