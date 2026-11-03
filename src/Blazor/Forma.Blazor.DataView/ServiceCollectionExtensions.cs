namespace Forma.Blazor.Components.DataView;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers what every data view in the application reads.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the query executor a data view counts and reads a queryable through.</summary>
    /// <param name="services">The application's services.</param>
    public static IServiceCollection AddFormaDataView(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDataViewQueryExecutor>(InMemoryQueryExecutor.Instance);

        return services;
    }
}
