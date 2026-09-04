namespace Forma.Blazor.Tests.Components.Feedback;

public sealed partial class FormaDialogTests
{
    public FormaDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
