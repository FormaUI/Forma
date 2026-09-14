namespace Forma.Framework.Windows;

/// <summary>The application or feature instance a window belongs to.</summary>
/// <param name="OriginId">The stable identity of the application or feature, shared by every instance of it.</param>
/// <param name="OriginName">The display name of the application or feature.</param>
/// <param name="InstanceId">The identity of one running instance, generated once when it starts.</param>
public sealed record WindowOrigin(string OriginId, string OriginName, Guid InstanceId)
{
    /// <summary>The stable identity of the application or feature, shared by every instance of it.</summary>
    public string OriginId { get; init; } = Required(OriginId);

    /// <summary>The display name of the application or feature.</summary>
    public string OriginName { get; init; } = Required(OriginName);

    /// <summary>Creates the origin of a new instance of an application or feature.</summary>
    public static WindowOrigin NewInstance(string originId, string originName)
        => new(originId, originName, Guid.NewGuid());

    private static string Required(string value, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);

        return value;
    }
}
