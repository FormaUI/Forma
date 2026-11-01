namespace Forma.Tests.Maui;

using Microsoft.Maui.Devices;

/// <summary>A device of the platform the test names.</summary>
/// <param name="platform">The device's platform.</param>
public sealed class FakeDeviceInfo(DevicePlatform platform) : IDeviceInfo
{
    /// <inheritdoc />
    public string Model
        => "Test";

    /// <inheritdoc />
    public string Manufacturer
        => "Forma";

    /// <inheritdoc />
    public string Name
        => "Test device";

    /// <inheritdoc />
    public string VersionString
        => "1.0";

    /// <inheritdoc />
    public Version Version
        => new(1, 0);

    /// <inheritdoc />
    public DevicePlatform Platform
        => platform;

    /// <inheritdoc />
    public DeviceIdiom Idiom
        => DeviceIdiom.Phone;

    /// <inheritdoc />
    public DeviceType DeviceType
        => DeviceType.Virtual;
}
