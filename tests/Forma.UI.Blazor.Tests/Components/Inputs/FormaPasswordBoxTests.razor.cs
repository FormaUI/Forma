namespace Forma.UI.Blazor.Tests.Components.Inputs;

public sealed partial class FormaPasswordBoxTests
{
    public FormaPasswordBoxTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
