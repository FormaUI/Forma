namespace Forma.Blazor.Tests.Components.Files;

using Forma.Framework.Localization;

public sealed partial class FormaFilePickerTests
{
    public FormaFilePickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
