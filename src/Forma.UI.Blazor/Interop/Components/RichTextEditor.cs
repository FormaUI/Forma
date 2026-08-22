namespace Forma.UI.Blazor.Interop.Components;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Rich text contenteditable surface interop and formatting operations.</summary>
internal sealed class RichTextEditor(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.UI/scripts/components/RichTextEditor.js")
{
    /// <summary>Attaches the rich text editing surface to the contenteditable target.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, DotNetObjectReference<TOwner> owner, int debounceDelay = 400)
        where TOwner : class
        => AttachCoreAsync(target, new { owner, debounceDelay });

    /// <summary>Sets the HTML content of the editor.</summary>
    public ValueTask<bool> SetHtmlAsync(string html)
        => InvokeAsync("setHtml", html);

    /// <summary>Gets the current HTML content of the editor.</summary>
    public ValueTask<string?> GetHtmlAsync()
        => InvokeAsync<string>("getHtml");

    /// <summary>Executes a rich text formatting command on the active selection.</summary>
    public ValueTask<RichTextCommandResult?> ExecAsync(string command, object? value = null)
        => InvokeAsync<RichTextCommandResult>("exec", command, value);
}

/// <summary>The result of executing a rich text command.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Constructed by JSON deserialization from JS interop.")]
public sealed record RichTextCommandResult(string Html, RichTextSelectionDto? Selection);

/// <summary>A selection snapshot reported by the rich text editor.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Constructed by JSON deserialization from JS interop.")]
public sealed record RichTextSelectionDto(int Start, int End, bool IsCollapsed, string? BlockType, IReadOnlyList<string> ActiveStyles);
