namespace Forma.Framework.Input.Tests.Keyboard;

using System.Globalization;

using Forma.Framework.Input.Keyboard;

using Xunit;

public sealed class TypeAheadTests
{
    private static readonly string[] Cities = ["Amsterdam", "Berlin", "Bern", "Bratislava", "Brussels", "Budapest", "Istanbul"];

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private static InputKeyInfo CreateKey(string character, double pressedAt = 0)
        => new() { Type = "keydown", Key = character, Timestamp = pressedAt };

    public sealed class Find
    {
        [Fact]
        public void Should_move_to_the_next_label_starting_with_the_character_typed()
        {
            // Arrange
            var typeAhead = new TypeAhead();

            // Act
            var found = typeAhead.Find(CreateKey("b"), Cities, current: 0, English);

            // Assert
            Assert.Equal(1, found);
        }

        [Fact]
        public void Should_build_a_prefix_from_the_characters_pressed_within_the_pause()
        {
            // Arrange
            var typeAhead = new TypeAhead();
            var pause = TypeAhead.Pause.TotalMilliseconds;

            // Act
            typeAhead.Find(CreateKey("b", 1000), Cities, current: 0, English);
            typeAhead.Find(CreateKey("r", 1000 + (pause / 2)), Cities, current: 1, English);
            var found = typeAhead.Find(CreateKey("u", 1000 + pause), Cities, current: 3, English);

            // Assert
            Assert.Equal(4, found);
        }

        [Fact]
        public void Should_keep_the_label_the_reader_is_on_while_a_longer_prefix_still_names_it()
        {
            // Arrange
            var typeAhead = new TypeAhead();
            var current = typeAhead.Find(CreateKey("b"), Cities, current: 0, English)!.Value;

            // Act
            var found = typeAhead.Find(CreateKey("e"), Cities, current, English);

            // Assert
            Assert.Equal(1, found);
        }

        [Fact]
        public void Should_start_again_with_a_character_pressed_after_the_pause()
        {
            // Arrange
            var typeAhead = new TypeAhead();
            typeAhead.Find(CreateKey("b", 1000), Cities, current: 0, English);

            // Act
            var found = typeAhead.Find(CreateKey("i", 1000 + TypeAhead.Pause.TotalMilliseconds + 1), Cities, current: 1, English);

            // Assert
            Assert.Equal(6, found);
        }

        [Fact]
        public void Should_cycle_through_the_labels_a_character_typed_again_starts()
        {
            // Arrange
            var typeAhead = new TypeAhead();
            var first = typeAhead.Find(CreateKey("b"), Cities, current: 0, English)!.Value;

            // Act
            var second = typeAhead.Find(CreateKey("b"), Cities, first, English)!.Value;
            var third = typeAhead.Find(CreateKey("b"), Cities, second, English)!.Value;

            // Assert
            Assert.Equal([1, 2, 3], [first, second, third]);
        }

        [Fact]
        public void Should_wrap_round_to_the_first_label_that_matches()
        {
            // Arrange
            var typeAhead = new TypeAhead();

            // Act
            var found = typeAhead.Find(CreateKey("a"), Cities, current: 5, English);

            // Assert
            Assert.Equal(0, found);
        }

        [Fact]
        public void Should_skip_a_label_that_cannot_be_moved_to()
        {
            // Arrange
            var typeAhead = new TypeAhead();

            // Act
            var found = typeAhead.Find(CreateKey("b"), Cities, current: 0, English, canTake: index => index != 1);

            // Assert
            Assert.Equal(2, found);
        }

        [Fact]
        public void Should_compare_the_labels_in_the_culture_given()
        {
            // Arrange — Turkish pairs a dotless lowercase letter with the plain capital
            var typeAhead = new TypeAhead();

            // Act
            var found = typeAhead.Find(CreateKey("ı"), ["Istanbul"], current: -1, CultureInfo.GetCultureInfo("tr-TR"));

            // Assert
            Assert.Equal(0, found);
        }

        [Fact]
        public void Should_find_nothing_where_no_label_starts_with_what_was_typed()
        {
            // Arrange
            var typeAhead = new TypeAhead();

            // Act
            var found = typeAhead.Find(CreateKey("z"), Cities, current: 0, English);

            // Assert
            Assert.Null(found);
        }
    }
}
