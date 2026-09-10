namespace Forma.Blazor.Tests.Components.Feedback;

public sealed partial class FormaConfirmDialogTests
{
    public FormaConfirmDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IConfirmDialogService, ConfirmDialogService>();
    }
}
