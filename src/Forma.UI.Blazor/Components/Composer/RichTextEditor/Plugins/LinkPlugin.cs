namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Glyphs;

/// <summary>Contributes hyperlink and anchor creation, editing, and removal operations to the rich text editor.</summary>
public sealed class LinkPlugin(RichTextEditorPlugin editor) : IComposerPlugin
{
    /// <summary>The command opening the insert or edit link dialog.</summary>
    public const string InsertLinkCommand = "link.insert";

    /// <summary>The command removing the hyperlink from the active selection.</summary>
    public const string UnlinkCommand = "link.unlink";

    /// <summary>The command opening the insert anchor dialog.</summary>
    public const string AnchorCommand = "link.anchor";

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
                        Id = "link",
                        Title = "Insert Link",
                        Icon = Glyph.Link,
                        Command = InsertLinkCommand,
                        ShortcutDisplay = "Ctrl+K",
                        ShortcutAriaKeys = "Control+k",
                    },
                    new FormaToolbarCommand
                    {
                        Id = "unlink",
                        Title = "Remove Link",
                        Icon = Glyph.Link,
                        Command = UnlinkCommand,
                    },
                    new FormaToolbarCommand
                    {
                        Id = "anchor",
                        Title = "Insert Anchor",
                        Icon = Glyph.LocationCrosshairs,
                        Command = AnchorCommand,
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

            case AnchorCommand:
                _editor.RequestLinkDialog(isAnchor: true);
                break;

            case UnlinkCommand:
                _editor.RequestCommand("unlink");
                break;
        }

        return ValueTask.CompletedTask;
    }
}
