namespace Forma.Tests.Culture;

using System.Globalization;

using Forma.Framework.Culture;

/// <summary>A culture provider whose culture for each name writes times on a 24-hour clock with seconds, as an application's own patterns do, recording each name asked for.</summary>
public sealed class FakeCultureProvider : ICultureProvider
{
    /// <summary>The time pattern each provided culture writes short times in.</summary>
    public const string ShortTimePattern = "HH:mm:ss";

    private readonly Dictionary<string, CultureInfo> _cultures = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The names asked for, in order.</summary>
    public List<string> Requested { get; } = [];

    /// <summary>The name of the culture the provider returns in place of each one asked for, as a provider that confuses its cultures does.</summary>
    public string? ReturnedName { get; set; }

    /// <inheritdoc />
    public CultureInfo GetCulture(string name)
    {
        Requested.Add(name);

        var key = ReturnedName ?? name;

        if (!_cultures.TryGetValue(key, out var culture))
        {
            var clone = (CultureInfo)CultureInfo.GetCultureInfo(key).Clone();
            clone.DateTimeFormat.ShortTimePattern = ShortTimePattern;
            culture = CultureInfo.ReadOnly(clone);
            _cultures[key] = culture;
        }

        return culture;
    }
}
