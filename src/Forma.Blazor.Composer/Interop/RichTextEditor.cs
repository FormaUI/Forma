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
        => AttachCoreAsync(target, new { owner, debounceDelay });

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
}

/// <summary>The result of executing a rich text command.</summary>
public sealed record RichTextCommandResult(string Html, RichTextSelectionDto? Selection);

/// <summary>What a link dialog acts on: the text it covers, the address of the single link it covers, and the number of links.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Constructed by JSON deserialization from JS interop.")]
internal sealed record RichTextLinkTargetDto(string Text, string? Href, int Count);

/// <summary>A selection snapshot reported by the rich text editor.</summary>
public sealed record RichTextSelectionDto(int Start, int End, bool IsCollapsed, string? BlockType, IReadOnlyList<string> ActiveStyles);
