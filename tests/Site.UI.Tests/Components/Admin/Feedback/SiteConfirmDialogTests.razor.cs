namespace Site.UI.Tests.Components.Admin.Feedback;

public sealed partial class SiteConfirmDialogTests
{
    public SiteConfirmDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
        Services.AddSingleton<IConfirmDialogService, ConfirmDialogService>();
    }
}
