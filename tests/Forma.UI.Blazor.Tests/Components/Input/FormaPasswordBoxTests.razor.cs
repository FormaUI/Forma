namespace Forma.UI.Blazor.Tests.Components.Input;

public sealed partial class FormaPasswordBoxTests
{
    public FormaPasswordBoxTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
