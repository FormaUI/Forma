namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes text alignment and paragraph direction operations.</summary>
public sealed class AlignmentBidiPlugin(RichTextEditorPlugin editor) : IComposerFeaturePlugin
{
    /// <summary>The command aligning text to the left.</summary>
    public const string AlignLeftCommand = "align.left";

    /// <summary>The command aligning text to the center.</summary>
    public const string AlignCenterCommand = "align.center";

    /// <summary>The command aligning text to the right.</summary>
    public const string AlignRightCommand = "align.right";

    /// <summary>The command justifying text.</summary>
    public const string AlignJustifyCommand = "align.justify";

    /// <summary>The command setting text direction to left-to-right.</summary>
    public const string DirectionLtrCommand = "dir.ltr";

    /// <summary>The command setting text direction to right-to-left.</summary>
    public const string DirectionRtlCommand = "dir.rtl";

    private readonly RichTextEditorPlugin _editorPlugin = editor;

    /// <inheritdoc />
    public string Id => "alignment-bidi";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Paragraph,
        [
            new ToolbarGroup
            {
                Id = "alignment",
                Title = "Alignment",
                Items =
                [
                    new ToolbarToggle { Id = "align-left", Title = "Align Left", Icon = Glyph.AlignLeft, Command = AlignLeftCommand },
                    new ToolbarToggle { Id = "align-center", Title = "Align Center", Icon = Glyph.AlignCenter, Command = AlignCenterCommand },
                    new ToolbarToggle { Id = "align-right", Title = "Align Right", Icon = Glyph.AlignRight, Command = AlignRightCommand },
                    new ToolbarToggle { Id = "align-justify", Title = "Justify", Icon = Glyph.AlignJustify, Command = AlignJustifyCommand },
                ],
            },
            new ToolbarGroup
            {
                Id = "text-direction",
                Title = "Text Direction",
                Items =
                [
                    new ToolbarToggle { Id = "dir-ltr", Title = "Left to Right", Icon = Glyph.ParagraphDirectionLtr, Command = DirectionLtrCommand },
                    new ToolbarToggle { Id = "dir-rtl", Title = "Right to Left", Icon = Glyph.ParagraphDirectionRtl, Command = DirectionRtlCommand },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
    [
        new() { Key = "l", Command = AlignLeftCommand, Control = true },
        new() { Key = "e", Command = AlignCenterCommand, Control = true },
        new() { Key = "r", Command = AlignRightCommand, Control = true },
        new() { Key = "j", Command = AlignJustifyCommand, Control = true },
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

        var (jsCommand, jsValue) = command switch
        {
            AlignLeftCommand => ("justifyLeft", (object?)null),
            AlignCenterCommand => ("justifyCenter", null),
            AlignRightCommand => ("justifyRight", null),
            AlignJustifyCommand => ("justifyFull", null),
            DirectionLtrCommand => ("dir", "ltr"),
            DirectionRtlCommand => ("dir", "rtl"),
            _ => (null, null),
        };

        if (jsCommand is not null)
        {
            _editorPlugin.RequestCommand(jsCommand, jsValue);
        }

        return ValueTask.CompletedTask;
    }
}
