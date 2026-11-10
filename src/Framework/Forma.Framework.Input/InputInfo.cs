namespace Forma.Framework.Input;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

using Forma.Framework.Input.Drop;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.Input.Pointer;
using Forma.Framework.Input.Wheel;

/// <summary>The input that raised an action, as its family describes it.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "family")]
[JsonDerivedType(typeof(InputKeyInfo), "keyboard")]
[JsonDerivedType(typeof(InputPointerInfo), "pointer")]
[JsonDerivedType(typeof(InputWheelInfo), "wheel")]
[JsonDerivedType(typeof(InputDropInfo), "drop")]
public abstract record InputInfo
{
    /// <summary>The DOM event type.</summary>
    public required string Type { get; init; }

    /// <summary>The item the input landed on, as the attribute the profile's <see cref="InputProfile.ItemAttribute"/> names reads it.</summary>
    public string? Item { get; init; }
}
