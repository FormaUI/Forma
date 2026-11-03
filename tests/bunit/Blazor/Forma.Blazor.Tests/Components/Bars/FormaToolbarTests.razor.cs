namespace Forma.Blazor.Tests.Components.Bars;

public sealed partial class FormaToolbarTests
{
    public FormaToolbarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Router = JSInterop.SetupModule("./_content/forma/input/scripts/InputRouter.js");
    }

    /// <summary>The input router module, which records the toolbar's registrations.</summary>
    internal BunitJSModuleInterop Router { get; }
}
