namespace Forma.AddOns.Share.Targets.Tests;

using Forma.Framework.Resources;
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;
using Forma.Tests.Share;

using Xunit;

public sealed class AccountShareTests
{
    private static ShareItem CreateFile()
        => new() { Kind = ShareItemKind.File, Name = "kit.zip", Resource = new ResourceInfo("https://example.com/files/kit.zip") };

    [Fact]
    public async Task Should_share_nothing_and_say_so_without_a_connected_account()
    {
        // Arrange
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var tokens = new FakeShareTokenSource();
        using var target = new DropboxShare(tokens, http);

        // Act
        var shared = await target.ShareAsync(CreateFile(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.False(shared),
            () => Assert.Null(handler.Request),
            () => Assert.Equal(["dropbox"], tokens.Requests));
    }

    [Fact]
    public async Task Should_share_under_the_users_token()
    {
        // Arrange
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var tokens = new FakeShareTokenSource { Tokens = { ["dropbox"] = "token-123" } };
        using var target = new DropboxShare(tokens, http);

        // Act
        var shared = await target.ShareAsync(CreateFile(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.True(shared),
            () => Assert.True(target.RequiresAccount),
            () => Assert.Equal("Bearer token-123", handler.Request!.Headers.Authorization!.ToString()));
    }

    [Fact]
    public async Task Should_not_ask_for_a_token_for_an_item_it_does_not_support()
    {
        // Arrange
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var tokens = new FakeShareTokenSource { Tokens = { ["dropbox"] = "token-123" } };
        using var target = new DropboxShare(tokens, http);
        var link = new ShareItem { Name = "Stage 12", Address = "https://example.com/news/stage-12" };

        // Act
        var shared = await target.ShareAsync(link, TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(
            () => Assert.False(shared),
            () => Assert.Empty(tokens.Requests),
            () => Assert.Null(handler.Request));
    }
}
