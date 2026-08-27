namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

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

    private readonly RichTextEditorPlugin _editor = editor;

    /// <inheritdoc />
    public string Id => "alignment-bidi";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Paragraph,
        [
            new FormaToolbarGroup
            {
                Id = "alignment",
                Title = "Alignment",
                Items =
                [
                    new FormaToolbarToggle { Id = "align-left", Title = "Align Left", Icon = Glyph.AlignLeft, Command = AlignLeftCommand },
                    new FormaToolbarToggle { Id = "align-center", Title = "Align Center", Icon = Glyph.AlignCenter, Command = AlignCenterCommand },
                    new FormaToolbarToggle { Id = "align-right", Title = "Align Right", Icon = Glyph.AlignRight, Command = AlignRightCommand },
                    new FormaToolbarToggle { Id = "align-justify", Title = "Justify", Icon = Glyph.AlignJustify, Command = AlignJustifyCommand },
                ],
            },
            new FormaToolbarGroup
            {
                Id = "text-direction",
                Title = "Text Direction",
                Items =
                [
                    new FormaToolbarToggle { Id = "dir-ltr", Title = "Left to Right", Icon = Glyph.TextDirectionLtr, Command = DirectionLtrCommand },
                    new FormaToolbarToggle { Id = "dir-rtl", Title = "Right to Left", Icon = Glyph.TextDirectionRtl, Command = DirectionRtlCommand },
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
    public ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);

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
            _editor.RequestCommand(jsCommand, jsValue);
        }

        return ValueTask.CompletedTask;
    }
}
