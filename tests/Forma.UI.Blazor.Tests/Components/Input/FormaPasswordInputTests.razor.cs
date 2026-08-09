namespace Forma.UI.Blazor.Tests.Components.Input;

public sealed partial class FormaPasswordInputTests
{
    public FormaPasswordInputTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
