namespace Forma.Framework.Culture;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the culture services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the culture state each user scope holds and the integration that applies accepted pairs to it.</summary>
    /// <remarks>
    /// The integration is the state's own instance, so applying always reaches the state every component reads. A state registered in
    /// its place that doesn't implement <see cref="ICultureStateIntegration"/> is rejected with <see cref="InvalidOperationException"/>
    /// as a scope first resolves it, or here where the registration is an instance.
    /// </remarks>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaCultureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ICultureState>(_ => new DefaultCultureState());
        RequireIntegration(services);
        services.TryAddScoped(provider => GetIntegration(provider.GetRequiredService<ICultureState>()));

        return services;
    }

    // The state resolution picks, the last registered, checked for the integration as it is created.
    private static void RequireIntegration(IServiceCollection services)
    {
        for (var index = services.Count - 1; index >= 0; index--)
        {
            var descriptor = services[index];

            if (descriptor.ServiceType != typeof(ICultureState) || descriptor.IsKeyedService)
            {
                continue;
            }

            if (descriptor.ImplementationInstance is ICultureState instance)
            {
                _ = GetIntegration(instance);
            }
            else if (descriptor.ImplementationFactory?.Target is not CheckedCultureStateFactory)
            {
                services[index] = ServiceDescriptor.Describe(typeof(ICultureState), new CheckedCultureStateFactory(descriptor).Create, descriptor.Lifetime);
            }

            return;
        }
    }

    private static ICultureStateIntegration GetIntegration(ICultureState state)
        => state as ICultureStateIntegration
            ?? throw new InvalidOperationException(
                $"The registered {nameof(ICultureState)}, {state.GetType().Name}, doesn't implement {nameof(ICultureStateIntegration)}, through which a switch applies the pair it accepted; a state registered in place of Forma's implements both on the same instance.");

    /// <summary>Creates the state a registration describes and checks that it implements the integration.</summary>
    private sealed class CheckedCultureStateFactory(ServiceDescriptor descriptor)
    {
        public object Create(IServiceProvider provider)
        {
            var state = (ICultureState)(descriptor.ImplementationFactory is { } factory
                ? factory(provider)
                : ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!));

            _ = GetIntegration(state);

            return state;
        }
    }
}
