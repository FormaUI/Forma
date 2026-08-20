namespace Forma.UI.Blazor.Tests.Components.Input;

public sealed partial class FormaMultiSelectTests
{
    public FormaMultiSelectTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
