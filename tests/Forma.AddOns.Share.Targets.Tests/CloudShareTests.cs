namespace Forma.AddOns.Share.Targets.Tests;

using Forma.Framework.Resources;
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

using Xunit;

public sealed class CloudShareTests
{
    private static Task<string> GetTokenAsync() => Task.FromResult("token-123");

    private static ShareItem CreateFile(string source = "https://example.com/files/kit.zip")
        => new() { Kind = ShareItemKind.File, Name = "kit.zip", Resource = new ResourceInfo(source) };

    public sealed class Dropbox
    {
        [Fact]
        public async Task Should_ask_the_api_to_save_the_resources_address()
        {
            // Arrange
            using var handler = new RecordingHandler();
            using var http = new HttpClient(handler);
            using var target = new DropboxShare(GetTokenAsync, http);

            // Act
            await target.ShareAsync(CreateFile());

            // Assert
            Assert.Multiple(
                () => Assert.Equal("https://api.dropboxapi.com/2/files/save_url", handler.Request!.RequestUri!.ToString()),
                () => Assert.Equal("Bearer token-123", handler.Request!.Headers.Authorization!.ToString()),
                () => Assert.Contains("\"/kit.zip\"", handler.Body, StringComparison.Ordinal),
                () => Assert.Contains("https://example.com/files/kit.zip", handler.Body, StringComparison.Ordinal));
        }
    }

    public sealed class GoogleDrive
    {
        [Fact]
        public async Task Should_fetch_the_resource_and_upload_it_under_its_name()
        {
            // Arrange
            using var handler = new RecordingHandler();
            handler.Downloads["https://example.com/files/kit.zip"] = [1, 2, 3];
            using var http = new HttpClient(handler);
            using var target = new GoogleDriveShare(GetTokenAsync, http);

            // Act
            await target.ShareAsync(CreateFile());

            // Assert
            Assert.Multiple(
                () => Assert.StartsWith("https://www.googleapis.com/upload/drive/v3/files", handler.Request!.RequestUri!.ToString(), StringComparison.Ordinal),
                () => Assert.Equal("Bearer token-123", handler.Request!.Headers.Authorization!.ToString()),
                () => Assert.Contains("kit.zip", handler.Body, StringComparison.Ordinal));
        }
    }

    public sealed class OneDrive
    {
        [Fact]
        public async Task Should_put_the_resource_under_its_name_through_graph()
        {
            // Arrange
            using var handler = new RecordingHandler();
            handler.Downloads["https://example.com/files/kit.zip"] = [1, 2, 3];
            using var http = new HttpClient(handler);
            using var target = new OneDriveShare(GetTokenAsync, http);

            // Act
            await target.ShareAsync(CreateFile());

            // Assert
            Assert.Multiple(
                () => Assert.Equal(HttpMethod.Put, handler.Request!.Method),
                () => Assert.Contains("graph.microsoft.com/v1.0/me/drive/root:/kit.zip:/content", handler.Request!.RequestUri!.ToString(), StringComparison.Ordinal),
                () => Assert.Equal("Bearer token-123", handler.Request!.Headers.Authorization!.ToString()));
        }
    }

    public sealed class TikTok
    {
        [Fact]
        public async Task Should_start_a_publish_that_pulls_the_video_from_its_address()
        {
            // Arrange
            using var handler = new RecordingHandler();
            using var http = new HttpClient(handler);
            using var target = new TikTokShare(GetTokenAsync, http);
            var clip = new ShareItem { Kind = ShareItemKind.Image, Name = "Finish line", Resource = new ResourceInfo("https://example.com/clips/finish.mp4") };

            // Act
            await target.ShareAsync(clip);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("https://open.tiktokapis.com/v2/post/publish/video/init/", handler.Request!.RequestUri!.ToString()),
                () => Assert.Contains("PULL_FROM_URL", handler.Body, StringComparison.Ordinal),
                () => Assert.Contains("https://example.com/clips/finish.mp4", handler.Body, StringComparison.Ordinal));
        }
    }

    public sealed class EveryTarget
    {
        [Fact]
        public void Should_refuse_an_item_with_nothing_to_fetch()
        {
            // Arrange
            var inMemory = new ShareItem { Kind = ShareItemKind.File, Name = "notes.txt", Resource = new ResourceInfo("data:text/plain,notes") };
            using var dropbox = new DropboxShare(GetTokenAsync);
            using var drive = new GoogleDriveShare(GetTokenAsync);
            using var onedrive = new OneDriveShare(GetTokenAsync);
            using var tiktok = new TikTokShare(GetTokenAsync);

            // Assert
            Assert.Multiple(
                () => Assert.False(dropbox.Supports(inMemory)),
                () => Assert.False(drive.Supports(inMemory)),
                () => Assert.False(onedrive.Supports(inMemory)),
                () => Assert.False(tiktok.Supports(inMemory)));
        }
    }
}
