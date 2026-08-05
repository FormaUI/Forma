namespace Forma.Blazor.Tests.Components.Input;

public sealed partial class FormaDatePickerTests
{
    public FormaDatePickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
    }
}
