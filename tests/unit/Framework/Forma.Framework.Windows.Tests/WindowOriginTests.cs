namespace Forma.Framework.Windows.Tests;

using Xunit;

public sealed class WindowOriginTests
{
    [Fact]
    public void Should_give_each_instance_its_own_identity_under_one_origin()
    {
        // Act
        var first = WindowOrigin.NewInstance("explorer", "Explorer");
        var second = WindowOrigin.NewInstance("explorer", "Explorer");

        // Assert
        Assert.Multiple(
            () => Assert.Equal(first.OriginId, second.OriginId),
            () => Assert.NotEqual(first.InstanceId, second.InstanceId));
    }

    [Fact]
    public void Should_keep_its_identity_when_renamed()
    {
        // Arrange
        var origin = WindowOrigin.NewInstance("explorer", "Explorer");

        // Act
        var renamed = origin with { OriginName = "File Explorer" };

        // Assert
        Assert.Multiple(
            () => Assert.Equal(origin.OriginId, renamed.OriginId),
            () => Assert.Equal(origin.InstanceId, renamed.InstanceId));
    }

    [Theory]
    [InlineData("", "Explorer")]
    [InlineData("explorer", " ")]
    public void Should_require_an_id_and_a_name(string id, string name)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new WindowOrigin(id, name, Guid.NewGuid()));
    }
}
