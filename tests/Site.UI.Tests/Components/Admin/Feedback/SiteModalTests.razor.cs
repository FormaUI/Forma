namespace Site.UI.Tests.Components.Admin.Feedback;

public sealed partial class SiteModalTests
{
    public SiteModalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
    }
}
