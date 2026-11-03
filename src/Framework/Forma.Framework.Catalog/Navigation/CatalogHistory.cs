namespace Forma.Framework.Catalog.Navigation;

/// <summary>The history of the folders a surface has visited, with a cursor that moves back and forward along it.</summary>
public sealed class CatalogHistory
{
    private readonly List<string?> _trail = [null];
    private int _index;

    /// <summary>Whether a step lies behind the cursor.</summary>
    public bool CanGoBack
        => _index > 0;

    /// <summary>Whether a step lies ahead of the cursor.</summary>
    public bool CanGoForward
        => _index < _trail.Count - 1;

    /// <summary>The source of the folder at the cursor, where null is the top level.</summary>
    public string? Current
        => _trail[_index];

    /// <summary>Records a visit to the folder, dropping whatever lay ahead of the cursor.</summary>
    public void Visit(string? folderSource)
    {
        if (CanGoForward)
        {
            _trail.RemoveRange(_index + 1, _trail.Count - (_index + 1));
        }

        _trail.Add(folderSource);
        _index = _trail.Count - 1;
    }

    /// <summary>Moves the cursor one step back.</summary>
    public void GoBack()
    {
        if (CanGoBack)
        {
            _index--;
        }
    }

    /// <summary>Moves the cursor one step forward.</summary>
    public void GoForward()
    {
        if (CanGoForward)
        {
            _index++;
        }
    }
}
