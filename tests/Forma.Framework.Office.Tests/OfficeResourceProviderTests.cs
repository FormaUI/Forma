namespace Forma.Framework.Office.Tests;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.Tests.Office;
using Forma.Tests.Resources;
using Forma.Tests.Shared;

using Microsoft.Extensions.Localization;

public sealed class OfficeResourceProviderTests
{
    private static JsonStringLocalizer<OfficeResourceProvider> Localizer()
        => new(new JsonStringLocalizerFactory());

    [Theory]
    [InlineData("/media/report.pdf")]
    [InlineData("/media/photo.png")]
    [InlineData("/media/notes.md")]
    [InlineData("/media/report")]
    [InlineData("/media/legacy.doc")]
    [InlineData("/media/legacy.xls")]
    public async Task Should_leave_an_address_it_does_not_claim_to_another_provider(string source)
    {
        // Arrange
        var reader = new FakeResourceContentReader();
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync(source, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(resource);
    }

    [Theory]
    [InlineData("/media/report.docx")]
    [InlineData("/media/REPORT.DOCX")]
    public async Task Should_convert_a_document_to_markup(string source)
    {
        // Arrange
        var reader = new FakeResourceContentReader()
            .Add(source, OfficeFixture.Docx(document => document.AddParagraph("Quarterly revenue rose.")));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync(source, TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.Equal(ResourceKind.Document, markup.Kind);
            Assert.Contains("Quarterly revenue rose.", markup.Html, StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData("/media/budget.xlsx")]
    [InlineData("/media/BUDGET.XLSX")]
    public async Task Should_convert_a_workbook_to_a_table_per_sheet(string source)
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add(source, OfficeFixture.Xlsx(workbook =>
        {
            var sheet = workbook.AddWorksheet("Regions");

            sheet.CellValue(1, 1, "Region");
            sheet.CellValue(1, 2, "Change");
            sheet.CellValue(2, 1, "South");
            sheet.CellValue(2, 2, "11.7");
        }));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync(source, TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.Contains("Regions", markup.Html, StringComparison.Ordinal);
            Assert.Contains("<thead><tr><th>Region</th>", markup.Html, StringComparison.Ordinal);
            Assert.Contains("<tbody><tr><td>South</td>", markup.Html, StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData("/media/deck.pptx")]
    [InlineData("/media/DECK.PPTX")]
    public async Task Should_convert_a_presentation_to_a_section_per_slide(string source)
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add(source, OfficeFixture.Pptx(presentation =>
        {
            var first = presentation.AddSlide();

            first.AddTitleInches("Roadmap for the year", 1, 0.5, 8, 1);
            first.AddTextBoxInches("Opening remarks", 1, 2, 8, 1);

            presentation.AddSlide().AddTitleInches("Second slide", 1, 0.5, 8, 1);
        }));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync(source, TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.Contains("<h2>Roadmap for the year</h2>", markup.Html, StringComparison.Ordinal);
            Assert.Contains("<p>Opening remarks</p>", markup.Html, StringComparison.Ordinal);
            Assert.Contains("<h2>Second slide</h2>", markup.Html, StringComparison.Ordinal);
        });
    }

    // A heading is taken from the slide when it has one, so a titled slide never shows a generated name.
    [Fact]
    public async Task Should_prefer_the_slide_title_over_a_generated_heading()
    {
        // Arrange
        using var fixture = new CultureFixture("en");
        var reader = new FakeResourceContentReader().Add("/media/deck.pptx", OfficeFixture.Pptx(presentation =>
            presentation.AddSlide().AddTitleInches("Only the title", 1, 0.5, 8, 1)));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/deck.pptx", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.Contains("<h2>Only the title</h2>", markup.Html, StringComparison.Ordinal);
            Assert.DoesNotContain("Slide 1", markup.Html, StringComparison.Ordinal);
        });
    }

    // A layout inserts a title placeholder whether or not the author fills it, so an empty one still needs naming.
    [Fact]
    public async Task Should_name_a_slide_whose_title_placeholder_is_empty()
    {
        // Arrange
        using var fixture = new CultureFixture("en");
        var reader = new FakeResourceContentReader().Add("/media/deck.pptx", OfficeFixture.Pptx(presentation =>
        {
            var slide = presentation.AddSlide();

            slide.AddTitleInches("   ", 1, 0.5, 8, 1);
            slide.AddTextBoxInches("Body copy", 1, 2, 8, 1);
        }));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/deck.pptx", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.Contains("<h2>Slide 1</h2>", markup.Html, StringComparison.Ordinal);
            Assert.Contains("<p>Body copy</p>", markup.Html, StringComparison.Ordinal);
        });
    }

    // Cell text is caller data, so it must reach the viewer as text rather than as markup.
    [Fact]
    public async Task Should_escape_markup_a_workbook_cell_contains()
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add("/media/budget.xlsx", OfficeFixture.Xlsx(workbook =>
            workbook.AddWorksheet("Sheet1").CellValue(1, 1, "<script>alert(1)</script>")));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/budget.xlsx", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.DoesNotContain("<script", markup.Html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("&lt;script", markup.Html, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Should_escape_markup_a_slide_contains()
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add("/media/deck.pptx", OfficeFixture.Pptx(presentation =>
            presentation.AddSlide().AddTextBoxInches("<img src=x onerror=alert(1)>", 1, 1, 4, 1)));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/deck.pptx", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.DoesNotContain("<img", markup.Html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("&lt;img", markup.Html, StringComparison.Ordinal);
        });
    }

    // Truncation is announced through this package's own resources so it follows the reader's culture.
    [Fact]
    public async Task Should_announce_a_truncated_sheet()
    {
        // Arrange
        using var fixture = new CultureFixture("en");
        var reader = new FakeResourceContentReader().Add("/media/big.xlsx", OfficeFixture.Xlsx(workbook =>
        {
            var sheet = workbook.AddWorksheet("Big");

            for (var row = 1; row <= 620; row++)
            {
                sheet.CellValue(row, 1, $"Row {row}");
            }
        }));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/big.xlsx", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.Contains("Row 500", markup.Html, StringComparison.Ordinal);
            Assert.DoesNotContain("Row 501", markup.Html, StringComparison.Ordinal);
            Assert.Contains("Showing the first 500 rows", markup.Html, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Should_claim_the_three_open_xml_formats()
    {
        // Arrange
        var provider = new OfficeResourceProvider(new FakeResourceContentReader(), Localizer());

        // Act & Assert
        Assert.Equal(
            [".docx", ".pptx", ".xlsx"],
            provider.ClaimedExtensions.Order(StringComparer.Ordinal));
    }

    // A host with no reader registered for the address must degrade to the viewer's download link.
    [Fact]
    public async Task Should_return_nothing_when_the_reader_does_not_have_the_content()
    {
        // Arrange
        var provider = new OfficeResourceProvider(new FakeResourceContentReader(), Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/missing.docx", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(resource);
    }

    [Fact]
    public async Task Should_report_the_size_of_the_document_it_read()
    {
        // Arrange
        var content = OfficeFixture.Docx(document => document.AddParagraph("Sized."));
        var reader = new FakeResourceContentReader().Add("/media/report.docx", content);
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/report.docx", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(content.Length, resource?.Size);
    }

    // A reader backed by a network response cannot seek, which Open XML requires.
    [Fact]
    public async Task Should_convert_a_document_a_reader_could_not_seek()
    {
        // Arrange
        var reader = new FakeResourceContentReader { Seekable = false }
            .Add("/media/report.docx", OfficeFixture.Docx(document => document.AddParagraph("Streamed in one pass.")));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/report.docx", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.Contains("Streamed in one pass.", markup.Html, StringComparison.Ordinal);
            Assert.False(reader.HasOpenStream);
        });
    }

    [Fact]
    public async Task Should_close_the_stream_the_reader_handed_it()
    {
        // Arrange
        var reader = new FakeResourceContentReader()
            .Add("/media/report.docx", OfficeFixture.Docx(document => document.AddParagraph("Closed.")));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        await provider.GetResourceAsync("/media/report.docx", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(reader.HasOpenStream);
    }

    // The viewer renders this markup as trusted HTML, so a scripted link must not survive conversion.
    [Fact]
    public async Task Should_strip_a_link_carrying_a_script_scheme()
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add(
            "/media/report.docx",
            OfficeFixture.Docx(document => document.AddHyperLink("Read more", new Uri("javascript:alert(1)"))));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/report.docx", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.DoesNotContain("javascript:", markup.Html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Read more", markup.Html, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Should_keep_an_ordinary_link_intact()
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add(
            "/media/report.docx",
            OfficeFixture.Docx(document => document.AddHyperLink("Read more", new Uri("https://example.com/report"))));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/report.docx", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Contains("https://example.com/report", markup.Html, StringComparison.Ordinal);
    }

    // A document routinely addresses a mailbox, and that scheme cannot carry script.
    [Fact]
    public async Task Should_keep_a_link_to_a_mailbox()
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add(
            "/media/report.docx",
            OfficeFixture.Docx(document => document.AddHyperLink("Contact", new Uri("mailto:team@example.com"))));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/report.docx", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Contains("mailto:team@example.com", markup.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_reject_an_address_that_is_missing()
    {
        // Arrange
        var provider = new OfficeResourceProvider(new FakeResourceContentReader(), Localizer());

        // Act
        var thrown = await Record.ExceptionAsync(
            () => provider.GetResourceAsync(" ", TestContext.Current.CancellationToken).AsTask());

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Should_reject_construction_without_a_reader()
    {
        // Act
        var thrown = Record.Exception(() => new OfficeResourceProvider(null!, Localizer()));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void Should_reject_construction_without_a_localizer()
    {
        // Act
        var thrown = Record.Exception(() => new OfficeResourceProvider(new FakeResourceContentReader(), null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    // The heading a slide cannot supply comes from this package's own resources, not a fixed language.
    [Theory]
    [InlineData("he", "\u05e9\u05e7\u05d5\u05e4\u05d9\u05ea 1")]
    [InlineData("es", "Diapositiva 1")]
    [InlineData("en", "Slide 1")]
    public async Task Should_name_an_untitled_slide_in_the_current_culture(string culture, string expected)
    {
        // Arrange
        using var fixture = new CultureFixture(culture);
        var reader = new FakeResourceContentReader().Add("/media/deck.pptx", OfficeFixture.Pptx(presentation =>
            presentation.AddSlide().AddTextBoxInches("Body only", 1, 1, 4, 1)));
        var provider = new OfficeResourceProvider(reader, Localizer());

        // Act
        var resource = await provider.GetResourceAsync("/media/deck.pptx", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Contains($"<h2>{expected}</h2>", markup.Html, StringComparison.Ordinal);
    }
}
