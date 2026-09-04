namespace Forma.Blazor.Showcase.Components.Pages.Features;

using System.Globalization;
using System.Text.RegularExpressions;

using Forma.Framework.Composer.Editing;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>The source canvas for the composer demo, numbering its lines and renaming a symbol throughout the document.</summary>
public sealed partial class DemoSourceEditor : ComponentBase, IDisposable
{
    // A pattern built from what the author typed, so a pathological name cannot run away with the request.
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    [CascadingParameter] public IComposerInteraction? Interaction { get; set; }

    /// <summary>The plugin this canvas belongs to, cascaded by the host so each composer drives its own.</summary>
    [CascadingParameter] public SourceEditorPlugin? Plugin { get; set; }

    private ElementReference _oldNameElement;
    private string? _typedContent;
    private string? _committedContent;
    private bool _isRenaming;
    private bool _shouldFocusRename;
    private string _oldName = string.Empty;
    private string _newName = string.Empty;
    private string _announcement = string.Empty;

    // Rendering what the textarea already holds keeps an interleaved render from overwriting uncommitted typing.
    private string Content
        => _typedContent ?? Context?.Document.Content ?? string.Empty;

    private int LineCount
        => Content.Split('\n').Length;

    private bool IsDisabled
        => Context?.IsDisabled ?? false;

    private bool IsReadOnly
        => Context?.IsReadOnly ?? false;

    private bool CanRename
        => !string.IsNullOrWhiteSpace(_oldName) && !string.IsNullOrWhiteSpace(_newName);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Plugin is null)
        {
            throw new InvalidOperationException($"{nameof(DemoSourceEditor)} requires a cascading {nameof(SourceEditorPlugin)}.");
        }

        Plugin.RenameRequested += HandleRenameRequested;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var documentContent = Context?.Document.Content;

        if (documentContent != _committedContent)
        {
            _typedContent = null;
            _committedContent = documentContent;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_shouldFocusRename)
        {
            return;
        }

        _shouldFocusRename = false;
        await _oldNameElement.FocusAsync();
    }

    private void HandleRenameRequested()
    {
        if (IsDisabled || IsReadOnly)
        {
            return;
        }

        _isRenaming = true;
        _shouldFocusRename = true;
        StateHasChanged();
    }

    private void HandleInputAsync(ChangeEventArgs e)
        => _typedContent = e.Value?.ToString() ?? string.Empty;

    private async Task HandleChangeAsync(ChangeEventArgs e)
    {
        _typedContent = e.Value?.ToString() ?? string.Empty;
        await CommitAsync(_typedContent);
    }

    private async Task HandleRenameKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            CancelRename();
        }
        else if (e.Key == "Enter" && CanRename)
        {
            await ApplyRenameAsync();
        }
    }

    private async Task ApplyRenameAsync()
    {
        if (Context is null || Interaction is null || !CanRename)
        {
            return;
        }

        // Whole-word matching is what separates renaming a symbol from replacing a substring.
        var pattern = $@"\b{Regex.Escape(_oldName)}\b";
        var replaced = 0;
        string content;

        try
        {
            content = Regex.Replace(Content, pattern, _ => { replaced++; return _newName; }, RegexOptions.None, MatchTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            _announcement = L["The rename took too long and was abandoned."];
            return;
        }

        _announcement = replaced == 0
            ? string.Format(CultureInfo.CurrentCulture, L["No occurrence of \"{0}\" was found."], _oldName)
            : string.Format(CultureInfo.CurrentCulture, L["Renamed {0} occurrences."], replaced.ToString(CultureInfo.CurrentCulture));

        CancelRename();

        if (replaced > 0)
        {
            await CommitAsync(content);
        }
    }

    private void CancelRename()
    {
        _isRenaming = false;
        _oldName = string.Empty;
        _newName = string.Empty;
    }

    private async Task CommitAsync(string content)
    {
        if (Context is null || Interaction is null || Context.Document.Content == content)
        {
            return;
        }

        _typedContent = null;
        _committedContent = content;

        await Interaction.ChangeDocumentAsync(Context.Document with { Content = content });
    }

    /// <inheritdoc />
    public void Dispose()
        => Plugin?.RenameRequested -= HandleRenameRequested;
}
