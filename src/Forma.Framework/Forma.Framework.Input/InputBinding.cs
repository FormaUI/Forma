namespace Forma.Framework.Input;

using System.Text.Json.Serialization;

using Forma.Framework.Input.Click;
using Forma.Framework.Input.Drop;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Input.Pointer;
using Forma.Framework.Input.Wheel;

/// <summary>A binding: the input a route is raised by, matched by the rules of its family.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "family")]
[JsonDerivedType(typeof(KeyBinding), "keyboard")]
[JsonDerivedType(typeof(TypingBinding), "typing")]
[JsonDerivedType(typeof(PointerBinding), "pointer")]
[JsonDerivedType(typeof(ClickBinding), "click")]
[JsonDerivedType(typeof(WheelBinding), "wheel")]
[JsonDerivedType(typeof(DropBinding), "drop")]
public abstract record InputBinding
{
    /// <summary>The DOM event type the binding matches.</summary>
    public abstract string Type { get; }
}
