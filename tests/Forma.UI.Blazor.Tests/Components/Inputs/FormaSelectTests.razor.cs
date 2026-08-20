namespace Forma.UI.Blazor.Tests.Components.Inputs;

public sealed partial class FormaSelectTests
{
    public FormaSelectTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
