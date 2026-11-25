namespace Forma.Tests.Interop;

using System.Text.Json;

/// <summary>What a fake script answers a call, as .NET reads it.</summary>
public static class InteropAnswer
{
    /// <summary>The JSON the browser answers <paramref name="value"/> as.</summary>
    public static JsonElement Of<TValue>(TValue value)
        => JsonSerializer.SerializeToElement(value, JsonSerializerOptions.Web);

    /// <summary>The JSON of <paramref name="json"/>, which can hold what no value serializes to, such as a number past what a double holds.</summary>
    public static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    /// <summary><paramref name="answer"/> as a call reading a <typeparamref name="TValue"/> takes it: its JSON where the call reads JSON, as the browser sends it, and the answer itself otherwise.</summary>
    public static TValue As<TValue>(object? answer)
        => typeof(TValue) == typeof(JsonElement) && answer is not JsonElement ? (TValue)(object)Of(answer) : (TValue)answer!;
}
