namespace Forma.Framework.Browser.Interop.Tests.Operations;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using Xunit;

public sealed class ElementLabelsTests
{
    private static readonly ElementReference[] Items = [new("open"), new("save"), new("close")];

    // A browser importing the module, under an owner logging through the factory.
    private static JSEnvironment CreateEnvironment(RecordingJSObjectReference module, FakeLoggerFactory loggerFactory)
        => new(new RecordingJSRuntime((identifier, _) => identifier == "import" ? module : null), loggerFactory.CreateLogger("Owner"), Guid.CreateVersion7());

    public sealed class Reading
    {
        [Fact]
        public async Task Should_read_the_labels_the_browser_answers_and_a_missing_one_as_empty()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var module = new RecordingJSObjectReference((_, _) => new JSTransfer { Value = InteropAnswer.Parse("[\"Open\", null, \"Close\"]") });
            await using var labels = new ElementLabels(CreateEnvironment(module, loggerFactory));

            // Act
            var read = await labels.ReadAsync(Items);

            // Assert
            Assert.NotNull(read);
            Assert.Equal(["Open", string.Empty, "Close"], read);
            Assert.Empty(loggerFactory.Entries);
        }

        [Theory]
        [InlineData("[\"Open\", \"Close\"]")]
        [InlineData("[\"Open\", 4, \"Close\"]")]
        [InlineData("\"Open\"")]
        public async Task Should_refuse_labels_that_do_not_label_each_element_and_log_it_at_debug(string json)
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var module = new RecordingJSObjectReference((_, _) => new JSTransfer { Value = InteropAnswer.Parse(json) });
            await using var labels = new ElementLabels(CreateEnvironment(module, loggerFactory));

            // Act
            var read = await labels.ReadAsync(Items);

            // Assert
            Assert.Null(read);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Equal(LogLevel.Debug, entry.Level),
                () => Assert.Contains("refused", entry.Message, StringComparison.Ordinal));
        }
    }
}
