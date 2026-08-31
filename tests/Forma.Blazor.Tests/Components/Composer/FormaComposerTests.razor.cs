namespace Forma.Blazor.Tests.Components.Composer;

using Forma.Framework.Localization;

public sealed partial class FormaComposerTests
{
    public FormaComposerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
