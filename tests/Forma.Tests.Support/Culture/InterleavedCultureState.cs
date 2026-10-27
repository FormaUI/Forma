namespace Forma.Tests.Culture;

using System.Globalization;

using Forma.Framework.Culture;

/// <summary>A culture state that changes to another culture right after its culture is first read, as a change made on another thread lands between a reader's reads.</summary>
/// <param name="culture">The culture the state holds until its culture is first read.</param>
/// <param name="changedCulture">The culture the state holds from then on.</param>
public sealed class InterleavedCultureState(CultureInfo culture, CultureInfo changedCulture) : ICultureState
{
    private CultureInfo _culture = culture;
    private CultureInfo _uiCulture = culture;
    private bool _hasChanged;

    /// <inheritdoc />
    /// <remarks>The first read returns the culture as it stood and then makes the change, cultures first and revision after.</remarks>
    public CultureInfo Culture
    {
        get
        {
            var current = _culture;

            if (!_hasChanged)
            {
                _hasChanged = true;
                _culture = changedCulture;
                _uiCulture = changedCulture;
                Revision++;
            }

            return current;
        }
    }

    /// <inheritdoc />
    public CultureInfo UICulture
        => _uiCulture;

    /// <inheritdoc />
    public bool IsRightToLeft
        => _uiCulture.TextInfo.IsRightToLeft;

    /// <inheritdoc />
    public int Revision { get; private set; }

    /// <inheritdoc />
    public Task ChangeAsync(CultureInfo culture, CultureInfo? uiCulture = null)
        => throw new NotSupportedException("The state changes only as its culture is first read.");

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
    {
    }

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
    {
    }
}
