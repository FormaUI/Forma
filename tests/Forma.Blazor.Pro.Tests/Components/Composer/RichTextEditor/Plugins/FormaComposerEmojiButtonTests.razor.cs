namespace Forma.Blazor.Pro.Tests.Components.Composer.RichTextEditor.Plugins;

public sealed partial class FormaComposerEmojiButtonTests
{
    public FormaComposerEmojiButtonTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop
            .SetupModule("./_content/forma/common/scripts/operations/Asset.js")
            .Setup<bool>("loadSprite", _ => true)
            .SetResult(true);
    }
}
