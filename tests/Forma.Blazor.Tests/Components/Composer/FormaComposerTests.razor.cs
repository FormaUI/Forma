namespace Forma.Blazor.Tests.Components.Composer;

using Forma.Framework.Localization;

public sealed partial class FormaComposerTests
{
    public FormaComposerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
        Router = JSInterop.SetupModule("./_content/forma/input/scripts/InputRouter.js");
    }

    /// <summary>The input router module, set up before any render so the composer's registrations on it are recorded.</summary>
    internal BunitJSModuleInterop Router { get; }
}
