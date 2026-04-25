using System.Collections.Concurrent;

namespace Jellyfin.Plugin.SleepGuard.Sessions;

public sealed class PlaybackTrackerStore : IDisposable
{
    private readonly ConcurrentDictionary<string, PlaybackTracker> _trackers = new(StringComparer.Ordinal);

    public PlaybackTracker GetOrAdd(PlaybackEvent playbackEvent, DateTimeOffset now)
    {
        return _trackers.GetOrAdd(playbackEvent.SessionId, _ => new PlaybackTracker(playbackEvent, now));
    }

    public bool TryGet(string sessionId, out PlaybackTracker? tracker)
    {
        return _trackers.TryGetValue(sessionId, out tracker);
    }

    public bool Remove(string sessionId)
    {
        return _trackers.TryRemove(sessionId, out _);
    }

    public void Dispose()
    {
        _trackers.Clear();
    }
}
