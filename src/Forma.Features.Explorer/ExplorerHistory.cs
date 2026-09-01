namespace Forma.Features.Explorer;

/// <summary>The trail of folders one explorer has visited, with a cursor moving back and forward along it.</summary>
public sealed class ExplorerHistory
{
    private readonly List<string?> _trail = [null];
    private int _index;

    /// <summary>Whether a step lies behind the cursor.</summary>
    public bool CanGoBack
        => _index > 0;

    /// <summary>Whether a step lies ahead of the cursor.</summary>
    public bool CanGoForward
        => _index < _trail.Count - 1;

    /// <summary>The source of the folder the cursor stands on, or null for all media.</summary>
    public string? Current
        => _trail[_index];

    /// <summary>Records a newly opened folder, dropping whatever lay ahead of the cursor.</summary>
    public void Visit(string? folderSource)
    {
        if (CanGoForward)
        {
            _trail.RemoveRange(_index + 1, _trail.Count - (_index + 1));
        }

        _trail.Add(folderSource);
        _index = _trail.Count - 1;
    }

    /// <summary>Moves the cursor one step back, staying put when nothing lies behind it.</summary>
    public void GoBack()
    {
        if (CanGoBack)
        {
            _index--;
        }
    }

    /// <summary>Moves the cursor one step forward, staying put when nothing lies ahead of it.</summary>
    public void GoForward()
    {
        if (CanGoForward)
        {
            _index++;
        }
    }
}
