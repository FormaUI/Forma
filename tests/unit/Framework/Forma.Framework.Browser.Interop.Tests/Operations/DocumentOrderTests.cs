namespace Forma.Framework.Browser.Interop.Tests.Operations;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using Xunit;

public sealed class DocumentOrderTests
{
    private static readonly ElementReference[] Items = [new("first"), new("second"), new("third")];

    private static readonly int[] Places = [2, 0, 1];

    // A browser importing the module, under an owner logging through the factory.
    private static JSEnvironment CreateEnvironment(RecordingJSObjectReference module, FakeLoggerFactory loggerFactory)
        => new(new RecordingJSRuntime((identifier, _) => identifier == "import" ? module : null), loggerFactory.CreateLogger("Owner"), Guid.CreateVersion7());

    public sealed class Sorting
    {
        [Fact]
        public async Task Should_read_the_places_the_browser_answers()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var module = new RecordingJSObjectReference((_, _) => InlineTransfer.Of(Places));
            await using var order = new DocumentOrder(CreateEnvironment(module, loggerFactory));

            // Act
            var places = await order.SortAsync(Items);

            // Assert
            Assert.NotNull(places);
            Assert.Equal(Places, places);
            Assert.Empty(loggerFactory.Entries);
        }

        [Theory]
        [InlineData("[0, 1]")]
        [InlineData("[0, 1, 1]")]
        [InlineData("[0, 1, 3]")]
        [InlineData("[0, -1, 2]")]
        [InlineData("[0, 1, 2147483648]")]
        [InlineData("[0, 1, \"2\"]")]
        [InlineData("{ \"places\": [0, 1, 2] }")]
        public async Task Should_refuse_an_order_that_does_not_place_each_element_once_and_log_it_at_debug(string json)
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var module = new RecordingJSObjectReference((_, _) => new JSTransfer { Value = InteropAnswer.Parse(json) });
            await using var order = new DocumentOrder(CreateEnvironment(module, loggerFactory));

            // Act
            var places = await order.SortAsync(Items);

            // Assert
            Assert.Null(places);
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Equal(LogLevel.Debug, entry.Level),
                () => Assert.Contains("refused", entry.Message, StringComparison.Ordinal));
        }
    }
}
