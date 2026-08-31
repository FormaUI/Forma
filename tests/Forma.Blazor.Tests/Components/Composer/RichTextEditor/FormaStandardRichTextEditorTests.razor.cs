namespace Forma.Blazor.Tests.Components.Composer.RichTextEditor;

using Forma.Framework.Localization;

public sealed partial class FormaStandardRichTextEditorTests
{
    public FormaStandardRichTextEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
