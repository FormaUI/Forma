namespace Forma.UI.Components.Feedback;

using Forma.Framework.Feedback;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaToastContainer : IDisposable
{
    private readonly Dictionary<string, Timer> _timers = [];

    [Inject] private IToastService ToastService { get; set; } = default!;

    [Inject] private IStringLocale<FormaToastContainer> L { get; set; } = default!;

    protected override void OnInitialized() => ToastService.Subscribe(HandleChangeAsync);

    private static string MarkerFor(ToastType type)
        => type switch
        {
            ToastType.Success => "\u2713",
            ToastType.Error => "\u2715",
            ToastType.Warning => "\u26A0",
            _ => "\u2139"
        };

    private Task HandleChangeAsync()
    {
        return InvokeAsync(() =>
        {
            CancelTimersForRemovedToasts();
            ScheduleAutoRemoval();
            StateHasChanged();
        });
    }

    private void ScheduleAutoRemoval()
    {
        foreach (var toast in ToastService.Toasts)
        {
            if (_timers.ContainsKey(toast.Id))
            {
                continue;
            }

            var id = toast.Id;
            var timer = new Timer(_ =>
            {
                _ = InvokeAsync(async () =>
                {
                    DisposeTimer(id);
                    await ToastService.RemoveAsync(id);
                });
            }, null, toast.Duration, Timeout.InfiniteTimeSpan);

            _timers[id] = timer;
        }
    }

    private void CancelTimersForRemovedToasts()
    {
        var activeIds = ToastService.Toasts.Select(t => t.Id).ToHashSet();
        var staleIds = _timers.Keys.Where(id => !activeIds.Contains(id)).ToList();

        foreach (var id in staleIds)
        {
            DisposeTimer(id);
        }
    }

    private void DisposeTimer(string id)
    {
        if (_timers.Remove(id, out var timer))
        {
            timer.Dispose();
        }
    }

    public void Dispose()
    {
        ToastService.Unsubscribe(HandleChangeAsync);

        foreach (var timer in _timers.Values)
        {
            timer.Dispose();
        }

        _timers.Clear();
    }
}
