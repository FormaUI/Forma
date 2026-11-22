namespace Forma.Blazor.Components;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Culture;
using Forma.Framework.Input.Interop;
using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>Makes a user scope's component contexts from the services its host registers, each with the library's default where the host registers none.</summary>
/// <param name="serviceProvider">The user scope's services, which hold its culture state and the logger factory.</param>
public sealed class ComponentContextFactory(IServiceProvider serviceProvider) : IComponentContextFactory
{
    private readonly ICultureState _cultureState = serviceProvider.GetRequiredService<ICultureState>();
    private readonly ILoggerFactory _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

    /// <inheritdoc />
    public ComponentContext Create(Type componentType, Func<Type> localeSourceType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(localeSourceType);

        serviceProvider.GetService<ICultureScopeInitializer>()?.Initialize();

        return new ComponentContext(
            _cultureState,
            serviceProvider.GetJSRuntime,
            () => serviceProvider.GetStringLocaleFactory().Create(localeSourceType(), _cultureState),
            () => _loggerFactory.CreateLogger(componentType),
            () => serviceProvider.GetService<DomBridge>(),
            serviceProvider.GetJSTransferOptions,
            () => serviceProvider.GetService<InputRouter>());
    }
}
