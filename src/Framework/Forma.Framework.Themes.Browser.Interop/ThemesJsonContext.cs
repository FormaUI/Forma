namespace Forma.Framework.Themes.Browser.Interop;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>What the page's theme coordinator answers, read through generated code rather than reflection.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(HostPageThemes))]
internal sealed partial class ThemesJsonContext : JsonSerializerContext;
