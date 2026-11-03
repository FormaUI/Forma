namespace Forma.Framework.Browser.Interop;

using System.Text.Json;
using System.Text.Json.Serialization;

using Forma.Framework.Browser.Interop.Focus;
using Forma.Framework.Browser.Interop.Scrolling;

/// <summary>The states the browser measures for an observation, read with the web's defaults through generated code rather than reflection.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(FocusState))]
[JsonSerializable(typeof(IntersectionState))]
[JsonSerializable(typeof(ScrollState))]
internal sealed partial class DomJsonContext : JsonSerializerContext;
