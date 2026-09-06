namespace Forma.Framework.Composer.Interop;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Rich text contenteditable canvas interop and formatting operations.</summary>
public sealed class RichTextCanvas(IJSRuntime js)
    : JsInterop(js, "./_content/forma/composer/scripts/RichTextEditor.js")
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
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Constructed by JSON deserialization from JS interop.")]
public sealed record RichTextCommandResult(string Html, RichTextSelectionDto? Selection);

/// <summary>What a link dialog is about to act on: the text covered, and the one address behind it when there is exactly one.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Constructed by JSON deserialization from JS interop.")]
public sealed record RichTextLinkTargetDto(string Text, string? Href, int Count);

/// <summary>A selection snapshot reported by the rich text editor.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Constructed by JSON deserialization from JS interop.")]
public sealed record RichTextSelectionDto(int Start, int End, bool IsCollapsed, string? BlockType, IReadOnlyList<string> ActiveStyles);
