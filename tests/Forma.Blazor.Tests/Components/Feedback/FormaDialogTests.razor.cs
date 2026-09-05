namespace Forma.Blazor.Tests.Components.Feedback;

public sealed partial class FormaDialogTests
{
    public FormaDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
        Router = JSInterop.SetupModule("./_content/Forma.Framework.Input.Interop/scripts/InputRouter.js");
        Dialog = JSInterop.SetupModule("./_content/Forma.Framework.Common.Interop/scripts/components/Dialog.js").SetupModule("attach", _ => true);
    }

    /// <summary>The input router module, set up before any render so the dialog's registrations on it are recorded.</summary>
    internal BunitJSModuleInterop Router { get; }

    /// <summary>The attached dialog script instance, whose open and close calls are recorded.</summary>
    internal BunitJSModuleInterop Dialog { get; }
}
