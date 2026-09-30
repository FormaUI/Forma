namespace Forma.Blazor.Composer.Tests;

public sealed partial class FormaComposerTests
{
    public FormaComposerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Router = JSInterop.SetupModule("./_content/forma/input/scripts/InputRouter.js");
    }

    /// <summary>The input router module that records the composer's registrations.</summary>
    internal BunitJSModuleInterop Router { get; }
}
