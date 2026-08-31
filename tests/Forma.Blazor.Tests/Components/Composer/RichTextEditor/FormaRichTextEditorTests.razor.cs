namespace Forma.Blazor.Tests.Components.Composer.RichTextEditor;

using Forma.Framework.Localization;

public sealed partial class FormaRichTextEditorTests
{
    public FormaRichTextEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
