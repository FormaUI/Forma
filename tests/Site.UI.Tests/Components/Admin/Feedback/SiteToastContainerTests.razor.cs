namespace Site.UI.Tests.Components.Admin.Feedback;

public sealed partial class SiteToastContainerTests
{
    public SiteToastContainerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
        Services.AddSingleton<IToastService, ToastService>();
    }
}
