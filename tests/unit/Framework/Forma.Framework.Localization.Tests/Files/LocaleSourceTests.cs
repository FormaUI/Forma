namespace Forma.Framework.Localization.Tests.Files;

using static Forma.Tests.Localization.LocaleSections;

public sealed class LocaleSourceTests
{
    private const string Assembly = "Contoso.Widgets";
    private const string Toolbar = "Contoso.Widgets.Bars.Toolbar";

    // A form other than the default, reading files beside the folder they complete.
    private static readonly LocalePath Beside = LocalePath.Parse("$RootDir/$AnchorName.$Culture$Extension");

    [Fact]
    public void Should_read_from_the_root_down_to_the_types_own_folder()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/.locales/he.fake"] = new(CreateSections(("Bars.Toolbar", CreateEntries(("Bold", "root file"), ("Italic", "root file"))))),
                ["Contoso.Widgets/Bars/.locales/he.fake"] = new(CreateSections(("Toolbar", CreateEntries(("Italic", "folder file"), ("Underline", "folder file"))))),
                ["Contoso.Widgets/Bars/Toolbar/.locales/he.fake"] = new(CreateEntries(("Underline", "type's own file"))),
            },
        };

        // Act
        var entries = source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Equal(
            [
                new("Contoso.Widgets/.locales/he.fake", "Bars.Toolbar", "Bold", "root file"),
                new("Contoso.Widgets/.locales/he.fake", "Bars.Toolbar", "Italic", "root file"),
                new("Contoso.Widgets/Bars/.locales/he.fake", "Toolbar", "Italic", "folder file"),
                new("Contoso.Widgets/Bars/.locales/he.fake", "Toolbar", "Underline", "folder file"),
                new("Contoso.Widgets/Bars/Toolbar/.locales/he.fake", string.Empty, "Underline", "type's own file"),
            ],
            entries);
    }

    [Fact]
    public void Should_read_a_file_named_for_the_type_in_its_folders_locales_folder()
    {
        // Arrange — the folder's file and the type's own file, both in the folder's .locales.
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/.locales/he.fake"] = new(CreateSections(("Toolbar", CreateEntries(("Bold", "folder file"), ("Italic", "folder file"))))),
                ["Contoso.Widgets/Bars/.locales/Toolbar.he.fake"] = new(CreateEntries(("Italic", "type's own file"))),
            },
        };

        // Act
        var entries = source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Equal(
            [
                new("Contoso.Widgets/Bars/.locales/he.fake", "Toolbar", "Bold", "folder file"),
                new("Contoso.Widgets/Bars/.locales/he.fake", "Toolbar", "Italic", "folder file"),
                new("Contoso.Widgets/Bars/.locales/Toolbar.he.fake", string.Empty, "Italic", "type's own file"),
            ],
            entries);
    }

    [Fact]
    public void Should_read_a_rooted_section_from_the_assemblys_root_wherever_the_file_sits()
    {
        // Arrange — a file in another folder names the type from the root.
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/.locales/he.fake"] = new(CreateSections(("Toolbar", CreateEntries(("Bold", "folder file"), ("Italic", "folder file"))))),
                ["Contoso.Widgets/Strings/.locales/he.fake"] = new(CreateSections(("@.Bars.Toolbar", CreateEntries(("Italic", "rooted section"))))),
            },
        };

        // Act
        var entries = source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Equal(
            [
                new("Contoso.Widgets/Bars/.locales/he.fake", "Toolbar", "Bold", "folder file"),
                new("Contoso.Widgets/Bars/.locales/he.fake", "Toolbar", "Italic", "folder file"),
                new("Contoso.Widgets/Strings/.locales/he.fake", "@.Bars.Toolbar", "Italic", "rooted section"),
            ],
            entries);
    }

    [Fact]
    public void Should_read_a_rooted_section_naming_a_namespace_for_every_type_beneath_it()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/.locales/he.fake"] = new(CreateSections(("@.Bars", CreateSections(("Toolbar", CreateEntries(("Bold", "rooted namespace"))))))),
            },
        };

        // Act
        var entries = source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/.locales/he.fake", "@.Bars.Toolbar", "Bold", "rooted namespace")], entries);
    }

    [Fact]
    public void Should_read_a_rooted_section_for_a_type_outside_the_root_namespace_by_its_full_name()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/.locales/he.fake"] = new(CreateSections(("@.Fabrikam.Gadgets.Dial", CreateEntries(("Bold", "full name"))))),
            },
        };

        // Act
        var entries = source.Read(Assembly, "Fabrikam.Gadgets.Dial", "he");

        // Assert
        Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/.locales/he.fake", "@.Fabrikam.Gadgets.Dial", "Bold", "full name")], entries);
    }

    [Fact]
    public void Should_leave_a_rooted_section_beneath_the_top_level_unread()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/.locales/he.fake"] = new(CreateSections(("Bars", CreateSections(("@.Bars.Toolbar", CreateEntries(("Bold", "nested"))))))),
            },
        };

        // Act
        var entries = source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void Should_read_the_form_its_config_names_in_place_of_the_default()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars.he.fake"] = new(CreateSections(("Toolbar", CreateEntries(("Bold", "beside the folder"))))),
                ["Contoso.Widgets/Bars/.locales/he.fake"] = new(CreateSections(("Toolbar", CreateEntries(("Bold", "the default form"))))),
            },
            Configs =
            {
                ["Contoso.Widgets/.locale.json"] = LocaleConfig.Empty with { Paths = [Beside] },
            },
        };

        // Act
        var entries = source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Bars.he.fake", "Toolbar", "Bold", "beside the folder")], entries);
    }

    [Fact]
    public void Should_complete_a_types_name_from_the_root_its_config_declares()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Toolbar/.locales/he.fake"] = new(CreateEntries(("Bold", "declared root"))),
            },
            Configs =
            {
                ["Contoso.Widgets/.locale.json"] = LocaleConfig.Empty with { RootNamespace = "Contoso.Widgets.Bars" },
            },
        };

        // Act
        var entries = source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Toolbar/.locales/he.fake", string.Empty, "Bold", "declared root")], entries);
    }

    [Fact]
    public void Should_ignore_a_file_whose_path_does_not_meet_the_declared_root()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/Toolbar/.locales/he.fake"] = new(CreateEntries(("Bold", "wrong root"))),
            },
            Configs =
            {
                ["Contoso.Widgets/.locale.json"] = LocaleConfig.Empty with { RootNamespace = "Contoso.Widgets.Bars" },
            },
        };

        // Act
        var entries = source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void Should_lay_a_type_outside_any_namespace_at_the_root()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/.locales/he.fake"] = new(CreateSections(("Toolbar", CreateEntries(("Bold", "no namespace"))))),
            },
        };

        // Act
        var entries = source.Read(Assembly, "Toolbar", "he");

        // Assert
        Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/.locales/he.fake", "Toolbar", "Bold", "no namespace")], entries);
    }

    [Fact]
    public void Should_take_a_file_a_source_already_holds_without_opening_it()
    {
        // Arrange
        var source = new PreparedLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/.locales/he.fake"] = new(CreateSections(("Toolbar", CreateEntries(("Bold", "already read"))))),
            },
        };

        // Act
        var entries = source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Multiple(
            () => Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Bars/.locales/he.fake", "Toolbar", "Bold", "already read")], entries),
            () => Assert.Contains("Contoso.Widgets/Bars/.locales/he.fake", source.Requests));
    }

    [Fact]
    public void Should_share_a_folders_entries_with_every_type_beneath_it()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/.locales/he.fake"] = new(
                    CreateSections(("Toolbar", CreateEntries(("Bold", "own")))).WithShared(("Bold", "shared"), ("Italic", "shared"))),
            },
        };

        // Act
        var sectioned = source.Read(Assembly, Toolbar, "he");
        var unsectioned = source.Read(Assembly, "Contoso.Widgets.Bars.Ruler", "he");

        // Assert
        Assert.Multiple(
            () => Assert.Equal(
                [
                    new("Contoso.Widgets/Bars/.locales/he.fake", "$shared", "Bold", "shared"),
                    new("Contoso.Widgets/Bars/.locales/he.fake", "$shared", "Italic", "shared"),
                    new("Contoso.Widgets/Bars/.locales/he.fake", "Toolbar", "Bold", "own"),
                ],
                sectioned),
            () => Assert.Equal(
                [
                    new LocaleSourceEntry("Contoso.Widgets/Bars/.locales/he.fake", "$shared", "Bold", "shared"),
                    new LocaleSourceEntry("Contoso.Widgets/Bars/.locales/he.fake", "$shared", "Italic", "shared"),
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
                ["Contoso.Widgets/.locales/he.fake"] = new(
                    CreateSections(("Bars", CreateSections().WithShared(("Bold", "bars"))), ("Menus", CreateSections(("Menu", CreateEntries(("Open", "menu"))))))),
            },
        };

        // Act
        var bars = source.Read(Assembly, Toolbar, "he");
        var menus = source.Read(Assembly, "Contoso.Widgets.Menus.Menu", "he");

        // Assert
        Assert.Multiple(
            () => Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/.locales/he.fake", "Bars.$shared", "Bold", "bars")], bars),
            () => Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/.locales/he.fake", "Menus.Menu", "Open", "menu")], menus));
    }

    [Fact]
    public void Should_not_share_a_section_merely_named_like_the_shared_one()
    {
        // Arrange — a section under the reserved name, as an escaped key produces.
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/.locales/he.fake"] = new(
                    CreateSections(("$shared", CreateEntries(("Bold", "escaped"))), ("Toolbar", CreateEntries(("Bold", "own"))))),
            },
        };

        // Act
        var sectioned = source.Read(Assembly, Toolbar, "he");
        var unsectioned = source.Read(Assembly, "Contoso.Widgets.Bars.Ruler", "he");

        // Assert
        Assert.Multiple(
            () => Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Bars/.locales/he.fake", "Toolbar", "Bold", "own")], sectioned),
            () => Assert.Empty(unsectioned));
    }

    [Fact]
    public void Should_open_a_file_once_however_often_its_type_is_read()
    {
        // Arrange
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets/Bars/.locales/he.fake"] = new(CreateSections(("Toolbar", CreateEntries(("Bold", "folder file"))))),
            },
        };

        // Act
        source.Read(Assembly, Toolbar, "he");
        source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Equal(["Contoso.Widgets/Bars/.locales/he.fake"], source.Requests);
    }

    [Fact]
    public void Should_read_a_file_two_paths_name_at_one_anchor_once()
    {
        // Arrange — at the root, a file beside the root's folder is what both paths name.
        var source = new FakeLocaleSource
        {
            Files =
            {
                ["Contoso.Widgets.he.fake"] = new(CreateSections(("Bars.Toolbar", CreateEntries(("Bold", "root file"))))),
            },
            Configs =
            {
                ["Contoso.Widgets/.locale.json"] = LocaleConfig.Empty with { Paths = [Beside, LocalePath.Parse("$RootDir/$AnchorName/$CurrentName.$Culture$Extension")] },
            },
        };

        // Act
        var entries = source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.he.fake", "Bars.Toolbar", "Bold", "root file")], entries);
    }

    [Fact]
    public void Should_ask_for_each_file_once()
    {
        // Arrange
        var source = new FakeLocaleSource();

        // Act
        source.Read(Assembly, Toolbar, "he");
        source.Read(Assembly, Toolbar, "he");

        // Assert
        Assert.Equal(source.Requests.Count, source.Requests.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("Contoso.Widgets/.locales/he.fake", "Contoso.Widgets")]
    [InlineData("Contoso.Widgets/Bars/.locales/he.fake", "Contoso.Widgets.Bars")]
    public void Should_name_the_anchor_a_file_stands_for(string name, string expected)
    {
        // Arrange
        var source = new FakeLocaleSource { Files = { [name] = new(CreateEntries(("Bold", "any"))) } };

        // Act
        var anchorNames = source.GetAnchorNames(Assembly, name, "he");

        // Assert
        Assert.Equal([expected], anchorNames);
    }

    [Fact]
    public void Should_name_no_anchor_for_a_file_no_path_reads()
    {
        // Arrange
        const string Name = "Contoso.Widgets/Bars.he.fake";
        var source = new FakeLocaleSource { Files = { [Name] = new(CreateEntries(("Bold", "any"))) } };

        // Act
        var anchorNames = source.GetAnchorNames(Assembly, Name, "he");

        // Assert
        Assert.Empty(anchorNames);
    }

    [Fact]
    public void Should_name_no_anchor_for_a_file_in_another_culture()
    {
        // Arrange
        const string Name = "Contoso.Widgets/Bars/.locales/he.fake";
        var source = new FakeLocaleSource { Files = { [Name] = new(CreateEntries(("Bold", "any"))) } };

        // Act
        var anchorNames = source.GetAnchorNames(Assembly, Name, "es");

        // Assert
        Assert.Empty(anchorNames);
    }

    [Fact]
    public void Should_name_the_full_name_a_type_outside_the_root_namespace_reads_a_file_by()
    {
        // Arrange
        const string Name = "Contoso.Widgets/Fabrikam/Gadgets/.locales/he.fake";
        var source = new FakeLocaleSource
        {
            Files = { [Name] = new(CreateEntries(("Bold", "any"))) },
            Configs = { ["Contoso.Widgets/.locale.json"] = LocaleConfig.Empty with { RootNamespace = "Contoso.Widgets" } },
        };

        // Act
        var anchorNames = source.GetAnchorNames(Assembly, Name, "he");

        // Assert
        Assert.Equal(["Contoso.Widgets.Fabrikam.Gadgets", "Fabrikam.Gadgets"], anchorNames);
    }
}
