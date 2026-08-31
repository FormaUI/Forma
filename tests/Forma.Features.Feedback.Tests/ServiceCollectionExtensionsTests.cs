namespace Forma.Features.Feedback.Tests;

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
    public void Should_register_no_status_badge_catalog()
    {
        // Act
        var services = new ServiceCollection().AddFormaFeedbackServices();

        // Assert
        // The statuses are an application's to define, so an empty default would only hide a missing registration.
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IStatusBadgeCatalog));
    }
}
