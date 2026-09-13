namespace Forma.Framework.Localization.Tests.Files;

using static Forma.Tests.Localization.LocaleSections;

public sealed class LocaleSourceTests
{
    private const string Assembly = "Contoso.Widgets";

    [Fact]
    public void Should_walk_from_the_assemblys_file_down_to_the_types_file()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets.he.fake"] = new(CreateSections(("Bars.Toolbar", CreateEntries(("Bold", "assembly file"), ("Italic", "assembly file")))), null),
                ["Contoso.Widgets/Bars/Bars.he.fake"] = new(CreateSections(("Toolbar", CreateEntries(("Italic", "folder file"), ("Underline", "folder file")))), null),
                ["Contoso.Widgets/Bars/Toolbar.he.fake"] = new(CreateEntries(("Underline", "type file")), null),
            },
        };

        // Act
        var entries = source.Read(Assembly, "Contoso.Widgets.Bars.Toolbar", "he");

        // Assert
        Assert.Equal(
            [
                new("Bold", "assembly file"),
                new("Italic", "assembly file"),
                new("Italic", "folder file"),
                new("Underline", "folder file"),
                new("Underline", "type file"),
            ],
            entries);
    }

    [Fact]
    public void Should_read_a_file_named_for_a_folder_from_beside_it_too()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars.he.fake"] = new(CreateSections(("Toolbar", CreateEntries(("Bold", "beside the folder")))), null),
            },
        };

        // Act
        var entries = source.Read(Assembly, "Contoso.Widgets.Bars.Toolbar", "he");

        // Assert
        Assert.Equal([new("Bold", "beside the folder")], entries);
    }

    [Fact]
    public void Should_complete_a_types_name_from_the_root_a_file_declares()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Toolbar.he.fake"] = new(CreateEntries(("Bold", "declared root")), "Contoso.Widgets.Bars"),
            },
        };

        // Act
        var entries = source.Read(Assembly, "Contoso.Widgets.Bars.Toolbar", "he");

        // Assert
        Assert.Equal([new("Bold", "declared root")], entries);
    }

    [Fact]
    public void Should_ignore_a_file_whose_declared_root_does_not_meet_its_path()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/Toolbar.he.fake"] = new(CreateEntries(("Bold", "wrong root")), "Contoso.Widgets.Bars"),
            },
        };

        // Act
        var entries = source.Read(Assembly, "Contoso.Widgets.Bars.Toolbar", "he");

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void Should_lay_a_type_outside_any_namespace_at_the_assembly()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Toolbar.he.fake"] = new(CreateEntries(("Bold", "no namespace")), null),
            },
        };

        // Act
        var entries = source.Read(Assembly, "Toolbar", "he");

        // Assert
        Assert.Equal([new("Bold", "no namespace")], entries);
    }

    [Fact]
    public void Should_ask_for_each_file_once()
    {
        // Arrange
        var source = new FakeLocaleSource();

        // Act
        source.Read(Assembly, "Contoso.Widgets.Bars.Toolbar", "he");
        source.Read(Assembly, "Contoso.Widgets.Bars.Toolbar", "he");

        // Assert
        Assert.Equal(source.Requests.Count, source.Requests.Distinct(StringComparer.Ordinal).Count());
    }
}
