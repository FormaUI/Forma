namespace Forma.UI.Blazor.Tests.Components.Input;

public sealed partial class FormaSelectTests
{
    public FormaSelectTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
