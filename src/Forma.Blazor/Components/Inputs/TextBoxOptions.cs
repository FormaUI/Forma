namespace Forma.Blazor.Components.Inputs;

/// <summary>What kind of text a box holds and how an agent assists typing it: validation bounds, keyboard, capitalization, spelling, and autofill.</summary>
public sealed record TextBoxOptions
{
    /// <summary>The kind of text the box holds, which decides how a browser validates and assists it.</summary>
    public TextInputType Type { get; init; } = TextInputType.Text;

    /// <summary>The fewest characters the box accepts before it counts as valid.</summary>
    public int? MinLength { get; init; }

    /// <summary>The greatest number of characters the box will accept.</summary>
    public int? MaxLength { get; init; }

    /// <summary>A regular expression the value must match for the box to be valid.</summary>
    public string? Pattern { get; init; }

    /// <summary>The virtual keyboard to ask a touch device for.</summary>
    public TextInputMode? InputMode { get; init; }

    /// <summary>What a touch keyboard's action key does, and how it is labeled.</summary>
    public TextEnterKeyHint? EnterKeyHint { get; init; }

    /// <summary>What a touch keyboard capitalizes automatically as text is typed.</summary>
    public TextAutoCapitalize? AutoCapitalize { get; init; }

    /// <summary>Whether the browser checks the spelling of the text.</summary>
    public bool? SpellCheck { get; init; }

    /// <summary>The autofill hint given to the browser, such as <c>email</c> or <c>new-password</c>.</summary>
    public string? AutoComplete { get; init; }

    /// <summary>Plain text with no bounds, letting the browser assist as it sees fit.</summary>
    public static TextBoxOptions Default { get; } = new();

    /// <summary>An email address: validated as one, with the email keyboard and autofill, uncapitalized and unchecked for spelling.</summary>
    public static TextBoxOptions Email { get; } = new()
    {
        Type = TextInputType.Email,
        InputMode = TextInputMode.Email,
        AutoCapitalize = TextAutoCapitalize.None,
        SpellCheck = false,
        AutoComplete = "email",
    };

    /// <summary>A telephone number: validated as one, with the telephone keypad and autofill.</summary>
    public static TextBoxOptions Phone { get; } = new()
    {
        Type = TextInputType.Tel,
        InputMode = TextInputMode.Tel,
        SpellCheck = false,
        AutoComplete = "tel",
    };

    /// <summary>A web address: validated as one, with the URL keyboard and autofill, uncapitalized and unchecked for spelling.</summary>
    public static TextBoxOptions Url { get; } = new()
    {
        Type = TextInputType.Url,
        InputMode = TextInputMode.Url,
        AutoCapitalize = TextAutoCapitalize.None,
        SpellCheck = false,
        AutoComplete = "url",
    };

    /// <summary>A person's name: each word capitalized, with the name autofill, unchecked for spelling.</summary>
    public static TextBoxOptions Name { get; } = new()
    {
        AutoCapitalize = TextAutoCapitalize.Words,
        SpellCheck = false,
        AutoComplete = "name",
    };

    /// <summary>A one-time code: the numeric keypad, offered the code the platform just received.</summary>
    public static TextBoxOptions OneTimeCode { get; } = new()
    {
        InputMode = TextInputMode.Numeric,
        AutoCapitalize = TextAutoCapitalize.None,
        SpellCheck = false,
        AutoComplete = "one-time-code",
    };
}
