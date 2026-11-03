namespace Forma.Blazor.BUnit.Tests.Components.Feedback;

public sealed partial class FormaToastContainerTests
{
    public FormaToastContainerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IToastService, ToastService>();
    }
}
