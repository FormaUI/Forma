namespace Forma.Framework.Localization.Tests.Files;

using static Forma.Tests.Localization.LocaleSections;

public sealed class LocaleFileTests
{
    [Fact]
    public void Should_list_a_types_own_entries_without_a_type_name()
    {
        // Arrange
        var file = new LocaleFile(CreateEntries(("Save", "שמור"), ("Reset", null)), null);

        // Act
        var entries = file.GetEntries();

        // Assert
        Assert.Equal(
            [new LocaleFileEntry(string.Empty, "Save", "שמור"), new LocaleFileEntry(string.Empty, "Reset", null)],
            entries);
    }

    [Fact]
    public void Should_name_the_type_each_section_completes()
    {
        // Arrange
        var file = new LocaleFile(
            CreateSections(
                ("Bars.Toolbar", CreateEntries(("Bold", "מודגש"))),
                ("Menus", CreateSections(("ShareMenu", CreateEntries(("Share", "שיתוף")))))),
            null);

        // Act
        var entries = file.GetEntries();

        // Assert
        Assert.Equal(
            [new LocaleFileEntry("Bars.Toolbar", "Bold", "מודגש"), new LocaleFileEntry("Menus.ShareMenu", "Share", "שיתוף")],
            entries);
    }

    [Fact]
    public void Should_list_the_entries_of_an_outer_type_before_its_nested_types()
    {
        // Arrange
        var file = new LocaleFile(
            new LocaleSection([new("Inner", CreateEntries(("Save", "nested")))], [new("Open", "outer")]),
            null);

        // Act
        var entries = file.GetEntries();

        // Assert
        Assert.Equal(
            [new LocaleFileEntry(string.Empty, "Open", "outer"), new LocaleFileEntry("Inner", "Save", "nested")],
            entries);
    }
}
