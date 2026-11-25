namespace Forma.Framework.Browser.Interop.Tests.Operations;

using Forma.Framework.Browser;
using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using Xunit;

public sealed class TextEntryTests
{
    private static readonly ElementReference Input = new("input");

    // A browser importing the module, under an owner logging through the factory.
    private static JSEnvironment CreateEnvironment(RecordingJSObjectReference module, FakeLoggerFactory loggerFactory)
        => new(new RecordingJSRuntime((identifier, _) => identifier == "import" ? module : null), loggerFactory.CreateLogger("Owner"), Guid.CreateVersion7());

    public sealed class Selection
    {
        [Fact]
        public async Task Should_read_the_range_the_browser_answers()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var module = new RecordingJSObjectReference((_, _) => new TextRange { Start = 2, End = 5 });
            await using var entry = new TextEntry(CreateEnvironment(module, loggerFactory));

            // Act
            var range = await entry.ReadSelectionAsync(Input);

            // Assert
            Assert.Equal(new TextRange { Start = 2, End = 5 }, range);
            Assert.Empty(loggerFactory.Entries);
        }

        [Theory]
        [InlineData("{ \"start\": 1.5, \"end\": 2 }")]
        [InlineData("{ \"start\": \"1\", \"end\": 2 }")]
        [InlineData("{ \"start\": 1, \"end\": 1e400 }")]
        [InlineData("[1, 2]")]
        public async Task Should_refuse_a_range_that_is_not_one_and_log_it_at_debug(string json)
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var module = new RecordingJSObjectReference((_, _) => InteropAnswer.Parse(json));
            await using var entry = new TextEntry(CreateEnvironment(module, loggerFactory));

            // Act
            var range = await entry.ReadSelectionAsync(Input);

            // Assert
            Assert.Null(range);
            var logEntry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Equal(LogLevel.Debug, logEntry.Level),
                () => Assert.Contains("was refused", logEntry.Message, StringComparison.Ordinal));
        }
    }
}
