namespace Forma.UI.Blazor.Tests.Components.Media;

using Forma.Framework.Localization;

public sealed partial class FormaVideoPlayerTests
{
    public FormaVideoPlayerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
