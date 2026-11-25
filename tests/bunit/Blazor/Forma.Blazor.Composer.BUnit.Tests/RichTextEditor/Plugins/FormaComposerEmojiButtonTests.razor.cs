namespace Forma.Blazor.Composer.BUnit.Tests.RichTextEditor.Plugins;

using System.Text.Json;

using Forma.Tests.Interop;

public sealed partial class FormaComposerEmojiButtonTests
{
    public FormaComposerEmojiButtonTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        JSInterop
            .SetupModule("./_content/forma/browser/scripts/operations/Asset.js")
            .Setup<JsonElement>("loadSprite", _ => true)
            .SetResult(InteropAnswer.Of(true));
    }
}
