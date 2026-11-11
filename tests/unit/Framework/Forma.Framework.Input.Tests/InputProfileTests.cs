namespace Forma.Framework.Input.Tests;

using System.Text.Json;

using Forma.Framework.Input;
using Forma.Framework.Input.Drop;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Input.Pointer;
using Forma.Framework.Input.Wheel;

using Xunit;

public sealed class InputProfileTests
{
    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Should_serialize_to_the_shape_the_script_reads()
    {
        var profile = new InputProfile
        {
            Modal = true,
            Escape = DismissBehavior.Cancel,
            PressAway = PressAwayBehavior.PassThrough,
            Boundary = ["keydown"],
            InputRoutes =
            [
                new InputRoute("composer.bold", KeyBinding.Shortcut("b")) { Priority = 1 },
                new InputRoute("track.scroll", WheelBinding.Any) { PreventDefault = false, Propagation = InputPropagation.Continue },
            ],
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(profile, Wire));
        var root = json.RootElement;

        Assert.True(root.GetProperty("modal").GetBoolean());
        Assert.Equal("cancel", root.GetProperty("escape").GetString());
        Assert.Equal("pass-through", root.GetProperty("pressAway").GetString());
        Assert.Equal("keydown", root.GetProperty("boundary")[0].GetString());

        var bold = root.GetProperty("routes")[0];
        Assert.Equal("composer.bold", bold.GetProperty("action").GetString());
        Assert.Equal(1, bold.GetProperty("priority").GetInt32());
        Assert.Equal(JsonValueKind.Null, bold.GetProperty("preventDefault").ValueKind);
        Assert.Equal(JsonValueKind.Null, bold.GetProperty("propagation").ValueKind);

        var binding = bold.GetProperty("bindings")[0];
        Assert.Equal("keyboard", binding.EnumerateObject().First().Name == "family" ? binding.GetProperty("family").GetString() : null);
        Assert.Equal("keydown", binding.GetProperty("type").GetString());
        Assert.Equal("b", binding.GetProperty("key").GetString());
        Assert.True(binding.GetProperty("modifiers").GetProperty("primary").GetBoolean());
        Assert.Equal(JsonValueKind.Null, binding.GetProperty("modifiers").GetProperty("control").ValueKind);
        Assert.False(binding.GetProperty("modifiers").GetProperty("shift").GetBoolean());

        var wheel = root.GetProperty("routes")[1];
        Assert.Equal("wheel", wheel.GetProperty("bindings")[0].GetProperty("family").GetString());
        Assert.Equal(JsonValueKind.Null, wheel.GetProperty("bindings")[0].GetProperty("axis").ValueKind);
        Assert.False(wheel.GetProperty("preventDefault").GetBoolean());
        Assert.Equal("continue", wheel.GetProperty("propagation").GetString());
    }

    [Fact]
    public void Should_serialize_every_binding_family()
    {
        IReadOnlyList<InputBinding> bindings =
        [
            new KeyBinding("Enter") { MatchesRelease = true },
            new PointerBinding { Button = 2, PointerType = PointerType.Touch },
            PointerBinding.Drag,
            new WheelBinding { Axis = WheelAxis.Horizontal, Modifiers = new() { Shift = true } },
            DropBinding.Files,
        ];

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(bindings, Wire));
        var items = json.RootElement.EnumerateArray().ToList();

        Assert.Equal("keyup", items[0].GetProperty("type").GetString());
        Assert.Equal("pointer", items[1].GetProperty("family").GetString());
        Assert.Equal("touch", items[1].GetProperty("pointerType").GetString());
        Assert.Equal(2, items[1].GetProperty("button").GetInt32());
        Assert.Equal("pointermove", items[2].GetProperty("type").GetString());
        Assert.Equal(1, items[2].GetProperty("buttons").GetInt32());
        Assert.Equal("horizontal", items[3].GetProperty("axis").GetString());
        Assert.True(items[3].GetProperty("modifiers").GetProperty("shift").GetBoolean());
        Assert.Equal("drop", items[4].GetProperty("type").GetString());
        Assert.Equal("Files", items[4].GetProperty("data")[0].GetString());
    }

    [Fact]
    public void Should_leave_inherited_ambient_behavior_null()
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(InputProfile.Default, Wire));

        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("escape").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("pressAway").ValueKind);
        Assert.Equal(0, json.RootElement.GetProperty("routes").GetArrayLength());
    }

    [Fact]
    public void Should_serialize_the_host_policy()
    {
        var options = new InputRouterOptions
        {
            PressAway = PressAwayBehavior.Ignore,
            Routes = [new InputRoute("app.palette", KeyBinding.Shortcut("k"))],
            PreemptiveRoutes = [new InputRoute("app.help", new KeyBinding("F1"))],
            Bindings = new Dictionary<string, InputBindingOverride>
            {
                ["composer.bold"] = InputBindingOverride.ReplaceWith(new KeyBinding("b") { Modifiers = new() { Alt = true } }),
                ["composer.italic"] = InputBindingOverride.Unbound,
                ["select.next"] = new() { Add = [new KeyBinding("j")], Remove = [new KeyBinding("ArrowDown")] },
            },
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(options, Wire));
        var root = json.RootElement;

        Assert.Equal("dismiss", root.GetProperty("escape").GetString());
        Assert.Equal("ignore", root.GetProperty("pressAway").GetString());
        Assert.Equal("app.palette", root.GetProperty("routes")[0].GetProperty("action").GetString());
        Assert.Equal("app.help", root.GetProperty("preemptiveRoutes")[0].GetProperty("action").GetString());

        var bindings = root.GetProperty("bindings");
        Assert.True(bindings.GetProperty("composer.bold").GetProperty("replace")[0].GetProperty("modifiers").GetProperty("alt").GetBoolean());
        Assert.Equal(0, bindings.GetProperty("composer.italic").GetProperty("replace").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, bindings.GetProperty("select.next").GetProperty("replace").ValueKind);
        Assert.Equal("j", bindings.GetProperty("select.next").GetProperty("add")[0].GetProperty("key").GetString());
        Assert.Equal("ArrowDown", bindings.GetProperty("select.next").GetProperty("remove")[0].GetProperty("key").GetString());
    }

    [Fact]
    public void Should_read_the_input_the_script_describes_as_its_family()
    {
        const string key = """{"family":"keyboard","type":"keydown","key":"k","code":"KeyK","modifiers":{"control":true,"meta":false,"alt":false,"shift":false,"primary":true}}""";
        const string pointer = """{"family":"pointer","type":"pointerdown","pointerId":3,"pointerType":"touch","button":0,"clientX":10.5,"clientY":20,"modifiers":{"control":false,"meta":false,"alt":false,"shift":false,"primary":false}}""";
        const string wheel = """{"family":"wheel","type":"wheel","deltaX":0,"deltaY":40,"deltaMode":0,"modifiers":{"control":false,"meta":false,"alt":false,"shift":true,"primary":false}}""";
        const string drop = """{"family":"drop","type":"drop","data":["Files"],"clientX":1,"clientY":2}""";

        var keyInfo = Assert.IsType<InputKeyInfo>(JsonSerializer.Deserialize<InputInfo>(key, Wire));
        var pointerInfo = Assert.IsType<InputPointerInfo>(JsonSerializer.Deserialize<InputInfo>(pointer, Wire));
        var wheelInfo = Assert.IsType<InputWheelInfo>(JsonSerializer.Deserialize<InputInfo>(wheel, Wire));
        var dropInfo = Assert.IsType<InputDropInfo>(JsonSerializer.Deserialize<InputInfo>(drop, Wire));

        Assert.Equal("k", keyInfo.Key);
        Assert.True(keyInfo.Modifiers.Primary);
        Assert.Equal(PointerType.Touch, pointerInfo.PointerType);
        Assert.Equal(10.5, pointerInfo.ClientX);
        Assert.Equal(40, wheelInfo.DeltaY);
        Assert.True(wheelInfo.Modifiers.Shift);
        Assert.Equal(["Files"], dropInfo.Data);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    public void Should_reject_a_key_binding_without_a_key(string key)
    {
        Assert.Throws<ArgumentException>(() => new KeyBinding(key));
        Assert.Throws<ArgumentException>(() => KeyBinding.Shortcut(key));
    }

    [Theory]
    [InlineData("next")]
    [InlineData("Composer.Bold")]
    [InlineData("composer.")]
    [InlineData("")]
    public void Should_reject_an_action_not_qualified_by_its_owner(string action)
        => Assert.Throws<ArgumentException>(() => new InputRoute(action, new KeyBinding("a")));

    [Fact]
    public void Should_serialize_the_close_request_behavior_separately()
    {
        var profile = new InputProfile { Escape = DismissBehavior.Ignore, CloseRequest = DismissBehavior.Dismiss };
        var options = new InputRouterOptions { CloseRequest = DismissBehavior.Cancel };

        using var profileJson = JsonDocument.Parse(JsonSerializer.Serialize(profile, Wire));
        using var optionsJson = JsonDocument.Parse(JsonSerializer.Serialize(options, Wire));
        using var defaultJson = JsonDocument.Parse(JsonSerializer.Serialize(InputRouterOptions.Default, Wire));

        Assert.Equal("ignore", profileJson.RootElement.GetProperty("escape").GetString());
        Assert.Equal("dismiss", profileJson.RootElement.GetProperty("closeRequest").GetString());
        Assert.Equal("cancel", optionsJson.RootElement.GetProperty("closeRequest").GetString());
        Assert.Equal(JsonValueKind.Null, defaultJson.RootElement.GetProperty("closeRequest").ValueKind);
    }
}
