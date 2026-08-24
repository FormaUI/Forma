namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Glyphs;

/// <summary>Contributes hyperlink and anchor creation and removal to the rich text editor.</summary>
public sealed class LinkPlugin(RichTextEditorPlugin editor) : IComposerPlugin
{
    /// <summary>The command adding a hyperlink, or removing the one the caret sits in.</summary>
    public const string ToggleLinkCommand = "link.toggle";

    /// <summary>The command adding a named anchor, or removing the one the caret sits in.</summary>
    public const string ToggleAnchorCommand = "link.toggle-anchor";

    /// <summary>The active style reported while the caret sits inside a hyperlink.</summary>
    public const string LinkStyle = "link";

    /// <summary>The active style reported while the caret sits inside a named anchor.</summary>
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
                    new FormaToolbarToggle
                    {
                        Id = LinkStyle,
                        Title = "Insert Link",
                        AlternateTitle = "Remove Link",
                        Icon = Glyph.Link,
                        AlternateIcon = Glyph.LinkSlash,
                        Command = ToggleLinkCommand,
                        ShortcutDisplay = "Ctrl+K",
                        ShortcutAriaKeys = "Control+k",
                    },
                    new FormaToolbarToggle
                    {
                        Id = AnchorStyle,
                        Title = "Insert Anchor",
                        AlternateTitle = "Remove Anchor",
                        Icon = Glyph.Anchor,
                        AlternateIcon = Glyph.AnchorCircleXmark,
                        Command = ToggleAnchorCommand,
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
    [
        new() { Key = "k", Command = ToggleLinkCommand, Control = true },
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
            case ToggleLinkCommand when context.Selection.ActiveStyles.Contains(LinkStyle):
                _editor.RequestCommand("unlink");
                break;

            case ToggleLinkCommand:
                _editor.RequestLinkDialog(isAnchor: false);
                break;

            case ToggleAnchorCommand when context.Selection.ActiveStyles.Contains(AnchorStyle):
                _editor.RequestCommand("removeAnchor");
                break;

            case ToggleAnchorCommand:
                _editor.RequestLinkDialog(isAnchor: true);
                break;
        }

        return ValueTask.CompletedTask;
    }
}
