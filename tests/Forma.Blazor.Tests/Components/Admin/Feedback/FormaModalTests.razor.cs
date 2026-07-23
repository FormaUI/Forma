namespace Forma.Blazor.Tests.Components.Admin.Feedback;

public sealed partial class FormaModalTests
{
    public FormaModalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
    }
}
