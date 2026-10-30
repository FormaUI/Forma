namespace Forma.Framework.Culture.Browser.Interop;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>The routes a page publishes, written through generated code rather than reflection.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(IReadOnlyList<CultureRouteMapEntry>))]
internal sealed partial class CultureRouteMapJsonContext : JsonSerializerContext;
