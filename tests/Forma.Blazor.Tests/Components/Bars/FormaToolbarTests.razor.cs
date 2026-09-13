namespace Forma.Blazor.Tests.Components.Bars;

public sealed partial class FormaToolbarTests
{
    public FormaToolbarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Router = JSInterop.SetupModule("./_content/forma/input/scripts/InputRouter.js");
        Focus = JSInterop.SetupModule("./_content/forma/common/scripts/behaviors/Focus.js");
    }

    /// <summary>The input router module, set up before any render so the toolbar's registrations on it are recorded.</summary>
    internal BunitJSModuleInterop Router { get; }

    /// <summary>The focus module, set up before any render so the toolbar's attachment to it is recorded.</summary>
    internal BunitJSModuleInterop Focus { get; }
}
