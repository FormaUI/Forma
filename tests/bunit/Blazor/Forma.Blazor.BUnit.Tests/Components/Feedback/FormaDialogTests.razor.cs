namespace Forma.Blazor.BUnit.Tests.Components.Feedback;

public sealed partial class FormaDialogTests
{
    public FormaDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Router = new InputRouterProbe(this);
        Dialog = JSInterop.SetupModule("./_content/forma/browser/scripts/components/Dialog.js").SetupModule("attach", _ => true);
    }

    /// <summary>The input router's stand-in, which records the dialog's registrations and delivers dismissals.</summary>
    internal InputRouterProbe Router { get; }

    /// <summary>The attached dialog script instance, whose open and close calls are recorded.</summary>
    internal BunitJSModuleInterop Dialog { get; }
}
