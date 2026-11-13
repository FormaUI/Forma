namespace Forma.Framework.Input.Tests.Keyboard;

using System.Globalization;

using Forma.Framework.Input.Keyboard;

using Microsoft.Extensions.Time.Testing;

using Xunit;

public sealed class TypeAheadTests
{
    private static readonly string[] Cities = ["Amsterdam", "Berlin", "Bern", "Bratislava", "Brussels", "Budapest", "Istanbul"];

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    public sealed class Find
    {
        [Fact]
        public void Should_move_to_the_next_label_starting_with_the_character_typed()
        {
            // Arrange
            var typeAhead = new TypeAhead(new FakeTimeProvider());

            // Act
            var found = typeAhead.Find("b", Cities, current: 0, English);

            // Assert
            Assert.Equal(1, found);
        }

        [Fact]
        public void Should_build_a_prefix_from_the_characters_typed_within_the_pause()
        {
            // Arrange
            var clock = new FakeTimeProvider();
            var typeAhead = new TypeAhead(clock);

            // Act
            typeAhead.Find("b", Cities, current: 0, English);
            clock.Advance(TypeAhead.Pause / 2);
            typeAhead.Find("r", Cities, current: 1, English);
            clock.Advance(TypeAhead.Pause / 2);
            var found = typeAhead.Find("u", Cities, current: 3, English);

            // Assert
            Assert.Equal(4, found);
        }

        [Fact]
        public void Should_keep_the_label_the_reader_is_on_while_a_longer_prefix_still_names_it()
        {
            // Arrange
            var typeAhead = new TypeAhead(new FakeTimeProvider());
            var current = typeAhead.Find("b", Cities, current: 0, English)!.Value;

            // Act
            var found = typeAhead.Find("e", Cities, current, English);

            // Assert
            Assert.Equal(1, found);
        }

        [Fact]
        public void Should_start_again_once_the_pause_has_passed()
        {
            // Arrange
            var clock = new FakeTimeProvider();
            var typeAhead = new TypeAhead(clock);
            typeAhead.Find("b", Cities, current: 0, English);

            // Act
            clock.Advance(TypeAhead.Pause + TimeSpan.FromMilliseconds(1));
            var found = typeAhead.Find("i", Cities, current: 1, English);

            // Assert
            Assert.Equal(6, found);
        }

        [Fact]
        public void Should_cycle_through_the_labels_a_character_typed_again_starts()
        {
            // Arrange
            var typeAhead = new TypeAhead(new FakeTimeProvider());
            var first = typeAhead.Find("b", Cities, current: 0, English)!.Value;

            // Act
            var second = typeAhead.Find("b", Cities, first, English)!.Value;
            var third = typeAhead.Find("b", Cities, second, English)!.Value;

            // Assert
            Assert.Equal([1, 2, 3], [first, second, third]);
        }

        [Fact]
        public void Should_wrap_round_to_the_first_label_that_matches()
        {
            // Arrange
            var typeAhead = new TypeAhead(new FakeTimeProvider());

            // Act
            var found = typeAhead.Find("a", Cities, current: 5, English);

            // Assert
            Assert.Equal(0, found);
        }

        [Fact]
        public void Should_skip_a_label_that_cannot_be_moved_to()
        {
            // Arrange
            var typeAhead = new TypeAhead(new FakeTimeProvider());

            // Act
            var found = typeAhead.Find("b", Cities, current: 0, English, canTake: index => index != 1);

            // Assert
            Assert.Equal(2, found);
        }

        [Fact]
        public void Should_compare_the_labels_in_the_culture_given()
        {
            // Arrange — Turkish pairs a dotless lowercase letter with the plain capital
            var typeAhead = new TypeAhead(new FakeTimeProvider());

            // Act
            var found = typeAhead.Find("ı", ["Istanbul"], current: -1, CultureInfo.GetCultureInfo("tr-TR"));

            // Assert
            Assert.Equal(0, found);
        }

        [Fact]
        public void Should_find_nothing_where_no_label_starts_with_what_was_typed()
        {
            // Arrange
            var typeAhead = new TypeAhead(new FakeTimeProvider());

            // Act
            var found = typeAhead.Find("z", Cities, current: 0, English);

            // Assert
            Assert.Null(found);
        }
    }
}
