namespace Forma.UI.Features.Share;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the share targets this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the social networks a share menu offers a link or a picture: Facebook, X, WhatsApp, Telegram, Bluesky, Reddit, Snapchat, and Pinterest for pictures.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
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
            ServiceDescriptor.Singleton<IShareTarget, PinterestShare>(),
        ]);

        return services;
    }

    /// <summary>Registers a Discord channel as a share target, posting through the webhook address the host configured.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="webhookAddress">The channel webhook's address, from Discord's channel settings.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaDiscordSharing(this IServiceCollection services, Uri webhookAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(webhookAddress);

        // Built by the container so the container also disposes it.
        services.AddSingleton<IShareTarget>(_ => new DiscordWebhookShare(webhookAddress));

        return services;
    }

    /// <summary>Registers Dropbox as a share target, saving into the account the token accessor answers for.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="accessToken">Answers a current access token for the account files are saved into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaDropboxSharing(this IServiceCollection services, Func<Task<string>> accessToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(accessToken);

        services.AddSingleton<IShareTarget>(_ => new DropboxShare(accessToken));

        return services;
    }

    /// <summary>Registers Google Drive as a share target, uploading into the account the token accessor answers for.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="accessToken">Answers a current access token for the account files are uploaded into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaGoogleDriveSharing(this IServiceCollection services, Func<Task<string>> accessToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(accessToken);

        services.AddSingleton<IShareTarget>(_ => new GoogleDriveShare(accessToken));

        return services;
    }

    /// <summary>Registers OneDrive as a share target, uploading into the account the token accessor answers for.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="accessToken">Answers a current access token for the account files are uploaded into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaOneDriveSharing(this IServiceCollection services, Func<Task<string>> accessToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(accessToken);

        services.AddSingleton<IShareTarget>(_ => new OneDriveShare(accessToken));

        return services;
    }

    /// <summary>Registers TikTok as a share target, publishing on the account the token accessor answers for.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="accessToken">Answers a current access token for the account videos are published on.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaTikTokSharing(this IServiceCollection services, Func<Task<string>> accessToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(accessToken);

        services.AddSingleton<IShareTarget>(_ => new TikTokShare(accessToken));

        return services;
    }
}
