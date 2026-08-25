namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Glyphs;

/// <summary>Contributes hyperlink and anchor creation and removal to the rich text editor.</summary>
public sealed class LinkPlugin(RichTextEditorPlugin editor) : IComposerPlugin
{
    /// <summary>The command adding a hyperlink, or retargeting whichever ones the selection covers.</summary>
    public const string InsertLinkCommand = "link.insert";

    /// <summary>The command removing every hyperlink the selection touches.</summary>
    public const string RemoveLinkCommand = "link.remove";

    /// <summary>The command adding a named anchor.</summary>
    public const string InsertAnchorCommand = "link.insert-anchor";

    /// <summary>The command removing every named anchor the selection touches.</summary>
    public const string RemoveAnchorCommand = "link.remove-anchor";

    /// <summary>The active style reported while the selection sits inside or overlaps a hyperlink.</summary>
    public const string LinkStyle = "link";

    /// <summary>The active style reported while the selection sits inside or overlaps a named anchor.</summary>
    public const string AnchorStyle = "anchor";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <inheritdoc />
    public string Id => "link";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Formatting,
        [
            new FormaToolbarGroup
            {
                Id = "links",
                Title = "Links",
                Items =
                [
                    new FormaToolbarCommand
                    {
                        Id = "link-insert",
                        Title = "Insert Link",
                        Icon = Glyph.Link,
                        Command = InsertLinkCommand,
                        ShortcutDisplay = "Ctrl+K",
                        ShortcutAriaKeys = "Control+k",
                    },
                    new FormaToolbarCommand
                    {
                        Id = "link-remove",
                        Title = "Remove Link",
                        Icon = Glyph.LinkSlash,
                        Command = RemoveLinkCommand,
                        RequiresState = LinkStyle,
                    },
                    new FormaToolbarCommand
                    {
                        Id = "anchor-insert",
                        Title = "Insert Anchor",
                        Icon = Glyph.Anchor,
                        Command = InsertAnchorCommand,
                    },
                    new FormaToolbarCommand
                    {
                        Id = "anchor-remove",
                        Title = "Remove Anchor",
                        Icon = Glyph.AnchorCircleXmark,
                        Command = RemoveAnchorCommand,
                        RequiresState = AnchorStyle,
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
    [
        new() { Key = "k", Command = InsertLinkCommand, Control = true },
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerAsset> Assets => [];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);

        switch (command)
        {
            case InsertLinkCommand:
                _editor.RequestLinkDialog(isAnchor: false);
                break;

            case RemoveLinkCommand:
                _editor.RequestCommand("unlink");
                break;

            case InsertAnchorCommand:
                _editor.RequestLinkDialog(isAnchor: true);
                break;

            case RemoveAnchorCommand:
                _editor.RequestCommand("removeAnchor");
                break;
        }

        return ValueTask.CompletedTask;
    }
}
