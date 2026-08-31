namespace Forma.Blazor.Tests.Components.Files;

using Forma.Framework.Localization;

public sealed partial class FormaFileViewerTests
{
    public FormaFileViewerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
