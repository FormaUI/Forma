namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Features.Composer.Assets;
using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes hyperlink and anchor creation and removal to the rich text editor.</summary>
public sealed class LinkPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
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

    private readonly RichTextEditorPlugin _editorPlugin = editor;

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
                        Icon = Glyph.RemoveLink,
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
                        Icon = Glyph.RemoveAnchor,
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
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        switch (command)
        {
            case InsertLinkCommand:
                _editorPlugin.RequestLinkDialog(isAnchor: false);
                break;

            case RemoveLinkCommand:
                _editorPlugin.RequestCommand("unlink");
                break;

            case InsertAnchorCommand:
                _editorPlugin.RequestLinkDialog(isAnchor: true);
                break;

            case RemoveAnchorCommand:
                _editorPlugin.RequestCommand("removeAnchor");
                break;
        }

        return ValueTask.CompletedTask;
    }
}
