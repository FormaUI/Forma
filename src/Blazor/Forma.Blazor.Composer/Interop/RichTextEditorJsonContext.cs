namespace Forma.Blazor.Components.Composer.Interop;

using System.Text.Json;
using System.Text.Json.Serialization;

using Forma.Blazor.Components.Composer.Dialogs;

/// <summary>What the rich text editor's canvas sends through the transfer, read with the web's defaults through generated code rather than reflection.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(RichTextLinkTargetDto))]
[JsonSerializable(typeof(ImageDialogResult))]
[JsonSerializable(typeof(VideoDialogResult))]
[JsonSerializable(typeof(EmbedDialogResult))]
internal sealed partial class RichTextEditorJsonContext : JsonSerializerContext;
