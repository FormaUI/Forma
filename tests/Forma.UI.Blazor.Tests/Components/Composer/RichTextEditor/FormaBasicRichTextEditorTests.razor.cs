namespace Forma.UI.Blazor.Tests.Components.Composer.RichTextEditor;

using Forma.Framework.Localization;

public sealed partial class FormaBasicRichTextEditorTests
{
    public FormaBasicRichTextEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddJsonLocalization();
    }
}
