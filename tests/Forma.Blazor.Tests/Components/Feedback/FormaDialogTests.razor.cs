namespace Forma.Blazor.Tests.Components.Feedback;

public sealed partial class FormaDialogTests
{
    public FormaDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Router = new InputRouterProbe(this);
        Dialog = JSInterop.SetupModule("./_content/forma/browser/scripts/components/Dialog.js").SetupModule("attach", _ => true);
    }

    /// <summary>The input router's stand-in, set up before any render so the dialog's registrations on it are recorded and a dismissal can be delivered to it.</summary>
    internal InputRouterProbe Router { get; }

    /// <summary>The attached dialog script instance, whose open and close calls are recorded.</summary>
    internal BunitJSModuleInterop Dialog { get; }
}
