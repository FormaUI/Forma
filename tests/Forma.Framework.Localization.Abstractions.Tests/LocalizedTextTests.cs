namespace Forma.Framework.Localization.Abstractions.Tests;

public sealed class LocalizedTextTests
{
    [Fact]
    public void Should_render_as_its_value()
    {
        // Arrange
        var text = new LocalizedText("Save", "שמור");

        // Act
        var rendered = text.ToString();

        // Assert
        Assert.Equal("שמור", rendered);
    }

    [Fact]
    public void Should_convert_to_a_string_implicitly()
    {
        // Arrange
        var text = new LocalizedText("Save", "שמור");

        // Act
        string value = text;

        // Assert
        Assert.Equal("שמור", value);
    }

    [Fact]
    public void Should_report_a_found_resource_by_default()
    {
        // Act
        var text = new LocalizedText("Save", "שמור");

        // Assert
        Assert.False(text.ResourceNotFound);
        Assert.Null(text.SearchedLocation);
    }

    [Fact]
    public void Should_carry_the_key_it_was_looked_up_under()
    {
        // Act
        var text = new LocalizedText("Save", "Save", ResourceNotFound: true, SearchedLocation: "Forma.UI.Components.Actions.FormaButton");

        // Assert
        Assert.Equal("Save", text.Name);
        Assert.True(text.ResourceNotFound);
        Assert.Equal("Forma.UI.Components.Actions.FormaButton", text.SearchedLocation);
    }
}
