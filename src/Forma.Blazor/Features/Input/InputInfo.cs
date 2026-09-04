namespace Forma.Blazor.Features.Input;

using System.Text.Json.Serialization;

using Forma.Blazor.Features.Input.Drop;
using Forma.Blazor.Features.Input.Keyboard;
using Forma.Blazor.Features.Input.Pointer;
using Forma.Blazor.Features.Input.Wheel;

/// <summary>The input that raised an action, as its family describes it.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "family")]
[JsonDerivedType(typeof(InputKeyInfo), "keyboard")]
[JsonDerivedType(typeof(InputPointerInfo), "pointer")]
[JsonDerivedType(typeof(InputWheelInfo), "wheel")]
[JsonDerivedType(typeof(InputDropInfo), "drop")]
public abstract record InputInfo
{
    /// <summary>The DOM event type.</summary>
    public required string Type { get; init; }
}
