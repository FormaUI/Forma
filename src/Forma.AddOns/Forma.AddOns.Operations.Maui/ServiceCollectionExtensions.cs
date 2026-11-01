namespace Forma.Framework.Browser.Interop.Operations.Maui;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

/// <summary>Registers the native operations a MAUI application performs for its web views.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="MauiOperations"/> as the application's <see cref="INativeOperations"/>, a singleton, only where nothing is registered for it yet.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaMauiOperations(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<INativeOperations>(_ => new MauiOperations(Share.Default, FileSystem.Current, DeviceInfo.Current));

        return services;
    }
}
