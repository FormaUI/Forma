#!/usr/bin/env dotnet
#:package xunit.v3@4.0.2
#:include GlyphParser.cs

// The rules a glyph drawing is held to, exercised on drawings written inline.

namespace Forma.Scripts.Tests;

using Xunit;

public sealed class GlyphParserTests
{
    public sealed class Drawings
    {
        private const string StrokeRoot = @"xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 32 32"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""";

        private const string SolidRoot = @"xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 32 32"" fill=""currentColor"" fill-rule=""evenodd"" stroke=""none""";

        [Fact]
        public void Should_read_a_stroke_drawing()
        {
            // Arrange
            var problems = new List<string>();
            var content = Svg(StrokeRoot, @"<path d=""M 8 8 L 24 24"" />");

            // Act
            var drawing = Drawing.Parse(content, "Glyph.svg", 32, expectStroke: true, problems);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(2, drawing.Stroke);
                Assert.Equal("M 8 8 L 24 24", Assert.Single(drawing.Paths).Data);
                Assert.Empty(problems);
            });
        }

        [Fact]
        public void Should_read_the_opacity_and_the_solid_mark_of_a_path()
        {
            // Arrange
            var problems = new List<string>();
            var content = Svg(StrokeRoot, @"<path d=""M 8 8 L 24 24"" opacity=""0.4"" /><path d=""M 4 4 L 6 6"" fill=""currentColor"" />");

            // Act
            var drawing = Drawing.Parse(content, "Glyph.svg", 32, expectStroke: true, problems);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(0.4, drawing.Paths[0].Opacity);
                Assert.False(drawing.Paths[0].Filled);
                Assert.Equal(1, drawing.Paths[1].Opacity);
                Assert.True(drawing.Paths[1].Filled);
            });
        }

        [Theory]
        [InlineData(@"<?xml version=""1.0""?>")]
        [InlineData("<!-- a note -->")]
        public void Should_refuse_markup_that_is_not_the_drawing_alone(string extra)
        {
            // Arrange
            var content = extra + Svg(StrokeRoot, @"<path d=""M 8 8 L 24 24"" />");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => Drawing.Parse(content, "Glyph.svg", 32, expectStroke: true, []));
        }

        [Fact]
        public void Should_refuse_an_element_other_than_a_path()
        {
            // Arrange
            var content = Svg(StrokeRoot, @"<circle cx=""16"" cy=""16"" /><path d=""M 8 8 L 24 24"" />");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => Drawing.Parse(content, "Glyph.svg", 32, expectStroke: true, []));
        }

        [Theory]
        [InlineData("id")]
        [InlineData("class")]
        [InlineData("style")]
        [InlineData("transform")]
        public void Should_refuse_an_attribute_that_styles_or_moves_the_drawing(string attribute)
        {
            // Arrange
            var content = Svg(StrokeRoot, $@"<path {attribute}=""x"" d=""M 8 8 L 24 24"" />");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => Drawing.Parse(content, "Glyph.svg", 32, expectStroke: true, []));
        }

        [Fact]
        public void Should_refuse_a_view_box_that_is_not_the_grid_it_is_read_for()
        {
            // Arrange
            var content = Svg(StrokeRoot, @"<path d=""M 8 8 L 24 24"" />");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => Drawing.Parse(content, "Glyph.svg", 24, expectStroke: true, []));
        }

        [Fact]
        public void Should_refuse_root_attributes_that_are_neither_form()
        {
            // Arrange
            var content = Svg(@"xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 32 32"" fill=""red""", @"<path d=""M 8 8 L 24 24"" />");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => Drawing.Parse(content, "Glyph.svg", 32, expectStroke: true, []));
        }

        [Fact]
        public void Should_refuse_the_filled_form_where_a_stroke_drawing_is_asked_for()
        {
            // Arrange
            var content = Svg(SolidRoot, @"<path d=""M 8 8 L 24 24"" />");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => Drawing.Parse(content, "Glyph.svg", 32, expectStroke: true, []));
        }

        [Fact]
        public void Should_read_the_filled_form_where_no_stroke_drawing_is_asked_for()
        {
            // Arrange
            var content = Svg(SolidRoot, @"<path d=""M 8 8 L 24 24"" />");

            // Act
            var drawing = Drawing.Parse(content, "Glyph.svg", 32, expectStroke: false, []);

            // Assert
            Assert.Equal(0, drawing.Stroke);
        }

        [Fact]
        public void Should_refuse_a_coordinate_off_the_grid()
        {
            // Arrange
            var content = Svg(StrokeRoot, @"<path d=""M 8.25 8 L 24 24"" />");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => Drawing.Parse(content, "Glyph.svg", 32, expectStroke: true, []));
        }

        [Fact]
        public void Should_allow_a_solid_silhouette_a_half_unit_on_the_reference_grid()
        {
            // Arrange
            var content = Svg(SolidRoot, @"<path d=""M 8.5 8 L 24 24"" fill=""currentColor"" stroke=""none"" fill-rule=""evenodd"" />");

            // Act
            var drawing = Drawing.Parse(content, "Glyph.svg", 32, expectStroke: false, []);

            // Assert
            Assert.Equal("M 8.5 8 L 24 24", Assert.Single(drawing.Paths).Data);
        }

        [Fact]
        public void Should_refuse_a_drawing_with_no_path()
        {
            // Arrange
            var content = Svg(StrokeRoot, string.Empty);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => Drawing.Parse(content, "Glyph.svg", 32, expectStroke: true, []));
        }

        [Fact]
        public void Should_report_a_circle_whose_points_do_not_span_its_diameter()
        {
            // Arrange
            var problems = new List<string>();
            var content = Svg(StrokeRoot, @"<path d=""M 10 16 A 6 6 0 1 1 20 16 A 6 6 0 1 1 10 16 Z"" />");

            // Act
            Drawing.Parse(content, "Lens.svg", 32, expectStroke: true, problems);

            // Assert
            Assert.Contains("Lens.svg", Assert.Single(problems), StringComparison.Ordinal);
        }

        [Fact]
        public void Should_pass_a_circle_whose_points_span_its_diameter()
        {
            // Arrange
            var problems = new List<string>();
            var content = Svg(StrokeRoot, @"<path d=""M 10 16 A 6 6 0 1 1 22 16 A 6 6 0 1 1 10 16 Z"" />");

            // Act
            Drawing.Parse(content, "Circle.svg", 32, expectStroke: true, problems);

            // Assert
            Assert.Empty(problems);
        }

        [Fact]
        public void Should_report_an_arc_the_renderer_would_enlarge()
        {
            // Arrange
            var problems = new List<string>();
            var content = Svg(StrokeRoot, @"<path d=""M 8 16 A 2 2 0 0 1 18 16"" />");

            // Act
            Drawing.Parse(content, "Flat.svg", 32, expectStroke: true, problems);

            // Assert
            Assert.Contains("Flat.svg", Assert.Single(problems), StringComparison.Ordinal);
        }

        [Fact]
        public void Should_pass_an_arc_a_rounded_end_only_just_overshoots()
        {
            // Arrange
            var problems = new List<string>();
            var content = Svg(StrokeRoot, @"<path d=""M 8 16 A 6 6 0 0 1 18 16"" />");

            // Act
            Drawing.Parse(content, "Curve.svg", 32, expectStroke: true, problems);

            // Assert
            Assert.Empty(problems);
        }

        private static string Svg(string rootAttributes, string body)
            => $"<svg {rootAttributes}>{body}</svg>\n";
    }

    public sealed class Drafts
    {
        [Fact]
        public void Should_scale_the_view_box_to_the_target_grid()
        {
            // Arrange
            var content = @"<svg viewBox=""0 0 32 32""><path d=""M 8 8 L 24 24"" /></svg>";

            // Act
            var scaled = GlyphDraft.Scale(content, 32, 24);

            // Assert
            Assert.Contains(@"viewBox=""0 0 24 24""", scaled, StringComparison.Ordinal);
        }

        [Fact]
        public void Should_scale_every_coordinate_and_snap_it_to_the_grid()
        {
            // Arrange
            var content = @"<svg viewBox=""0 0 32 32""><path d=""M 8 8 L 24 24"" /></svg>";

            // Act
            var scaled = GlyphDraft.Scale(content, 32, 24);

            // Assert
            Assert.Contains(@"d=""M 6 6 L 18 18""", scaled, StringComparison.Ordinal);
        }

        [Fact]
        public void Should_scale_an_arc_radius_as_a_length_and_leave_its_flags_alone()
        {
            // Arrange
            var content = @"<svg viewBox=""0 0 32 32""><path d=""A 4 4 0 1 0 16 16"" /></svg>";

            // Act
            var scaled = GlyphDraft.Scale(content, 32, 24);

            // Assert — the radii and the end point scale; the rotation and both flags carry over untouched.
            Assert.Contains(@"d=""A 3 3 0 1 0 12 12""", scaled, StringComparison.Ordinal);
        }

        [Fact]
        public void Should_round_a_coordinate_landing_between_units_away_from_zero()
        {
            // Arrange — 10 on the 32 grid is 7.5 on the 24 grid.
            var content = @"<svg viewBox=""0 0 32 32""><path d=""M 10 10"" /></svg>";

            // Act
            var scaled = GlyphDraft.Scale(content, 32, 24);

            // Assert
            Assert.Contains(@"d=""M 8 8""", scaled, StringComparison.Ordinal);
        }

        [Fact]
        public void Should_leave_a_drawing_already_on_the_target_grid_as_it_is()
        {
            // Arrange
            var content = @"<svg viewBox=""0 0 24 24""><path d=""M 6 6 L 18 18"" /></svg>";

            // Act
            var scaled = GlyphDraft.Scale(content, 24, 24);

            // Assert
            Assert.Equal(content, scaled);
        }
    }
}
