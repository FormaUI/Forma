namespace Forma.Blazor.Tests.Components.Admin.Feedback;

public sealed partial class FormaConfirmDialogTests
{
    public FormaConfirmDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
        Services.AddSingleton<IConfirmDialogService, ConfirmDialogService>();
    }
}
