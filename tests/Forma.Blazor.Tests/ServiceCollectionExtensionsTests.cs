namespace Forma.Blazor.Tests;

using Forma.Framework.Emojis;
using Forma.Framework.Emojis.Unicode;
using Forma.Framework.Icons;
using Forma.Framework.Icons.Glyphs;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_default_input_policy()
    {
        using var provider = new ServiceCollection().AddFormaUI().BuildServiceProvider();

        Assert.Same(InputRouterOptions.Default, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_register_the_host_input_policy()
    {
        var policy = new InputRouterOptions { Escape = DismissBehavior.Ignore };

        using var provider = new ServiceCollection().AddFormaUI(policy).BuildServiceProvider();

        Assert.Same(policy, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_leave_a_policy_the_host_registered_first()
    {
        var policy = new InputRouterOptions { Routes = [new InputRoute("app.palette", KeyBinding.Shortcut("k"))] };
        var services = new ServiceCollection();
        services.AddSingleton(policy);

        using var provider = services.AddFormaUI().BuildServiceProvider();

        Assert.Same(policy, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_register_the_bundled_artwork()
    {
        using var provider = new ServiceCollection().AddFormaUI().BuildServiceProvider();

        Assert.Multiple(
            () => Assert.IsType<GlyphIconProvider>(provider.GetRequiredService<IIconProvider>()),
            () => Assert.IsType<UnicodeEmojiProvider>(provider.GetRequiredService<IEmojiProvider>()));
    }
}
