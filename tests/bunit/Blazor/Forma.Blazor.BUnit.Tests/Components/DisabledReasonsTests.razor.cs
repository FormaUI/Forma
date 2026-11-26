namespace Forma.Blazor.BUnit.Tests.Components;

public sealed partial class DisabledReasonsTests
{
    public DisabledReasonsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/forma/input/scripts/InputRouter.js");
    }
}
