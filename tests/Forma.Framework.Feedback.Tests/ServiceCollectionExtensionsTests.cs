namespace Forma.Framework.Feedback.Tests;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_toast_and_confirm_dialog_services_scoped()
    {
        // Act
        var services = new ServiceCollection().AddFormaFeedbackServices();

        // Assert
        Assert.Multiple(
            () => Assert.Contains(services, descriptor
                => descriptor.ServiceType == typeof(IToastService)
                    && descriptor.ImplementationType == typeof(ToastService)
                    && descriptor.Lifetime == ServiceLifetime.Scoped),
            () => Assert.Contains(services, descriptor
                => descriptor.ServiceType == typeof(IConfirmDialogService)
                    && descriptor.ImplementationType == typeof(ConfirmDialogService)
                    && descriptor.Lifetime == ServiceLifetime.Scoped));
    }

    [Fact]
    public void Should_leave_a_service_the_host_registered_first()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IToastService, ToastService>();

        // Act
        services.AddFormaFeedbackServices();

        // Assert
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IToastService));
    }

    [Fact]
    public void Should_register_an_empty_status_badge_catalog()
    {
        // Act
        var services = new ServiceCollection().AddFormaFeedbackServices();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IStatusBadgeCatalog));
        var catalog = Assert.IsType<StatusBadgeCatalog>(descriptor.ImplementationInstance);
        Assert.Empty(catalog.Statuses);
    }

    [Fact]
    public void Should_leave_a_status_badge_catalog_the_host_registered_first()
    {
        // Arrange
        var catalog = new StatusBadgeCatalog();
        var services = new ServiceCollection();
        services.AddSingleton<IStatusBadgeCatalog>(catalog);

        // Act
        services.AddFormaFeedbackServices();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IStatusBadgeCatalog));
        Assert.Same(catalog, descriptor.ImplementationInstance);
    }
}
