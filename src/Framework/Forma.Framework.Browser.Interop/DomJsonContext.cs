namespace Forma.Framework.Browser.Interop;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>What the browser sends the browser interop: the states it measures for an observation and what its scripts answer, read with the web's defaults through generated code rather than reflection.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(AutofillState))]
[JsonSerializable(typeof(FocusState))]
[JsonSerializable(typeof(IntersectionState))]
[JsonSerializable(typeof(ScrollState))]
[JsonSerializable(typeof(SizeState))]
[JsonSerializable(typeof(TextSelection))]
[JsonSerializable(typeof(TextRange))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class DomJsonContext : JsonSerializerContext;
