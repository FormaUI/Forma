namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Icons;

/// <summary>The composer's own registry of pushed status items and gutter markers.</summary>
public sealed class ComposerRegionRegistry : IComposerRegionRegistry
{
    private readonly List<StatusItem> _statusItems = [];
    private readonly List<GutterMarker> _gutterMarkers = [];

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public IReadOnlyList<IComposerStatusItem> StatusItems
        => _statusItems;

    /// <inheritdoc />
    public IReadOnlyList<IComposerGutterMarker> GutterMarkers
        => _gutterMarkers;

    /// <inheritdoc />
    public IComposerStatusItem CreateStatusItem(string text, string? tooltip = null, string? command = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var item = new StatusItem(this, text, tooltip, command);
        _statusItems.Add(item);
        RaiseChanged();

        return item;
    }

    /// <inheritdoc />
    public IComposerGutterMarker CreateGutterMarker(int block, Icon icon, string? tooltip = null, string? command = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(block);

        var marker = new GutterMarker(this, block, icon, tooltip, command);
        _gutterMarkers.Add(marker);
        RaiseChanged();

        return marker;
    }

    private void RaiseChanged()
        => Changed?.Invoke();

    private sealed class StatusItem(ComposerRegionRegistry owner, string text, string? tooltip, string? command) : IComposerStatusItem
    {
        private string _text = text;
        private string? _tooltip = tooltip;
        private string? _command = command;
        private bool _isAnnounced;
        private bool _isDisposed;

        public string Text
        {
            get => _text;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);

                if (_text == value)
                {
                    return;
                }

                _text = value;
                owner.RaiseChanged();
            }
        }

        public string? Tooltip
        {
            get => _tooltip;
            set
            {
                if (_tooltip == value)
                {
                    return;
                }

                _tooltip = value;
                owner.RaiseChanged();
            }
        }

        public string? Command
        {
            get => _command;
            set
            {
                if (_command == value)
                {
                    return;
                }

                _command = value;
                owner.RaiseChanged();
            }
        }

        public bool IsAnnounced
        {
            get => _isAnnounced;
            set
            {
                if (_isAnnounced == value)
                {
                    return;
                }

                _isAnnounced = value;
                owner.RaiseChanged();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            owner._statusItems.Remove(this);
            owner.RaiseChanged();
        }
    }

    private sealed class GutterMarker(ComposerRegionRegistry owner, int block, Icon icon, string? tooltip, string? command) : IComposerGutterMarker
    {
        private int _block = block;
        private Icon _icon = icon;
        private string? _tooltip = tooltip;
        private string? _command = command;
        private bool _isDisposed;

        public int Block
        {
            get => _block;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                if (_block == value)
                {
                    return;
                }

                _block = value;
                owner.RaiseChanged();
            }
        }

        public Icon Icon
        {
            get => _icon;
            set
            {
                if (_icon == value)
                {
                    return;
                }

                _icon = value;
                owner.RaiseChanged();
            }
        }

        public string? Tooltip
        {
            get => _tooltip;
            set
            {
                if (_tooltip == value)
                {
                    return;
                }

                _tooltip = value;
                owner.RaiseChanged();
            }
        }

        public string? Command
        {
            get => _command;
            set
            {
                if (_command == value)
                {
                    return;
                }

                _command = value;
                owner.RaiseChanged();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            owner._gutterMarkers.Remove(this);
            owner.RaiseChanged();
        }
    }
}
