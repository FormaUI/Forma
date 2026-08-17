namespace Forma.UI.Blazor.Tests.Components.Gallery;

using Forma.Framework.Localization;

public sealed partial class FormaGalleryTests
{
    public FormaGalleryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
