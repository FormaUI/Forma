namespace Forma.Blazor.Tests.Components.Inputs;

public sealed partial class FormaEmojiPickerTests
{
    private readonly Action? _completeSpriteLoad;
    private readonly Action? _failSpriteLoad;

    public FormaEmojiPickerTests(bool autoResolveSpriteLoad = true)
    {
        Services.AddJsonLocalization();
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Configured explicitly even under Loose, so a test can hold the load pending.
        var handler = JSInterop
            .SetupModule("./_content/Forma.Blazor/scripts/operations/Asset.js")
            .Setup<bool>("loadSprite", _ => true);

        if (autoResolveSpriteLoad)
        {
            handler.SetResult(true);
        }
        else
        {
            _completeSpriteLoad = () => handler.SetResult(true);
            _failSpriteLoad = () => handler.SetException(new JSException("The sprite never arrived."));
        }
    }

    /// <summary>Resolves every sprite load left pending by <c>autoResolveSpriteLoad: false</c>.</summary>
    internal void CompleteSpriteLoad()
        => _completeSpriteLoad?.Invoke();

    /// <summary>Fails every sprite load left pending by <c>autoResolveSpriteLoad: false</c>.</summary>
    internal void FailSpriteLoad()
        => _failSpriteLoad?.Invoke();
}
