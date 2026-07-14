using System.Diagnostics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class SharedAnimationClock
{
    private static readonly object Sync = new();
    private static readonly Dictionary<long, Action<double>> Subscribers = new();
    private static IDispatcherTimer? _timer;
    private static long _nextId;
    private static long _startedAt;

    public static IDisposable Subscribe(Action<double> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var id = Interlocked.Increment(ref _nextId);
        lock (Sync) Subscribers[id] = callback;
        EnsureStarted();
        return new Subscription(id);
    }

    private static void EnsureStarted()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            lock (Sync)
            {
                if (_timer?.IsRunning == true || Subscribers.Count == 0)
                    return;
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null)
                    return;
                _startedAt = Stopwatch.GetTimestamp();
                _timer ??= dispatcher.CreateTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(33);
                _timer.IsRepeating = true;
                _timer.Tick -= OnTick;
                _timer.Tick += OnTick;
                _timer.Start();
            }
        });
    }

    private static void OnTick(object? sender, EventArgs e)
    {
        Action<double>[] snapshot;
        lock (Sync)
        {
            if (Subscribers.Count == 0)
            {
                _timer?.Stop();
                return;
            }
            snapshot = Subscribers.Values.ToArray();
        }
        var elapsed = Stopwatch.GetElapsedTime(_startedAt).TotalSeconds;
        foreach (var callback in snapshot)
        {
            try { callback(elapsed); }
            catch { /* A visual must not stop the shared clock. */ }
        }
    }

    private static void Unsubscribe(long id)
    {
        lock (Sync)
        {
            Subscribers.Remove(id);
            if (Subscribers.Count == 0)
                _timer?.Stop();
        }
    }

    private sealed class Subscription(long id) : IDisposable
    {
        private long _id = id;
        public void Dispose()
        {
            var value = Interlocked.Exchange(ref _id, 0);
            if (value != 0) Unsubscribe(value);
        }
    }
}
