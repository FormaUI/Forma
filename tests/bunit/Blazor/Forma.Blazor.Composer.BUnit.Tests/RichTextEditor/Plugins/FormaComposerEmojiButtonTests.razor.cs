namespace Forma.Blazor.Composer.BUnit.Tests.RichTextEditor.Plugins;

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
