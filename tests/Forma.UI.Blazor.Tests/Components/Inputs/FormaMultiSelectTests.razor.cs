namespace Forma.UI.Blazor.Tests.Components.Inputs;

public sealed partial class FormaMultiSelectTests
{
    public FormaMultiSelectTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
