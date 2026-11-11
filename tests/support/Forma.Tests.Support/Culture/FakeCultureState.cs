namespace Forma.Tests.Culture;

using System.Globalization;

using Forma.Framework.Culture;

/// <summary>A culture state and its integration that record each accepted pair applied to them, holding the last one.</summary>
/// <param name="culture">The culture the state holds until a pair is applied.</param>
public sealed class FakeCultureState(CultureInfo culture) : ICultureState, ICultureStateIntegration
{
    /// <summary>The pairs applied, in the order they were.</summary>
    public List<AcceptedCulture> AppliedCultures { get; } = [];

    /// <inheritdoc />
    public CultureInfo Culture { get; private set; } = culture;

    /// <inheritdoc />
    public CultureInfo UICulture { get; private set; } = culture;

    /// <inheritdoc />
    public bool IsRightToLeft
        => UICulture.TextInfo.IsRightToLeft;

    /// <inheritdoc />
    public int Revision
        => AppliedCultures.Count;

    /// <inheritdoc />
    public CultureVersion Version { get; private set; } = CultureVersion.Initial;

    /// <inheritdoc />
    public Task ApplyAsync(AcceptedCulture acceptedCulture)
    {
        ArgumentNullException.ThrowIfNull(acceptedCulture);

        AppliedCultures.Add(acceptedCulture);
        Culture = acceptedCulture.Culture;
        UICulture = acceptedCulture.UICulture;
        Version = acceptedCulture.Version;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
    {
    }

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
    {
    }
}
