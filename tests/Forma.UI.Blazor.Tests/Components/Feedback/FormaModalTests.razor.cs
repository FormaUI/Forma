namespace Forma.UI.Blazor.Tests.Components.Feedback;

public sealed partial class FormaModalTests
{
    public FormaModalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
