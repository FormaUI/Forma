namespace Forma.Blazor.Tests.Components.Media;

using Forma.Localization;

public sealed partial class FormaVideoPlayerTests
{
    public FormaVideoPlayerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
