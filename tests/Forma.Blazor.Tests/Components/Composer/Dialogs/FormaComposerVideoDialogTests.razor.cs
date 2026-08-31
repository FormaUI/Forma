namespace Forma.Blazor.Tests.Components.Composer.Dialogs;

public sealed partial class FormaComposerVideoDialogTests
{
    public FormaComposerVideoDialogTests()
    {
        Services.AddJsonLocalization();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
