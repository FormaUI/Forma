namespace Forma.AddOns.Share.Targets.Tests;

using System.Net;

using Forma.Framework.Resources;
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

using Xunit;

public sealed class DiscordWebhookShareTests
{
    private static readonly Uri Webhook = new("https://discord.com/api/webhooks/123/abc");

    [Fact]
    public async Task Should_post_the_items_name_and_address_to_the_webhook()
    {
        // Arrange
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        using var target = new DiscordWebhookShare(Webhook, http);
        var item = new ShareItem { Name = "Stage 12", Address = "https://example.com/news/stage-12" };

        // Act
        await target.ShareAsync(item);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(Webhook, handler.Request!.RequestUri),
            () => Assert.Contains("Stage 12", handler.Body, StringComparison.Ordinal),
            () => Assert.Contains("https://example.com/news/stage-12", handler.Body, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_fall_back_to_a_resources_public_address()
    {
        // Arrange
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        using var target = new DiscordWebhookShare(Webhook, http);
        var item = new ShareItem { Kind = ShareItemKind.File, Name = "kit.zip", Resource = new ResourceInfo("https://example.com/files/kit.zip") };

        // Act
        await target.ShareAsync(item);

        // Assert
        Assert.Contains("https://example.com/files/kit.zip", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_refuse_an_item_that_lives_nowhere_reachable()
    {
        // Arrange
        using var target = new DiscordWebhookShare(Webhook);
        var inMemory = new ShareItem { Kind = ShareItemKind.File, Name = "notes.txt", Resource = new ResourceInfo("data:text/plain,notes") };

        // Assert
        Assert.False(target.Supports(inMemory));
        Assert.True(target.Supports(new ShareItem { Address = "https://example.com/a" }));
    }

    [Fact]
    public async Task Should_surface_a_failed_post_rather_than_swallowing_it()
    {
        // Arrange
        using var handler = new RecordingHandler(HttpStatusCode.Unauthorized);
        using var http = new HttpClient(handler);
        using var target = new DiscordWebhookShare(Webhook, http);

        // Act + Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => target.ShareAsync(new ShareItem { Address = "https://example.com/a" }));
    }
}
