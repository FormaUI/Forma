namespace Forma.AddOns.Composer.Plugins.Explorer.Tests;

using Forma.Blazor.Components.Composer.Plugins;
using Forma.Framework.Composer.Dialogs;
using Forma.Framework.Composer.Editing;
using Forma.Tests.Composer;

public sealed class ExplorerPluginTests
{
    public sealed class Browsing
    {
        [Fact]
        public async Task Should_ask_the_composer_to_open_the_stored_files_dialog()
        {
            // Arrange
            var plugin = new ExplorerPlugin();
            var interaction = new RecordingComposerInteraction();

            // Act
            await plugin.ExecuteAsync(ExplorerPlugin.BrowseCommand, ComposerContexts.Create(), interaction, TestContext.Current.CancellationToken);

            // Assert
            var request = Assert.Single(interaction.OpenedDialogs);
            Assert.Equal(typeof(FormaComposerExplorerDialog), request.Body);
            Assert.Equal(ComposerDialogSize.Large, request.Size);
            Assert.Equal("explorer", plugin.Id);
        }

        [Fact]
        public async Task Should_open_nothing_for_a_command_it_does_not_own()
        {
            // Arrange
            var plugin = new ExplorerPlugin();
            var interaction = new RecordingComposerInteraction();

            // Act
            await plugin.ExecuteAsync("something.else", ComposerContexts.Create(), interaction, TestContext.Current.CancellationToken);

            // Assert
            Assert.Empty(interaction.OpenedDialogs);
        }

        [Fact]
        public async Task Should_carry_the_limits_the_host_set()
        {
            // Arrange
            var plugin = new ExplorerPlugin(new ExplorerPluginOptions
            {
                AcceptedResourceKinds = "contentType:image",
                AcceptedUploadKinds = "globs:*.png",
                AllowUpload = false,
                InsertMode = MediaInsertMode.Both,
            });
            var interaction = new RecordingComposerInteraction();

            // Act
            await plugin.ExecuteAsync(ExplorerPlugin.BrowseCommand, ComposerContexts.Create(), interaction, TestContext.Current.CancellationToken);

            // Assert
            var request = Assert.Single(interaction.OpenedDialogs);
            var carried = Assert.IsType<ExplorerPluginOptions>(request.Parameters[nameof(FormaComposerExplorerDialog.Options)]);
            Assert.Multiple(
                () => Assert.Equal("contentType:image", carried.AcceptedResourceKinds),
                () => Assert.Equal("globs:*.png", carried.AcceptedUploadKinds),
                () => Assert.False(carried.AllowUpload),
                () => Assert.Equal(MediaInsertMode.Both, carried.InsertMode));
        }
    }

    public sealed class Options
    {
        [Fact]
        public void Should_default_to_embedding_everything_it_can()
        {
            // Arrange & Act
            var plugin = new ExplorerPlugin();

            // Assert
            Assert.Null(plugin.Options.AcceptedResourceKinds);
            Assert.True(plugin.Options.AllowUpload);
            Assert.Equal(MediaInsertMode.Embed, plugin.Options.InsertMode);
        }
    }
}
