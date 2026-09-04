namespace Forma.Framework.Resources.Tests.Extensions;

using Forma.Framework.Resources.Extensions;

using Xunit;

public sealed class ResourceInfoExtensionsTests
{
    [Fact]
    public void Should_write_the_size_for_a_reader()
    {
        var resource = new ResourceInfo("/media/cover.jpg") { Size = 2048 };

        Assert.Equal(ResourceFormatter.FromByteCount(2048), resource.DisplaySize);
    }

    [Fact]
    public void Should_write_nothing_when_the_size_is_unknown()
    {
        var resource = new ResourceInfo("/media/cover.jpg");

        Assert.Null(resource.DisplaySize);
    }
}
