namespace Forma.Blazor.Composer.Tests.RichTextEditor;

using Forma.Blazor.Components.Composer.RichTextEditor;
using Forma.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.Blazor.Components.Inputs;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Sanitization;
using Forma.Framework.Toolbar;
using Forma.Tests.Composer;

public sealed class RichTextPluginTests
{

    private static FormaToolbarMenu FindMenu(IReadOnlyList<ComposerToolbarItem> items, string id)
        => (FormaToolbarMenu)items.SelectMany(entry => entry.Items).SelectMany(Flatten).Single(item => item.Id == id);

    private static IEnumerable<FormaToolbarItem> Flatten(FormaToolbarItem item)
        => item is FormaToolbarGroup group ? group.Items.SelectMany(Flatten) : [item];

    private static ComposerContext CreateState(string html = "<p>Test</p>", params string[] activeStyles)
        => ComposerContexts.Create(
            html,
            activeStyles.Length == 0
                ? null
                : new ComposerSelection { ActiveStyles = new HashSet<string>(activeStyles, StringComparer.OrdinalIgnoreCase) });

    public sealed class BasicFormatting
    {
        [Fact]
        public async Task Should_dispatch_formatting_commands()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new BasicFormattingPlugin(editor);
            var state = CreateState();
            string? requestedCommand = null;
            editor.CommandRequested += (cmd, _) => requestedCommand = cmd;

            // Act
            await plugin.ExecuteAsync(BasicFormattingPlugin.BoldCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("bold", requestedCommand);
            Assert.Equal("basic-formatting", plugin.Id);
            Assert.NotEmpty(plugin.Items);
            Assert.NotEmpty(plugin.KeyBindings);
        }
    }

    public sealed class ParagraphFormat
    {
        [Fact]
        public async Task Should_dispatch_block_commands()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new ParagraphFormatPlugin(editor);
            var state = CreateState();
            string? requestedCommand = null;
            object? requestedValue = null;
            editor.CommandRequested += (cmd, val) =>
            {
                requestedCommand = cmd;
                requestedValue = val;
            };

            // Act
            await plugin.ExecuteAsync(ParagraphFormatPlugin.Heading1Command, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("formatBlock", requestedCommand);
            Assert.Equal("h1", requestedValue);
            Assert.Equal("paragraph-format", plugin.Id);
        }
    }

    public sealed class Lists
    {
        [Fact]
        public async Task Should_dispatch_list_commands()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new ListPlugin(editor);
            var state = CreateState();
            string? requestedCommand = null;
            editor.CommandRequested += (cmd, _) => requestedCommand = cmd;

            // Act
            await plugin.ExecuteAsync(ListPlugin.BulletListCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("insertUnorderedList", requestedCommand);
            Assert.Equal("list", plugin.Id);
        }
    }

    public sealed class AlignmentBidi
    {
        [Fact]
        public async Task Should_dispatch_alignment_and_direction_commands()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new AlignmentBidiPlugin(editor);
            var state = CreateState();
            string? requestedCommand = null;
            object? requestedValue = null;
            editor.CommandRequested += (cmd, val) =>
            {
                requestedCommand = cmd;
                requestedValue = val;
            };

            // Act
            await plugin.ExecuteAsync(AlignmentBidiPlugin.AlignRightCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);
            Assert.Equal("justifyRight", requestedCommand);

            await plugin.ExecuteAsync(AlignmentBidiPlugin.DirectionRtlCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);
            Assert.Equal("dir", requestedCommand);
            Assert.Equal("rtl", requestedValue);
        }
    }

    public sealed class Typography
    {
        [Fact]
        public async Task Should_dispatch_typography_commands()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new TypographyPlugin(editor);
            var state = CreateState();
            string? requestedCommand = null;
            object? requestedValue = null;
            editor.CommandRequested += (cmd, val) =>
            {
                requestedCommand = cmd;
                requestedValue = val;
            };

            // Act
            await plugin.ExecuteAsync(TypographyPlugin.TextColorCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("foreColor", requestedCommand);
            Assert.NotNull(requestedValue);
            Assert.Equal("typography", plugin.Id);
        }

        [Theory]
        [InlineData(ComposerFontSizeUnit.Rem, "16", "1rem")]
        [InlineData(ComposerFontSizeUnit.Rem, "12", "0.75rem")]
        [InlineData(ComposerFontSizeUnit.Rem, "72", "4.5rem")]
        [InlineData(ComposerFontSizeUnit.Points, "16", "16pt")]
        public void Should_write_a_font_size_in_the_configured_unit(
            ComposerFontSizeUnit unit,
            string label,
            string expected)
        {
            // Arrange
            var plugin = new TypographyPlugin(
                new RichTextEditorPlugin(),
                new TypographyPluginOptions { FontSizeUnit = unit, FontSizes = [label] });

            // Act
            var option = FindMenu(plugin.Items, "font-size").Options.Single();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(label, option.Title),
                () => Assert.Equal($"font-size:{expected}", option.Id),
                () => Assert.Equal($"{TypographyPlugin.FontSizeCommand}:{expected}", option.Command));
        }

        [Fact]
        public void Should_scale_authored_sizes_with_the_reader_by_default()
        {
            // Arrange
            var plugin = new TypographyPlugin(new RichTextEditorPlugin());

            // Act
            var options = FindMenu(plugin.Items, "font-size").Options;

            // Assert
            Assert.All(options, option => Assert.EndsWith("rem", option.Command, StringComparison.Ordinal));
        }
    }

    public sealed class Link
    {
        [Fact]
        public async Task Should_open_a_dialog_when_the_caret_is_not_already_on_one()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new LinkPlugin(editor);
            var state = CreateState();
            bool? isAnchorRequested = null;

            editor.LinkDialogRequested += isAnchor => isAnchorRequested = isAnchor;

            // Act
            await plugin.ExecuteAsync(LinkPlugin.InsertLinkCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.False(isAnchorRequested);

            // Act
            await plugin.ExecuteAsync(LinkPlugin.InsertAnchorCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.True(isAnchorRequested);
            Assert.Equal("link", plugin.Id);
            Assert.NotEmpty(plugin.KeyBindings);
        }

        [Fact]
        public async Task Should_remove_through_its_own_command_rather_than_a_toggle()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new LinkPlugin(editor);
            var dialogRequested = false;
            string? requestedCommand = null;

            editor.LinkDialogRequested += _ => dialogRequested = true;
            editor.CommandRequested += (cmd, _) => requestedCommand = cmd;

            // Act
            await plugin.ExecuteAsync(
                LinkPlugin.RemoveLinkCommand,
                CreateState(activeStyles: LinkPlugin.LinkStyle),
                new RecordingComposerInteraction(),
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("unlink", requestedCommand);

            // Act
            await plugin.ExecuteAsync(
                LinkPlugin.RemoveAnchorCommand,
                CreateState(activeStyles: LinkPlugin.AnchorStyle),
                new RecordingComposerInteraction(),
                TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("removeAnchor", requestedCommand);
            Assert.False(dialogRequested);
        }
    }

    public sealed class HorizontalRule
    {
        [Fact]
        public async Task Should_dispatch_rule_command()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new HorizontalRulePlugin(editor);
            var state = CreateState();
            string? requestedCommand = null;
            editor.CommandRequested += (cmd, _) => requestedCommand = cmd;

            // Act
            await plugin.ExecuteAsync(HorizontalRulePlugin.InsertRuleCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("insertHorizontalRule", requestedCommand);
            Assert.Equal("horizontal-rule", plugin.Id);
        }
    }

    public sealed class Clipboard
    {
        [Fact]
        public async Task Should_dispatch_clipboard_commands()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new ClipboardPlugin(editor);
            var state = CreateState();
            string? requestedCommand = null;
            editor.CommandRequested += (cmd, _) => requestedCommand = cmd;

            // Act & Assert
            await plugin.ExecuteAsync(ClipboardPlugin.SelectAllCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);
            Assert.Equal("selectAll", requestedCommand);

            await plugin.ExecuteAsync(ClipboardPlugin.PasteTextCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);
            Assert.Equal("pasteText", requestedCommand);

            await plugin.ExecuteAsync(ClipboardPlugin.PasteWordCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);
            Assert.Equal("pasteWord", requestedCommand);
            Assert.Equal("clipboard", plugin.Id);
        }
    }

    public sealed class SpecialCharacter
    {
        [Fact]
        public async Task Should_dispatch_character_insert()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new SpecialCharacterPlugin(editor);
            var state = CreateState();
            string? requestedCommand = null;
            object? requestedValue = null;
            editor.CommandRequested += (cmd, val) =>
            {
                requestedCommand = cmd;
                requestedValue = val;
            };

            // Act
            await plugin.ExecuteAsync($"{SpecialCharacterPlugin.InsertCharacterPrefix}©", state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("insertText", requestedCommand);
            Assert.Equal("©", requestedValue);
            Assert.Equal("special-character", plugin.Id);
        }
    }

    public sealed class Emoji
    {
        [Fact]
        public async Task Should_dispatch_emoji_insert()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new EmojiPlugin(editor);
            var state = CreateState();
            string? requestedCommand = null;
            object? requestedValue = null;
            editor.CommandRequested += (cmd, val) =>
            {
                requestedCommand = cmd;
                requestedValue = val;
            };

            // Act
            await plugin.ExecuteAsync($"{EmojiPlugin.InsertEmojiPrefix}🎉", state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("insertText", requestedCommand);
            Assert.Equal("🎉", requestedValue);
            Assert.Equal("emoji", plugin.Id);
        }

        [Fact]
        public void Should_contribute_the_composer_emoji_button()
        {
            // Arrange
            var plugin = new EmojiPlugin(new RichTextEditorPlugin(), new EmojiPluginOptions { Picker = new() { MaxRecent = 12 } });

            // Act
            var item = (FormaToolbarCustomItem)plugin.Items.SelectMany(entry => entry.Items).SelectMany(Flatten).Single(entry => entry.Id == "emoji");

            // Assert
            Assert.Equal(typeof(FormaComposerEmojiButton), item.Component);
            Assert.Equal(12, ((EmojiPickerOptions)item.Parameters?["PickerOptions"]!).MaxRecent);
        }
    }

    public sealed class Table
    {
        [Fact]
        public async Task Should_dispatch_table_commands()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new TablePlugin(editor);
            var state = CreateState();
            var tableDialogRequested = false;
            string? requestedCommand = null;

            editor.TableDialogRequested += () => tableDialogRequested = true;
            editor.CommandRequested += (cmd, _) => requestedCommand = cmd;

            // Act & Assert
            await plugin.ExecuteAsync(TablePlugin.InsertTableCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);
            Assert.True(tableDialogRequested);

            await plugin.ExecuteAsync(TablePlugin.AddRowBeforeCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);
            Assert.Equal("tableAddRowBefore", requestedCommand);

            await plugin.ExecuteAsync(TablePlugin.DeleteTableCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);
            Assert.Equal("tableDelete", requestedCommand);
            Assert.Equal("table", plugin.Id);
        }
    }

    public sealed class Image
    {
        [Fact]
        public async Task Should_dispatch_image_request()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new ImagePlugin(editor);
            var state = CreateState();
            var requested = false;
            editor.ImageDialogRequested += () => requested = true;

            // Act
            await plugin.ExecuteAsync(ImagePlugin.InsertImageCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.True(requested);
            Assert.Equal("image", plugin.Id);
        }
    }

    public sealed class Explorer
    {
        [Fact]
        public async Task Should_dispatch_browse_request()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new ExplorerPlugin(editor);
            var state = CreateState();
            ExplorerPluginOptions? requestedOptions = null;
            editor.ExplorerDialogRequested += options => requestedOptions = options;

            // Act
            await plugin.ExecuteAsync(ExplorerPlugin.BrowseCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.Same(plugin.Options, requestedOptions);
            Assert.Equal("explorer", plugin.Id);
        }

        [Fact]
        public async Task Should_carry_the_limits_the_host_set()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new ExplorerPlugin(editor, new ExplorerPluginOptions
            {
                AcceptedResourceKinds = "contentType:image",
                AcceptedUploadKinds = "globs:*.png",
                AllowUpload = false,
                InsertMode = MediaInsertMode.Both,
            });
            var state = CreateState();
            ExplorerPluginOptions? requestedOptions = null;
            editor.ExplorerDialogRequested += options => requestedOptions = options;

            // Act
            await plugin.ExecuteAsync(ExplorerPlugin.BrowseCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(requestedOptions);
            Assert.Equal("contentType:image", requestedOptions.AcceptedResourceKinds);
            Assert.Equal("globs:*.png", requestedOptions.AcceptedUploadKinds);
            Assert.False(requestedOptions.AllowUpload);
            Assert.Equal(MediaInsertMode.Both, requestedOptions.InsertMode);
        }

        [Fact]
        public void Should_default_to_embedding_everything_it_can()
        {
            // Arrange & Act
            var plugin = new ExplorerPlugin(new RichTextEditorPlugin());

            // Assert
            Assert.Null(plugin.Options.AcceptedResourceKinds);
            Assert.True(plugin.Options.AllowUpload);
            Assert.Equal(MediaInsertMode.Embed, plugin.Options.InsertMode);
        }
    }

    public sealed class Video
    {
        [Fact]
        public async Task Should_dispatch_video_request()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new VideoPlugin(editor);
            var state = CreateState();
            var videoDialogRequested = false;
            editor.VideoDialogRequested += () => videoDialogRequested = true;

            // Act
            await plugin.ExecuteAsync(VideoPlugin.InsertVideoCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.True(videoDialogRequested);
            Assert.Equal("video", plugin.Id);
        }
    }

    public sealed class Template
    {
        [Fact]
        public async Task Should_dispatch_template_insert()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var template = new ComposerTemplate("sample", "Sample", "Sample description", "<p>Sample</p>");
            var plugin = new TemplatePlugin(editor, new TemplatePluginOptions { Templates = [template] });
            var state = CreateState();
            string? requestedCommand = null;
            object? requestedValue = null;
            editor.CommandRequested += (cmd, val) =>
            {
                requestedCommand = cmd;
                requestedValue = val;
            };

            // Act
            await plugin.ExecuteAsync($"{TemplatePlugin.InsertTemplatePrefix}sample", state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("insertHtml", requestedCommand);
            Assert.Equal("<p>Sample</p>", requestedValue);
            Assert.Equal("template", plugin.Id);
        }
    }

    public sealed class Embed
    {
        [Fact]
        public async Task Should_dispatch_embed_request()
        {
            // Arrange
            var editor = new RichTextEditorPlugin();
            var plugin = new EmbedPlugin(editor);
            var state = CreateState();
            var embedDialogRequested = false;
            editor.EmbedDialogRequested += () => embedDialogRequested = true;

            // Act
            await plugin.ExecuteAsync(EmbedPlugin.InsertEmbedCommand, state, new RecordingComposerInteraction(), TestContext.Current.CancellationToken);

            // Assert
            Assert.True(embedDialogRequested);
            Assert.Equal("embed", plugin.Id);
        }
    }
}
