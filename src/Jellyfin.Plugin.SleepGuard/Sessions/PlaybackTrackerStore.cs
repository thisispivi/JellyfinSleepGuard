using System.Collections.Concurrent;

namespace Jellyfin.Plugin.SleepGuard.Sessions;

/// <summary>
/// Thread-safe in-memory store of active <see cref="PlaybackTracker"/> instances, keyed by Jellyfin session ID.
/// </summary>
public sealed class PlaybackTrackerStore : IDisposable
{
    private readonly ConcurrentDictionary<string, PlaybackTracker> _trackers = new(StringComparer.Ordinal);

    /// <summary>
    /// Returns the existing tracker for <paramref name="playbackEvent"/>'s session,
    /// or creates and inserts a new one if none exists.
    /// </summary>
    /// <param name="playbackEvent">Event whose session is looked up; also seeds a new tracker.</param>
    /// <param name="now">Current UTC time.</param>
    /// <returns>The tracker for the session.</returns>
    public PlaybackTracker GetOrAdd(PlaybackEvent playbackEvent, DateTimeOffset now)
    {
        return _trackers.GetOrAdd(playbackEvent.SessionId, _ => new PlaybackTracker(playbackEvent, now));
    }

    /// <summary>
    /// Tries to retrieve an existing tracker without creating one.
    /// </summary>
    /// <param name="sessionId">Jellyfin session ID.</param>
    /// <param name="tracker">The tracker, when found.</param>
    /// <returns><c>true</c> when a tracker exists for the session.</returns>
    public bool TryGet(string sessionId, out PlaybackTracker? tracker)
    {
        return _trackers.TryGetValue(sessionId, out tracker);
    }

    /// <summary>
    /// Removes a tracker when a session ends cleanly.
    /// </summary>
    /// <param name="sessionId">Jellyfin session ID.</param>
    /// <returns><c>true</c> when a tracker was removed.</returns>
    public bool Remove(string sessionId)
    {
        return _trackers.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Removes all trackers whose <see cref="PlaybackTracker.LastAccessedUtc"/> is earlier than
    /// <paramref name="cutoff"/>. Called periodically to clean up orphaned sessions that ended
    /// without triggering <c>SessionEnded</c> (e.g., server restarts, network drops).
    /// </summary>
    /// <param name="cutoff">Trackers last accessed before this instant are removed.</param>
    /// <returns>The number of trackers evicted.</returns>
    public int EvictBefore(DateTimeOffset cutoff)
    {
        var evicted = 0;
        foreach (var (key, tracker) in _trackers)
        {
            if (tracker.LastAccessedUtc < cutoff && _trackers.TryRemove(key, out _))
            {
                evicted++;
            }
        }

        return evicted;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _trackers.Clear();
    }
}
