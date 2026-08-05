namespace Forma.Blazor.Tests.Components.Feedback;

public sealed partial class FormaToastContainerTests
{
    public FormaToastContainerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
        Services.AddSingleton<IToastService, ToastService>();
    }
}
