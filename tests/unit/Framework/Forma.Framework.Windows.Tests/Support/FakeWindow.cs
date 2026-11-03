namespace Forma.Framework.Windows.Tests.Support;

using Forma.Framework.Icons;
using Forma.Framework.Windows;

/// <summary>A window whose state a test sets directly and that records everything a manager asks of it.</summary>
public sealed class FakeWindow(string title, WindowOrigin? origin = null) : IManagedWindow
{
    /// <summary>Every request the manager made, in order: "active:true", "minimized:false", "hidden:true", "raise", "request-close", "close".</summary>
    public List<string> Log { get; } = [];

    /// <inheritdoc />
    public Guid RuntimeId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public string Title { get; set; } = title;

    /// <inheritdoc />
    public Icon? Icon { get; set; }

    /// <inheritdoc />
    public WindowOrigin Origin { get; set; } = origin ?? WindowOrigin.NewInstance("test", "Test");

    /// <inheritdoc />
    public IWindow? Parent { get; set; }

    /// <inheritdoc />
    public bool IsMinimized { get; set; }

    /// <inheritdoc />
    public bool IsDisabled { get; set; }

    /// <inheritdoc />
    public bool IsModal { get; set; }

    /// <inheritdoc />
    public bool IsFloating { get; set; } = true;

    /// <summary>Whether the manager has made this the active window.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Whether the manager has hidden this window under a minimized ancestor.</summary>
    public bool IsHidden { get; private set; }

    /// <summary>Whether the manager has barred this window behind a modal.</summary>
    public bool IsBlocked { get; private set; }

    /// <summary>Whether the manager has closed this window.</summary>
    public bool IsClosed { get; private set; }

    /// <summary>What the window answers when asked whether it may close.</summary>
    public bool AcceptsClose { get; set; } = true;

    /// <summary>Runs while the window is being asked whether it may close, before it answers.</summary>
    public Func<ValueTask>? WhileClosing { get; set; }

    /// <summary>Runs while the window is being raised.</summary>
    public Func<ValueTask>? WhileRaising { get; set; }

    /// <summary>How many times the window was asked whether it may close.</summary>
    public int CloseRequests { get; private set; }

    /// <inheritdoc />
    public ValueTask SetActiveAsync(bool active)
    {
        IsActive = active;
        Log.Add($"active:{active}");

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetMinimizedAsync(bool minimized)
    {
        IsMinimized = minimized;
        Log.Add($"minimized:{minimized}");

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetHiddenAsync(bool hidden)
    {
        IsHidden = hidden;
        Log.Add($"hidden:{hidden}");

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SetBlockedAsync(bool blocked)
    {
        IsBlocked = blocked;
        Log.Add($"blocked:{blocked}");

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask RaiseAsync()
    {
        Log.Add("raise");

        if (WhileRaising is { } whileRaising)
        {
            await whileRaising().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> RequestCloseAsync()
    {
        CloseRequests++;
        Log.Add("request-close");

        if (WhileClosing is { } whileClosing)
        {
            await whileClosing().ConfigureAwait(false);
        }

        return AcceptsClose;
    }

    /// <inheritdoc />
    public ValueTask PerformCloseAsync()
    {
        IsClosed = true;
        Log.Add("close");

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public override string ToString()
        => Title;
}
