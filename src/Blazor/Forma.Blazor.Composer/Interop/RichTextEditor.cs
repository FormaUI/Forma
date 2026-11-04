namespace Forma.Blazor.Components.Composer.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Forma.Blazor.Components.Composer.Dialogs;
using Forma.Framework.Composer.Editing;
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

    /// <summary>Commits what the canvas holds through its edits at once, cutting its pause short.</summary>
    public ValueTask<bool> CommitAsync()
        => InvokeAsync("commit");

    /// <summary>Freezes the selection a link dialog will act on, and reports what it covers.</summary>
    public async ValueTask<RichTextLinkTargetDto?> BeginLinkDialogAsync(bool isAnchor)
        => await InvokeJsonAsync("beginLinkDialog", isAnchor ? "a[name]" : "a[href]") is { } target
            ? target.Deserialize(RichTextEditorJsonContext.Default.RichTextLinkTargetDto)
            : null;

    /// <summary>Executes a rich text formatting command on the active selection.</summary>
    public ValueTask<RichTextCommandResult?> ExecuteCommandAsync(string command, string? value = null)
        => InvokeAsync<RichTextCommandResult>("exec", command, value);

    /// <summary>Executes a rich text command that takes a number on the active selection.</summary>
    public ValueTask<RichTextCommandResult?> ExecuteCommandAsync(string command, int value)
        => InvokeAsync<RichTextCommandResult>("exec", command, value);

    /// <summary>Executes the command a dialog collected <paramref name="result"/> for on the active selection.</summary>
    public ValueTask<RichTextCommandResult?> ExecuteCommandAsync(string command, ComposerDialogResult result)
        => InvokeAsync<RichTextCommandResult>("exec", command, result);

    /// <summary>Executes the command that puts <paramref name="insertion"/> into the document at the active selection.</summary>
    public ValueTask<RichTextCommandResult?> ExecuteCommandAsync(string command, ComposerInsertion insertion)
        => InvokeAsync<RichTextCommandResult>("exec", command, insertion);

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required DotNetObjectReference<TOwner> Owner { get; init; }

        public required int DebounceDelay { get; init; }
    }
}

/// <summary>The result of executing a rich text command: the selection it leaves, the document it leaves having reached the canvas's edits ahead of it.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record RichTextCommandResult
{
    public RichTextSelectionDto? Selection { get; init; }
}

/// <summary>What a link dialog acts on: the text it covers, the address, name and tab of the single link or anchor it covers, and the number it covers.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Constructed by JSON deserialization from JS interop.")]
internal sealed record RichTextLinkTargetDto
{
    public string Text { get; init; } = "";

    public string? Href { get; init; }

    public string? Name { get; init; }

    public bool OpensInNewTab { get; init; }

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
