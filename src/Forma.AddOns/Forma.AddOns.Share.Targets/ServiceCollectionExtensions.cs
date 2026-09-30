namespace Forma.Framework.Share.Targets;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>Registers the share targets this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the social networks as share targets: Facebook, X, WhatsApp, Telegram, Bluesky, Reddit, Snapchat, LinkedIn and Pinterest.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaShareNetworks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(
        [
            ServiceDescriptor.Singleton<IShareTarget, FacebookShare>(),
            ServiceDescriptor.Singleton<IShareTarget, XShare>(),
            ServiceDescriptor.Singleton<IShareTarget, WhatsAppShare>(),
            ServiceDescriptor.Singleton<IShareTarget, TelegramShare>(),
            ServiceDescriptor.Singleton<IShareTarget, BlueskyShare>(),
            ServiceDescriptor.Singleton<IShareTarget, RedditShare>(),
            ServiceDescriptor.Singleton<IShareTarget, SnapchatShare>(),
            ServiceDescriptor.Singleton<IShareTarget, LinkedInShare>(),
            ServiceDescriptor.Singleton<IShareTarget, PinterestShare>(),
        ]);

        return services;
    }

    /// <summary>Registers Messenger's send dialog as a share target, under the Facebook app the options name.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="configure">Configures the Messenger share options.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaMessengerSharing(this IServiceCollection services, Action<MessengerShareOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.AddOptions<MessengerShareOptions>()
            .Validate(options => !string.IsNullOrWhiteSpace(options.AppId), "Messenger sharing needs the host's Facebook app id.")
            .Validate(options => options.RedirectAddress is not null, "Messenger sharing needs the address the dialog returns to.")
            .ValidateOnStart();

        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.AddSingleton<IShareTarget>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<MessengerShareOptions>>().Value;

            return new MessengerShare(settings.AppId!, settings.RedirectAddress!);
        });

        return services;
    }

    /// <summary>Registers a Discord channel as a share target, posting through the webhook address the options name.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="configure">Configures the Discord share options.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaDiscordSharing(this IServiceCollection services, Action<DiscordShareOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.AddOptions<DiscordShareOptions>()
            .Validate(options => options.WebhookAddress is not null, "Discord sharing needs the channel's webhook address.")
            .ValidateOnStart();

        if (configure is not null)
        {
            options.Configure(configure);
        }

        // Built by the container so the container also disposes it.
        services.AddSingleton<IShareTarget>(provider =>
            new DiscordShare(provider.GetRequiredService<IOptions<DiscordShareOptions>>().Value.WebhookAddress!));

        return services;
    }

    /// <summary>Registers a Discord channel as a share target, posting through the webhook address.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="webhookAddress">The Discord channel webhook's address.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaDiscordSharing(this IServiceCollection services, Uri webhookAddress)
    {
        ArgumentNullException.ThrowIfNull(webhookAddress);

        return services.AddFormaDiscordSharing(options => options.WebhookAddress = webhookAddress);
    }

    /// <summary>Registers Mastodon as a share target, opening the composer on the server the user named.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaMastodonSharing(this IServiceCollection services)
        => services.AddAccountSharing<MastodonShare>();

    /// <summary>Registers Dropbox as a share target, saving into the account the user connected.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaDropboxSharing(this IServiceCollection services)
        => services.AddAccountSharing<DropboxShare>();

    /// <summary>Registers Google Drive as a share target, uploading into the account the user connected.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaGoogleDriveSharing(this IServiceCollection services)
        => services.AddAccountSharing<GoogleDriveShare>();

    /// <summary>Registers OneDrive as a share target, uploading into the account the user connected.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaOneDriveSharing(this IServiceCollection services)
        => services.AddAccountSharing<OneDriveShare>();

    /// <summary>Registers TikTok as a share target, publishing on the account the user connected.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaTikTokSharing(this IServiceCollection services)
        => services.AddAccountSharing<TikTokShare>();

    private static IServiceCollection AddAccountSharing<TTarget>(this IServiceCollection services)
        where TTarget : class, IShareTarget
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IShareTarget, TTarget>();

        return services;
    }
}
