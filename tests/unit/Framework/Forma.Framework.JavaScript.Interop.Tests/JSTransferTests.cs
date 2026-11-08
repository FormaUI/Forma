namespace Forma.Framework.JavaScript.Interop.Tests;

using System.Text;
using System.Text.Json;

using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Interop;

using Xunit;

public sealed class JSTransferTests
{
    private const long Accepted = 1024;

    private static JSTransfer CreateInline(string json)
        => JsonSerializer.Deserialize<JSTransfer>($$"""{ "value": {{json}} }""")!;

    private static JSTransfer CreateStreamed(FakeJSStreamReference stream, bool isText)
        => new() { Stream = stream, IsText = isText };

    public sealed class Text
    {
        [Fact]
        public async Task Should_read_a_text_carried_inline()
        {
            // Arrange
            var transfer = CreateInline("\"Héllo\"");

            // Act
            var text = await transfer.ReadTextAsync(Accepted, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("Héllo", text);
        }

        [Fact]
        public async Task Should_read_nothing_sent_as_null()
        {
            // Arrange
            var transfer = CreateInline("null");

            // Act
            var text = await transfer.ReadTextAsync(Accepted, TestContext.Current.CancellationToken);

            // Assert
            Assert.Null(text);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Should_read_a_value_other_than_a_text_as_null(bool isStreamed)
        {
            // Arrange
            const string Json = """{ "tokens": [4, 5] }""";
            await using var stream = new FakeJSStreamReference(Encoding.UTF8.GetBytes(Json));
            var transfer = isStreamed ? CreateStreamed(stream, isText: false) : CreateInline(Json);

            // Act
            var text = await transfer.ReadTextAsync(Accepted, TestContext.Current.CancellationToken);

            // Assert
            Assert.Null(text);
        }

        [Fact]
        public async Task Should_read_a_text_streamed_as_its_own_bytes()
        {
            // Arrange
            await using var stream = new FakeJSStreamReference(Encoding.UTF8.GetBytes("中文 \"quoted\" \\ text"));
            var transfer = CreateStreamed(stream, isText: true);

            // Act
            var text = await transfer.ReadTextAsync(Accepted, TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("中文 \"quoted\" \\ text", text),
                () => Assert.Equal(Accepted, stream.OpenedWith),
                () => Assert.True(stream.IsDisposed));
        }

        [Fact]
        public async Task Should_refuse_an_inline_text_longer_than_its_receiver_accepts()
        {
            // Arrange
            var transfer = CreateInline($"\"{new string('x', (int)Accepted + 1)}\"");

            // Act
            var failure = await Assert.ThrowsAsync<JSTransferException>(async () => await transfer.ReadTextAsync(Accepted, TestContext.Current.CancellationToken));

            // Assert
            Assert.True(failure.IsRefused);
        }

        [Fact]
        public async Task Should_refuse_a_stream_longer_than_its_receiver_accepts_before_reading_it()
        {
            // Arrange
            await using var stream = new FakeJSStreamReference(new byte[Accepted + 1]);
            var transfer = CreateStreamed(stream, isText: true);

            // Act
            var failure = await Assert.ThrowsAsync<JSTransferException>(async () => await transfer.ReadTextAsync(Accepted, TestContext.Current.CancellationToken));

            // Assert
            Assert.Multiple(
                () => Assert.True(failure.IsRefused),
                () => Assert.Null(stream.OpenedWith),
                () => Assert.True(stream.IsDisposed));
        }

        [Fact]
        public async Task Should_report_a_stream_that_breaks_on_its_way()
        {
            // Arrange
            await using var stream = new FakeJSStreamReference(new byte[10], new EndOfStreamException("The stream ended short."));
            var transfer = CreateStreamed(stream, isText: true);

            // Act
            var failure = await Assert.ThrowsAsync<JSTransferException>(async () => await transfer.ReadTextAsync(Accepted, TestContext.Current.CancellationToken));

            // Assert
            Assert.Multiple(
                () => Assert.False(failure.IsRefused),
                () => Assert.IsType<EndOfStreamException>(failure.InnerException),
                () => Assert.True(stream.IsDisposed));
        }

        [Fact]
        public async Task Should_reject_a_negative_accepted_size()
        {
            // Arrange
            var transfer = CreateInline("\"x\"");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await transfer.ReadTextAsync(-1, TestContext.Current.CancellationToken));
        }
    }

    public sealed class Discarding
    {
        [Fact]
        public async Task Should_release_a_streamed_value_unread()
        {
            // Arrange
            await using var stream = new FakeJSStreamReference("Kept"u8.ToArray());
            var transfer = CreateStreamed(stream, isText: true);

            // Act
            await transfer.DiscardAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Null(stream.OpenedWith),
                () => Assert.True(stream.IsDisposed));
        }

        [Fact]
        public async Task Should_leave_an_inline_value_with_nothing_to_release()
        {
            // Arrange
            var transfer = CreateInline("\"Kept\"");

            // Act
            await transfer.DiscardAsync();

            // Assert
            Assert.Equal("Kept", await transfer.ReadTextAsync(Accepted, TestContext.Current.CancellationToken));
        }
    }

    public sealed class Json
    {
        [Fact]
        public async Task Should_read_a_value_carried_inline()
        {
            // Arrange
            var transfer = CreateInline("""{ "tokens": [1, 2, 3] }""");

            // Act
            var value = await transfer.ReadJsonAsync(Accepted, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal([1, 2, 3], value.GetProperty("tokens").EnumerateArray().Select(token => token.GetInt32()));
        }

        [Fact]
        public async Task Should_read_a_value_streamed_as_its_json()
        {
            // Arrange
            await using var stream = new FakeJSStreamReference(Encoding.UTF8.GetBytes("""{ "tokens": [4, 5] }"""));
            var transfer = CreateStreamed(stream, isText: false);

            // Act
            var value = await transfer.ReadJsonAsync(Accepted, TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.Equal([4, 5], value.GetProperty("tokens").EnumerateArray().Select(token => token.GetInt32())),
                () => Assert.True(stream.IsDisposed));
        }

        [Fact]
        public async Task Should_read_a_streamed_text_as_a_json_string()
        {
            // Arrange
            await using var stream = new FakeJSStreamReference(Encoding.UTF8.GetBytes("a \"quoted\" text"));
            var transfer = CreateStreamed(stream, isText: true);

            // Act
            var value = await transfer.ReadJsonAsync(Accepted, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("a \"quoted\" text", value.GetString());
        }

        [Fact]
        public async Task Should_report_a_stream_whose_json_does_not_parse()
        {
            // Arrange
            await using var stream = new FakeJSStreamReference(Encoding.UTF8.GetBytes("{ \"tokens\": [4, "));
            var transfer = CreateStreamed(stream, isText: false);

            // Act
            var failure = await Assert.ThrowsAsync<JSTransferException>(async () => await transfer.ReadJsonAsync(Accepted, TestContext.Current.CancellationToken));

            // Assert
            Assert.False(failure.IsRefused);
        }

        [Fact]
        public async Task Should_refuse_an_inline_value_larger_than_its_receiver_accepts()
        {
            // Arrange
            var transfer = CreateInline($"[{string.Join(',', Enumerable.Repeat(1, (int)Accepted))}]");

            // Act
            var failure = await Assert.ThrowsAsync<JSTransferException>(async () => await transfer.ReadJsonAsync(Accepted, TestContext.Current.CancellationToken));

            // Assert
            Assert.True(failure.IsRefused);
        }
    }
}
