namespace Forma.Framework.Browser.Interop.Text;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;

/// <summary>A text field's entry: the running count of its characters, kept in the browser as the reader types.</summary>
public sealed class DomText
{
    private static readonly DomCapability<CharacterCountOptions> Counting = new(DomRoot.Engine, "text", DescribeCount);

    private readonly DomWatcher _watcher;
    private readonly DomEngine? _engine;
    private readonly ElementReference _element;

    internal DomText(DomWatcher watcher, DomEngine? engine, ElementReference element)
    {
        _watcher = watcher;
        _engine = engine;
        _element = element;
    }

    /// <summary>Attaches the running count of the field's characters, shown as <paramref name="options"/> sets out and held while <paramref name="shouldEnable"/>.</summary>
    /// <exception cref="ArgumentException">The options name an empty output.</exception>
    /// <exception cref="InvalidOperationException">The element was got from a watcher.</exception>
    public async Task<DomInteraction<CharacterCountOptions>> AttachAsync(CharacterCountOptions options, bool shouldEnable = true)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputId, nameof(options));

        if (options.MaxLength is { } maxLength)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1, nameof(options));
        }

        var engine = _engine ?? throw new InvalidOperationException("An element got from a watcher observes alone; get it from an engine to act on it.");
        var interaction = new DomInteraction<CharacterCountOptions>(_watcher.Session, Counting, engine.IsDisposed);
        await interaction.SyncAsync(_element, options, shouldEnable);

        return interaction;
    }

    private static CountDescription DescribeCount(CharacterCountOptions options)
        => new() { Output = options.OutputId, MaxLength = options.MaxLength };

    /// <summary>The count as the browser reads it.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record CountDescription
    {
        public required string Output { get; init; }

        public required int? MaxLength { get; init; }
    }
}
