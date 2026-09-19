namespace Forma.Blazor.Composer.Tests.RichTextEditor.Plugins;

public sealed partial class FormaComposerEmojiButtonTests
{
    public FormaComposerEmojiButtonTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop
            .SetupModule("./_content/forma/browser/scripts/operations/Asset.js")
            .Setup<bool>("loadSprite", _ => true)
            .SetResult(true);
    }
}
