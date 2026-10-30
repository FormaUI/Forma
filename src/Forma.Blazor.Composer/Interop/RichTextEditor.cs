namespace Forma.Blazor.Components.Composer.Interop;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The interop of the rich text editor's contenteditable canvas.</summary>
internal sealed class RichTextEditor(IJSEnvironment js)
    : JSInterop(js, "./_content/Forma.Blazor.Composer/scripts/RichTextEditor.js")
{
    /// <summary>Attaches the rich text canvas to the contenteditable target.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, DotNetObjectReference<TOwner> owner, int debounceDelay = 400)
        where TOwner : class
        => AttachCoreAsync(target, new AttachOptions<TOwner> { Owner = owner, DebounceDelay = debounceDelay });

    /// <summary>Sets the HTML content of the editor.</summary>
    public ValueTask<bool> SetHtmlAsync(string html)
        => InvokeAsync("setHtml", html);

    /// <summary>Gets the current HTML content of the editor.</summary>
    public ValueTask<string?> GetHtmlAsync()
        => InvokeAsync<string>("getHtml");

    /// <summary>Gets the text of the last selection made inside the canvas.</summary>
    public ValueTask<string?> GetSelectionTextAsync()
        => InvokeAsync<string>("getSelectionText");

    /// <summary>Freezes the selection a link dialog will act on, and reports what it covers.</summary>
    public ValueTask<RichTextLinkTargetDto?> BeginLinkDialogAsync(bool isAnchor)
        => InvokeAsync<RichTextLinkTargetDto>("beginLinkDialog", isAnchor ? "a[name]" : "a[href]");

    /// <summary>Executes a rich text formatting command on the active selection.</summary>
    public ValueTask<RichTextCommandResult?> ExecuteCommandAsync(string command, object? value = null)
        => InvokeAsync<RichTextCommandResult>("exec", command, value);

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required DotNetObjectReference<TOwner> Owner { get; init; }

        public required int DebounceDelay { get; init; }
    }
}

/// <summary>The result of executing a rich text command.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record RichTextCommandResult
{
    public string Html { get; init; } = "";

    public RichTextSelectionDto? Selection { get; init; }
}

/// <summary>What a link dialog acts on: the text it covers, the address of the single link it covers, and the number of links.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Constructed by JSON deserialization from JS interop.")]
internal sealed record RichTextLinkTargetDto
{
    public string Text { get; init; } = "";

    public string? Href { get; init; }

    public int Count { get; init; }
}

/// <summary>A selection snapshot reported by the rich text editor.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record RichTextSelectionDto
{
    public int Start { get; init; }

    public int End { get; init; }

    public bool IsCollapsed { get; init; }

    public string? BlockType { get; init; }

    public IReadOnlyList<string> ActiveStyles { get; init; } = [];
}
