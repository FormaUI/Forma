namespace Forma.UI.Blazor.Tests.Components.Feedback;

public sealed partial class FormaToastContainerTests
{
    public FormaToastContainerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
        Services.AddSingleton<IToastService, ToastService>();
    }
}
