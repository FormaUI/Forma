namespace Forma.Features.Files;

/// <summary>A zoom that moves along a fixed run of percentage steps, resting on the first.</summary>
public sealed class SteppedZoom
{
    private readonly IReadOnlyList<int> _steps;
    private int _index;

    public SteppedZoom(IReadOnlyList<int> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentOutOfRangeException.ThrowIfZero(steps.Count);

        _steps = steps;
    }

    /// <summary>The percentage the zoom stands on.</summary>
    public int Percent
        => _steps[_index];

    /// <summary>Whether the zoom has left its resting step.</summary>
    public bool IsZoomed
        => _index > 0;

    /// <summary>Whether no step lies below the current one.</summary>
    public bool AtMinimum
        => _index == 0;

    /// <summary>Whether no step lies above the current one.</summary>
    public bool AtMaximum
        => _index == _steps.Count - 1;

    /// <summary>Moves one step in, reporting whether it moved.</summary>
    public bool In()
        => MoveTo(_index + 1);

    /// <summary>Moves one step out, reporting whether it moved.</summary>
    public bool Out()
        => MoveTo(_index - 1);

    /// <summary>Returns the zoom to its resting step.</summary>
    public void Reset()
        => _index = 0;

    private bool MoveTo(int index)
    {
        if (index < 0 || index >= _steps.Count || index == _index)
        {
            return false;
        }

        _index = index;

        return true;
    }
}
