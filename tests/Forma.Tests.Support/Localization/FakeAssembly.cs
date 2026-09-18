namespace Forma.Tests.Localization;

using System.Globalization;
using System.Reflection;
using System.Text;

/// <summary>An assembly whose manifest resources are held in memory, with a satellite assembly per culture holding its own.</summary>
public sealed class FakeAssembly : Assembly
{
    private readonly string _name;

    /// <summary>Creates an assembly with the given name and no resources.</summary>
    /// <param name="name">The assembly's simple name.</param>
    public FakeAssembly(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        _name = name;
    }

    /// <summary>The manifest resources, each keyed by its name and holding its text.</summary>
    public Dictionary<string, string> Resources { get; } = new(StringComparer.Ordinal);

    /// <summary>The satellite assemblies, each keyed by the culture it is for.</summary>
    public Dictionary<string, FakeAssembly> Satellites { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public override string FullName
        => GetName().FullName;

    /// <inheritdoc />
    public override AssemblyName GetName()
        => new(_name);

    /// <inheritdoc />
    public override AssemblyName GetName(bool copiedName)
        => GetName();

    /// <inheritdoc />
    public override string[] GetManifestResourceNames()
        => [.. Resources.Keys];

    /// <inheritdoc />
    public override Stream? GetManifestResourceStream(string name)
        => Resources.TryGetValue(name, out var content) ? new MemoryStream(Encoding.UTF8.GetBytes(content), writable: false) : null;

    /// <inheritdoc />
    public override Assembly GetSatelliteAssembly(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        return Satellites.TryGetValue(culture.Name, out var satellite)
            ? satellite
            : throw new FileNotFoundException($"'{_name}' has no satellite assembly for '{culture.Name}'.");
    }

    /// <inheritdoc />
    public override Assembly GetSatelliteAssembly(CultureInfo culture, Version? version)
        => GetSatelliteAssembly(culture);

    /// <inheritdoc />
    public override bool Equals(object? o)
        => ReferenceEquals(this, o);

    /// <inheritdoc />
    public override int GetHashCode()
        => _name.GetHashCode(StringComparison.Ordinal);
}
