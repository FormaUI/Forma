namespace Forma.UI.Blazor.Tests.Components.Inputs;

public sealed partial class FormaDatePickerTests
{
    public FormaDatePickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
