namespace Forma.Tests.Interop;

using System.Text.Json;

using Forma.Framework.JavaScript.Interop;

/// <summary>Transfers carrying a value inline, as the browser sends one within the transfer's budget.</summary>
public static class InlineTransfer
{
    /// <summary>The transfer carrying <paramref name="value"/> inline.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The transfer.</returns>
    public static JSTransfer Of<TValue>(TValue value)
        => new() { Value = JsonSerializer.SerializeToElement(value, JsonSerializerOptions.Web) };
}
