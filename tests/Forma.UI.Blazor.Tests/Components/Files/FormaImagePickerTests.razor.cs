namespace Forma.UI.Blazor.Tests.Components.Files;

using Forma.Framework.Localization;

public sealed partial class FormaImagePickerTests
{
    public FormaImagePickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
