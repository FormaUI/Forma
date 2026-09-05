namespace Forma.Framework.Input.Tests;

using Forma.Framework.Input;

using Xunit;

public sealed class InputRouteTests
{
    [Theory]
    [InlineData("toolbar", "bold", "toolbar.bold")]
    [InlineData("toolbar", "Insert Table", "toolbar.insert-table")]
    [InlineData("toolbar", "format.bold", "toolbar.format-bold")]
    [InlineData("toolbar", "--Bold!--", "toolbar.bold")]
    public void Should_qualify_a_name_as_an_action_identifier(string owner, string name, string expected)
    {
        // Act
        var action = InputRoute.Qualify(owner, name);

        // Assert
        Assert.Equal(expected, action);
    }

    [Theory]
    [InlineData("toolbar", "!!!")]
    [InlineData("Toolbar", "bold")]
    [InlineData("", "bold")]
    public void Should_refuse_a_name_or_owner_that_leaves_no_identifier(string owner, string name)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => InputRoute.Qualify(owner, name));
    }

    [Fact]
    public void Should_accept_a_qualified_action()
    {
        // Act
        var route = new InputRoute(InputRoute.Qualify("toolbar", "bold"));

        // Assert
        Assert.Equal("toolbar.bold", route.Action);
    }
}
