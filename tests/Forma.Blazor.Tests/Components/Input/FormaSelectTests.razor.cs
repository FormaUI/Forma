namespace Forma.Blazor.Tests.Components.Input;

public sealed partial class FormaSelectTests
{
    public FormaSelectTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
