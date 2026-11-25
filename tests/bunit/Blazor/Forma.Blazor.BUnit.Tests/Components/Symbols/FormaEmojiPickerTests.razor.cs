namespace Forma.Blazor.BUnit.Tests.Components.Symbols;

using System.Text.Json;

using Forma.Blazor.Features.Emojis;
using Forma.Tests.Interop;

public sealed partial class FormaEmojiPickerTests
{
    public FormaEmojiPickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Configured explicitly even under Loose, so the sheets a test counts are fetched through the real loader.
        JSInterop
            .SetupModule("./_content/forma/browser/scripts/operations/Asset.js")
            .Setup<JsonElement>("loadSprite", _ => true)
            .SetResult(InteropAnswer.Of(true));
    }

    /// <summary>Puts a loader in place whose loads stay pending until the test finishes them.</summary>
    internal FakeEmojiSpriteLoader HoldSpriteLoads()
    {
        var loader = new FakeEmojiSpriteLoader();
        Services.AddScoped<IEmojiSpriteLoader>(_ => loader);

        return loader;
    }
}
