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
                new("Contoso.Widgets.he.fake", "Bars.Toolbar", "Bold", "assembly file"),
                new("Contoso.Widgets.he.fake", "Bars.Toolbar", "Italic", "assembly file"),
                new("Contoso.Widgets/Bars/Bars.he.fake", "Toolbar", "Italic", "folder file"),
                new("Contoso.Widgets/Bars/Bars.he.fake", "Toolbar", "Underline", "folder file"),
                new("Contoso.Widgets/Bars/Toolbar.he.fake", string.Empty, "Underline", "type file"),
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
        Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Bars.he.fake", "Toolbar", "Bold", "beside the folder")], entries);
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
        Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Toolbar.he.fake", string.Empty, "Bold", "declared root")], entries);
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
        Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Toolbar.he.fake", string.Empty, "Bold", "no namespace")], entries);
    }

    [Fact]
    public void Should_take_a_file_a_source_already_holds_without_opening_it()
    {
        // Arrange
        var source = new PreparedLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/Toolbar.he.fake"] = new(CreateEntries(("Bold", "already read")), null),
            },
        };

        // Act
        var entries = source.Read(Assembly, "Contoso.Widgets.Bars.Toolbar", "he");

        // Assert
        Assert.Multiple(
            () => Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Bars/Toolbar.he.fake", string.Empty, "Bold", "already read")], entries),
            () => Assert.Contains("Contoso.Widgets/Bars/Toolbar.he.fake", source.Requests));
    }

    [Fact]
    public void Should_share_a_folders_entries_with_every_type_beneath_it()
    {
        // Arrange — the folder's file shares one entry, and a type with a section of its own overrides it.
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/Bars.he.fake"] = new(
                    CreateSections(("Toolbar", CreateEntries(("Bold", "own")))).WithShared(("Bold", "shared"), ("Italic", "shared")),
                    null),
            },
        };

        // Act
        var sectioned = source.Read(Assembly, "Contoso.Widgets.Bars.Toolbar", "he");
        var unsectioned = source.Read(Assembly, "Contoso.Widgets.Bars.Ruler", "he");

        // Assert
        Assert.Multiple(
            () => Assert.Equal(
                [
                    new("Contoso.Widgets/Bars/Bars.he.fake", "$shared", "Bold", "shared"),
                    new("Contoso.Widgets/Bars/Bars.he.fake", "$shared", "Italic", "shared"),
                    new("Contoso.Widgets/Bars/Bars.he.fake", "Toolbar", "Bold", "own"),
                ],
                sectioned),
            () => Assert.Equal(
                [
                    new LocaleSourceEntry("Contoso.Widgets/Bars/Bars.he.fake", "$shared", "Bold", "shared"),
                    new LocaleSourceEntry("Contoso.Widgets/Bars/Bars.he.fake", "$shared", "Italic", "shared"),
                ],
                unsectioned));
    }

    [Fact]
    public void Should_share_a_nested_sections_entries_with_the_types_beneath_it_alone()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets.he.fake"] = new(
                    CreateSections(("Bars", CreateSections().WithShared(("Bold", "bars"))), ("Menus", CreateSections(("Menu", CreateEntries(("Open", "menu")))))),
                    null),
            },
        };

        // Act
        var bars = source.Read(Assembly, "Contoso.Widgets.Bars.Toolbar", "he");
        var menus = source.Read(Assembly, "Contoso.Widgets.Menus.Menu", "he");

        // Assert
        Assert.Multiple(
            () => Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.he.fake", "Bars.$shared", "Bold", "bars")], bars),
            () => Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.he.fake", "Menus.Menu", "Open", "menu")], menus));
    }

    [Fact]
    public void Should_not_share_a_section_merely_named_like_the_shared_one()
    {
        // Arrange — a section under the reserved name, which an escaped key produces, is a section like any other.
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/Bars.he.fake"] = new(
                    CreateSections(("$shared", CreateEntries(("Bold", "escaped"))), ("Toolbar", CreateEntries(("Bold", "own")))),
                    null),
            },
        };

        // Act
        var sectioned = source.Read(Assembly, "Contoso.Widgets.Bars.Toolbar", "he");
        var unsectioned = source.Read(Assembly, "Contoso.Widgets.Bars.Ruler", "he");

        // Assert
        Assert.Multiple(
            () => Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Bars/Bars.he.fake", "Toolbar", "Bold", "own")], sectioned),
            () => Assert.Empty(unsectioned));
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
