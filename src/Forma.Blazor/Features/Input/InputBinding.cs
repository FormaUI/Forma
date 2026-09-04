namespace Forma.Blazor.Features.Input;

using System.Text.Json.Serialization;

using Forma.Blazor.Features.Input.Drop;
using Forma.Blazor.Features.Input.Keyboard;
using Forma.Blazor.Features.Input.Pointer;
using Forma.Blazor.Features.Input.Wheel;

/// <summary>A binding: the input a route is raised by, matched by the rules of its family.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "family")]
[JsonDerivedType(typeof(KeyBinding), "keyboard")]
[JsonDerivedType(typeof(PointerBinding), "pointer")]
[JsonDerivedType(typeof(WheelBinding), "wheel")]
[JsonDerivedType(typeof(DropBinding), "drop")]
public abstract record InputBinding
{
    /// <summary>The DOM event type the binding matches.</summary>
    public abstract string Type { get; }
}
