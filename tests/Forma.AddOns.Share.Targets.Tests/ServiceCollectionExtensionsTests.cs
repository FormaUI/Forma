namespace Forma.AddOns.Share.Targets.Tests;

using Forma.Framework.Share;
using Forma.Framework.Share.Targets;
using Forma.Tests.Share;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    public sealed class AccountTargets
    {
        [Fact]
        public void Should_register_an_account_target_in_the_users_scope()
        {
            // Act
            var services = new ServiceCollection().AddFormaDropboxSharing();

            // Assert
            var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IShareTarget));
            Assert.Multiple(
                () => Assert.Equal(typeof(DropboxShare), descriptor.ImplementationType),
                () => Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));
        }

        [Fact]
        public void Should_build_a_federated_target_over_the_hosts_server_source()
        {
            // Arrange
            var services = new ServiceCollection().AddFormaMastodonSharing();
            services.AddScoped<IShareServerSource, FakeShareServerSource>();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            // Act
            var target = scope.ServiceProvider.GetRequiredService<IShareTarget>();

            // Assert
            Assert.IsType<MastodonShare>(target);
        }

        [Fact]
        public void Should_build_an_account_target_over_the_hosts_token_source()
        {
            // Arrange
            var services = new ServiceCollection().AddFormaOneDriveSharing();
            services.AddScoped<IShareTokenSource, FakeShareTokenSource>();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            // Act
            var target = scope.ServiceProvider.GetRequiredService<IShareTarget>();

            // Assert
            Assert.IsType<OneDriveShare>(target);
        }
    }

    public sealed class Discord
    {
        [Fact]
        public void Should_post_through_the_webhook_the_options_name()
        {
            // Arrange
            var webhook = new Uri("https://discord.com/api/webhooks/123/abc");
            using var provider = new ServiceCollection()
                .AddFormaDiscordSharing(options => options.WebhookAddress = webhook)
                .BuildServiceProvider();

            // Act
            var target = provider.GetRequiredService<IShareTarget>();

            // Assert
            Assert.IsType<DiscordShare>(target);
        }

        [Fact]
        public void Should_refuse_to_build_without_a_webhook()
        {
            // Arrange
            using var provider = new ServiceCollection().AddFormaDiscordSharing().BuildServiceProvider();

            // Act & Assert
            Assert.Throws<OptionsValidationException>(provider.GetRequiredService<IShareTarget>);
        }
    }

    public sealed class Messenger
    {
        [Fact]
        public void Should_open_the_dialog_under_the_app_the_options_name()
        {
            // Arrange
            using var provider = new ServiceCollection()
                .AddFormaMessengerSharing(options =>
                {
                    options.AppId = "123456";
                    options.RedirectAddress = new Uri("https://example.com/shared");
                })
                .BuildServiceProvider();
            var article = new ShareItem { Name = "Stage 12", Address = "https://example.com/news/stage-12" };

            // Act
            var address = provider.GetRequiredService<IShareTarget>().GetAddress(article);

            // Assert
            Assert.Contains("app_id=123456", address, StringComparison.Ordinal);
        }

        [Fact]
        public void Should_refuse_to_build_without_an_app_id()
        {
            // Arrange
            using var provider = new ServiceCollection()
                .AddFormaMessengerSharing(options => options.RedirectAddress = new Uri("https://example.com/shared"))
                .BuildServiceProvider();

            // Act & Assert
            Assert.Throws<OptionsValidationException>(provider.GetRequiredService<IShareTarget>);
        }
    }
}
