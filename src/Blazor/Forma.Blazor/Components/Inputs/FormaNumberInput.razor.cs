namespace Forma.Blazor.Components.Inputs;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Browser.Interop.Text;
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

    // The left-to-right, right-to-left and Arabic letter marks.
    private static readonly SearchValues<char> DirectionalMarks = SearchValues.Create("\u200E\u200F\u061C");

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

    /// <summary>The most decimal places a number the field accepts carries.</summary>
    [Parameter] public int? MaxDecimalPlaces { get; set; }

    /// <summary>The fewest decimal places a number is shown with.</summary>
    [Parameter] public int MinDecimalPlaces { get; set; }

    /// <summary>The text standing beside the number, such as a currency symbol or a unit.</summary>
    [Parameter] public NumberAdornment? Adornment { get; set; }

    /// <summary>How wide the input stands, where null fills the space it is given.</summary>
    [Parameter] public CssLength? Width { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private static readonly Type NumberType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    private static readonly bool IsNullable = Nullable.GetUnderlyingType(typeof(TValue)) is not null;

    private static readonly bool IsWhole = NumberType == typeof(int) || NumberType == typeof(long) || NumberType == typeof(short);

    private static readonly bool IsBinaryFraction = NumberType == typeof(double) || NumberType == typeof(float);

    // The numbers a whole type holds, which a step stops at; a fractional type holds more than any decimal can.
    private static readonly (decimal LowestValue, decimal HighestValue) TypeRange =
        NumberType == typeof(int) ? (int.MinValue, int.MaxValue)
        : NumberType == typeof(long) ? (long.MinValue, long.MaxValue)
        : NumberType == typeof(short) ? (short.MinValue, short.MaxValue)
        : (decimal.MinValue, decimal.MaxValue);

    // Each edit drafts the number as it is typed, and the field's commit reads it.
    private static readonly TextEditOptions EditOptions = new() { SendsOnInput = true };

    private InputRegistration _inputRegistration = default!;
    private DomTextEdits _edits = default!;

    // The edit in progress and the value it began over, which a different value bound to the field replaces.
    private readonly TextDraft _draft = new();
    private TValue? _valueWhenDrafted;

    private bool HasLabel
        => !string.IsNullOrWhiteSpace(Label);

    // The text the field shows: the edit in progress, or the number bound to it.
    private string? DisplayText
        => _draft.Text ?? CurrentValueAsString;

    // The value the input renders, which a render never writes back over the reader's typing.
    private string? ShownText
        => _edits.GetShownText(DisplayText);

    /// <inheritdoc />
    protected override bool HasFieldContainer
        => HasLabel || HasDescription;

    // A number that cannot be empty is required whatever the host says, since clearing it reports the field required.
    private bool IsRequired
        => Required || !IsNullable;

    // The floating label takes the placeholder's place over a labeled input.
    private string? EffectivePlaceholder
        => HasLabel ? " " : Placeholder;

    // A keyboard of digits where the number can be neither negative nor, for a whole number or one taking no decimal places, fractional; the
    // full keyboard otherwise, since a phone's keypad of digits offers no minus sign.
    private string InputModeValue
        => Min is not { } min || min < 0 ? "text" : IsWhole || MaxDecimalPlaces == 0 ? "numeric" : "decimal";

    private string? ValueNow
        => CurrentValue is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private string? ValueText
        => CurrentValue is not null ? CurrentValueAsString : null;

    private string FieldName
        => DisplayName ?? (HasLabel ? Label! : FieldIdentifier.FieldName);

    // A label floating where the adornment stands would cover it, so a field with an adornment pins its label above the number.
    private FieldLabelMode LabelMode
        => Adornment is null ? FieldLabelMode.Floating : FieldLabelMode.Pinned;

    private string AdornmentDescriptionId
        => $"{ElementId}-adornment";

    private string? AdornmentClass
        => Adornment is { } adornment
            ? new CssClassBuilder()
                .Add("forma-number-input-adornment")
                .Add("forma-number-input-adornment-start", adornment.Side is AdornmentSide.Start)
                .Add("forma-number-input-adornment-end", adornment.Side is AdornmentSide.End)
                .ToStringOrNull()
            : null;

    // The number reads left to right whatever the culture, so the side of the input that makes room for the adornment is its own left
    // where the adornment stands at the start of a left-to-right culture or the end of a right-to-left one, and its own right otherwise.
    private string? AdornmentRoomClass
        => Adornment is { } adornment
            ? (adornment.Side is AdornmentSide.Start) != Context.CultureState.IsRightToLeft
                ? "forma-number-input-room-start"
                : "forma-number-input-room-end"
            : null;

    // The number written to at least the places the field shows it with, and to every place it holds beyond them.
    private string? DisplayFormat
        => MinDecimalPlaces > 0 ? $"0.{new string('0', MinDecimalPlaces)}{new string('#', 28 - MinDecimalPlaces)}" : null;

    // The places the number takes, as the browser's own input steps through them: whole ones, the last decimal place the field takes,
    // or any for a fraction of any places.
    private string PostedStep
        => IsWhole || MaxDecimalPlaces == 0 ? "1"
            : MaxDecimalPlaces is { } places ? $"0.{new string('0', places - 1)}1"
            : "any";

    // A labeled input's width sizes the field wrapper, which is what occupies the row.
    private string? FieldStyle
        => HasLabel && Width is { } width ? $"inline-size: {width}" : null;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _edits = new DomTextEdits(Context.DomEngine, DisplayText, HandleTextChangedAsync);
        JoinForm(_edits.SettleAsync);
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

        ArgumentOutOfRangeException.ThrowIfNegative(MinDecimalPlaces);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(MinDecimalPlaces, 28);

        if (MaxDecimalPlaces is { } maxDecimalPlaces)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maxDecimalPlaces, nameof(MaxDecimalPlaces));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(MinDecimalPlaces, maxDecimalPlaces);
        }

        if (IsWhole && MinDecimalPlaces > 0)
        {
            throw new ArgumentException($"{nameof(MinDecimalPlaces)} ({MinDecimalPlaces}) must be zero for a {NumberType.Name}, which holds no decimal places.", nameof(MinDecimalPlaces));
        }

        if (Adornment is { } adornment)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(adornment.Text, nameof(Adornment));
        }

        if (_draft.IsInProgress && !EqualityComparer<TValue>.Default.Equals(Value, _valueWhenDrafted))
        {
            _draft.Clear();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _edits.SyncAsync(RootElement, DisplayText, EditOptions);

        await _inputRegistration.SyncAsync(RootElement, StepProfile);
    }

    private Task HandleTextChangedAsync(TextChange change)
    {
        ReportTextFault(change.IsFaulted, L["Your text didn't arrive in full. Edit it to try again."]);

        if (change.IsCommit)
        {
            Commit(change.Text);
        }
        else
        {
            Draft(change.Text);
        }

        StateHasChanged();

        return Task.CompletedTask;
    }

    // Reads the number the field committed.
    private void Commit(string text)
    {
        Draft(text);
        CurrentValueAsString = text;

        // Empty text clears a number that can be empty without being read, which ends the edit as a read does.
        if (IsNullable && string.IsNullOrEmpty(text))
        {
            _draft.Clear();
        }
    }

    // Takes the text the reader typed into the edit in progress, which begins over the value bound to the field.
    private void Draft(string? text)
    {
        if (!_draft.IsInProgress)
        {
            _valueWhenDrafted = Value;
        }

        _draft.Edit(text, Context.CultureState.Culture);
    }

    // A step starts from the number shown, an edit in progress's where its text reads as one in the culture it was typed in, or from the
    // limit it moves away from, or from zero, and stops at the limits.
    private async Task HandleActionAsync(string action, InputInfo input)
    {
        var direction = action switch
        {
            IncreaseAction => 1,
            DecreaseAction => -1,
            _ => 0,
        };

        // The step reads the number the reader typed, every edit sent ahead of the key applied.
        if (direction == 0 || Disabled || ReadOnly || !await _edits.SettleAsync())
        {
            return;
        }

        var start = _draft.IsInProgress && TryParseNumber(_draft.Text, _draft.Culture, out var draftedNumber) ? draftedNumber : CurrentValue;
        decimal nextValue;

        try
        {
            nextValue = start is { } value
                ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) + (direction * Step)
                : (direction > 0 ? Min : Max) ?? 0;
        }
        catch (OverflowException)
        {
            return;
        }

        var lowestValue = Math.Max(Min ?? TypeRange.LowestValue, TypeRange.LowestValue);
        var highestValue = Math.Min(Max ?? TypeRange.HighestValue, TypeRange.HighestValue);

        if (lowestValue > highestValue)
        {
            return;
        }

        // Dividing by one written to the most decimal places drops the trailing zeros a sum keeps, so 12.5 and a half step shows 13.
        nextValue = Math.Clamp(nextValue, lowestValue, highestValue) / 1.0000000000000000000000000000m;
        _draft.Clear();
        CurrentValueAsString = nextValue.ToString(Context.CultureState.Culture);

        // A routed key is no event of the field's own, so the field renders the number it stepped to itself, and its edits write it in.
        StateHasChanged();
    }

    // A sign, digits and, for a fractional number, the culture's decimal separator, with the whole part grouped as the culture groups it or
    // not grouped at all.
    private static bool TryParseNumber(string text, CultureInfo culture, out TValue result)
    {
        const NumberStyles WholeNumberStyles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign;
        const NumberStyles FractionalNumberStyles = WholeNumberStyles | NumberStyles.AllowDecimalPoint;
        var format = GetReadingFormat(culture.NumberFormat);
        object? number = null;
        result = default!;

        if (Ungroup(RemoveDirectionalMarks(text), format) is not { } ungroupedText)
        {
            return false;
        }

        text = ungroupedText;

        if (NumberType == typeof(int) && int.TryParse(text, WholeNumberStyles, format, out var parsedInt))
        {
            number = parsedInt;
        }
        else if (NumberType == typeof(long) && long.TryParse(text, WholeNumberStyles, format, out var parsedLong))
        {
            number = parsedLong;
        }
        else if (NumberType == typeof(short) && short.TryParse(text, WholeNumberStyles, format, out var parsedShort))
        {
            number = parsedShort;
        }
        else if (NumberType == typeof(decimal) && decimal.TryParse(text, FractionalNumberStyles, format, out var parsedDecimal))
        {
            number = parsedDecimal;
        }
        else if (NumberType == typeof(double) && double.TryParse(text, FractionalNumberStyles, format, out var parsedDouble) && double.IsFinite(parsedDouble))
        {
            number = parsedDouble;
        }
        else if (NumberType == typeof(float) && float.TryParse(text, FractionalNumberStyles, format, out var parsedFloat) && float.IsFinite(parsedFloat))
        {
            number = parsedFloat;
        }

        result = number is null ? default! : (TValue)number;

        return number is not null;
    }

    // The culture's number format with its signs as a reader types them. A right-to-left culture writes a directional mark beside its
    // signs, which no keyboard types with them, so in Hebrew -5 is read as the culture's own sign followed by five.
    private static NumberFormatInfo GetReadingFormat(NumberFormatInfo format)
    {
        var negativeSign = RemoveDirectionalMarks(format.NegativeSign);
        var positiveSign = RemoveDirectionalMarks(format.PositiveSign);

        if (negativeSign == format.NegativeSign && positiveSign == format.PositiveSign)
        {
            return format;
        }

        var reading = (NumberFormatInfo)format.Clone();
        reading.NegativeSign = negativeSign;
        reading.PositiveSign = positiveSign;

        return reading;
    }

    private static string RemoveDirectionalMarks(string text)
        => text.AsSpan().ContainsAny(DirectionalMarks)
            ? string.Concat(text.Where(character => !DirectionalMarks.Contains(character)))
            : text;

    // The text without its group separators, or null where they do not group the whole part as the culture does: every group but the
    // first holds exactly the culture's count of digits for its place, the first holds no more than that, and no separator falls past the
    // decimal one. So in English 1,234 is a thousand and more, while 1,5 is refused rather than taken for fifteen.
    private static string? Ungroup(string text, NumberFormatInfo format)
    {
        var trimmedText = text.Trim();
        var decimalAt = trimmedText.IndexOf(format.NumberDecimalSeparator, StringComparison.Ordinal);
        var wholePart = decimalAt < 0 ? trimmedText : trimmedText[..decimalAt];
        var fraction = decimalAt < 0 ? string.Empty : trimmedText[decimalAt..];
        var separators = GetGroupSeparators(format);

        if (separators.Any(separator => fraction.Contains(separator, StringComparison.Ordinal)))
        {
            return null;
        }

        var groups = wholePart.Split(separators, StringSplitOptions.None);

        if (groups.Length == 1)
        {
            return trimmedText;
        }

        var sizes = format.NumberGroupSizes;

        // Read from the decimal separator leftward, each place taking its size from the culture and the last size repeating.
        for (var place = 0; place < groups.Length; place++)
        {
            var group = groups[^(place + 1)];
            var size = sizes.Length == 0 ? 0 : sizes[Math.Min(place, sizes.Length - 1)];
            var isFirst = place == groups.Length - 1;
            var digitCount = isFirst ? group.Count(char.IsAsciiDigit) : group.Length;

            if (size == 0
                || (!isFirst && (group.Length != size || !group.All(char.IsAsciiDigit)))
                || (isFirst && (digitCount == 0 || digitCount > size)))
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

    // The decimal places the text writes, past the culture's decimal separator, with trailing zeros left out.
    private static int CountDecimalPlaces(string text, CultureInfo culture)
    {
        var trimmedText = text.Trim();
        var decimalAt = trimmedText.IndexOf(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);

        return decimalAt < 0
            ? 0
            : trimmedText[(decimalAt + culture.NumberFormat.NumberDecimalSeparator.Length)..].TrimEnd('0').Count(char.IsAsciiDigit);
    }

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
            .Add("forma-number-input")
            .Add(AdornmentRoomClass);

    /// <inheritdoc />
    protected override void BuildRootStyle(CssStyleBuilder builder)
    {
        if (!HasLabel)
        {
            builder.Add("inline-size", Width);
        }

        if (Adornment is { } adornment)
        {
            builder.Add("--forma-number-input-adornment-size", CssLength.FromCh(adornment.Text.Length));
        }
    }

    /// <inheritdoc />
    protected override void BuildDescribedBy(ICollection<string> ids)
    {
        if (Adornment is { Description: not null })
        {
            ids.Add(AdornmentDescriptionId);
        }
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value)
        => value is IFormattable number ? number.ToString(DisplayFormat, Context.CultureState.Culture) : null;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        validationErrorMessage = Read(value, out result);

        if (validationErrorMessage is null)
        {
            _draft.Clear();

            return true;
        }

        Draft(value);

        return false;
    }

    // The number the text writes, read in the culture of the edit in progress, or why the field cannot take it.
    private string? Read(string? value, out TValue result)
    {
        result = default!;

        if (string.IsNullOrWhiteSpace(value))
        {
            return IsNullable ? null : L["The {Field} field is required.", FieldName];
        }

        var culture = _draft.Culture ?? Context.CultureState.Culture;

        if (!TryParseNumber(value, culture, out var parsedNumber))
        {
            return L["The {Field} field must be a number.", FieldName];
        }

        if (MaxDecimalPlaces is { } places && CountDecimalPlaces(value, culture) > places)
        {
            return places == 0
                ? L["The {Field} field must be a whole number.", FieldName]
                : L["The {Field} field must have no more than {Places} [$ [Places] plural; one: decimal place; other: decimal places; $].", FieldName, places];
        }

        if (Min is { } min && CompareToLimit(parsedNumber, min) < 0)
        {
            return L["The {Field} field must be at least {Minimum}.", FieldName, min.ToString(Context.CultureState.Culture)];
        }

        if (Max is { } max && CompareToLimit(parsedNumber, max) > 0)
        {
            return L["The {Field} field must be at most {Maximum}.", FieldName, max.ToString(Context.CultureState.Culture)];
        }

        result = parsedNumber;

        return null;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _edits.DisposeAsync();

        await _inputRegistration.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
