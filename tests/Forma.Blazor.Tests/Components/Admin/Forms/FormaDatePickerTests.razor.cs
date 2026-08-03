namespace Forma.Blazor.Tests.Components.Admin.Forms;

public sealed partial class FormaDatePickerTests
{
    public FormaDatePickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
    }
}
