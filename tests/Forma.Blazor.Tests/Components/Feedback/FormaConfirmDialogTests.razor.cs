namespace Forma.Blazor.Tests.Components.Feedback;

public sealed partial class FormaConfirmDialogTests
{
    public FormaConfirmDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
        Services.AddSingleton<IConfirmDialogService, ConfirmDialogService>();
    }
}
