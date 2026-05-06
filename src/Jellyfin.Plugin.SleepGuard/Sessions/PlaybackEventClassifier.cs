using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.SleepGuard.Sessions;

/// <summary>
/// Classifies raw playback events into typed <see cref="PlaybackTransition"/> values
/// by comparing the incoming event against the current tracker state.
/// </summary>
public sealed class PlaybackEventClassifier
{
    /// <summary>
    /// Classifies a playback-start event (fired when a new item begins playing).
    /// </summary>
    public PlaybackTransition ClassifyStart(PlaybackTracker? tracker, PlaybackEvent playbackEvent, DateTimeOffset now)
    {
        if (tracker is null || tracker.NowPlayingItemId is null)
        {
            return PlaybackTransition.StartNew;
        }

        if (IsAutoplayNext(tracker, playbackEvent, now))
        {
            return PlaybackTransition.AutoplayNext;
        }

        if (tracker.NowPlayingItemId == playbackEvent.ItemId)
        {
            return PlaybackTransition.Tick;
        }

        return PlaybackTransition.ManualSwitch;
    }

    /// <summary>
    /// Classifies a playback-progress event (periodic tick, pause, resume, or seek).
    /// </summary>
    public PlaybackTransition ClassifyProgress(PlaybackTracker tracker, PlaybackEvent playbackEvent, DateTimeOffset now)
    {
        if (playbackEvent.IsPaused && !tracker.IsPaused)
        {
            return tracker.SuppressNextPauseEventUntilUtc >= now
                ? PlaybackTransition.SelfPause
                : PlaybackTransition.ManualPause;
        }

        if (!playbackEvent.IsPaused && tracker.IsPaused)
        {
            return tracker.LastActionAtUtc is not null
                || (tracker.LastPausedAtUtc is not null && now - tracker.LastPausedAtUtc.Value > SessionConstants.LongPauseWindow)
                ? PlaybackTransition.ManualResume
                : PlaybackTransition.Tick;
        }

        if (IsSeek(tracker, playbackEvent, now))
        {
            return PlaybackTransition.Seek;
        }

        return PlaybackTransition.Tick;
    }

    private static bool IsAutoplayNext(PlaybackTracker tracker, PlaybackEvent playbackEvent, DateTimeOffset now)
    {
        return tracker.LastStoppedAtUtc is not null
            && now - tracker.LastStoppedAtUtc.Value <= SessionConstants.AutoplayWindow
            && tracker.ItemKind == BaseItemKind.Episode
            && playbackEvent.ItemKind == BaseItemKind.Episode
            && tracker.SeriesId is not null
            && tracker.SeriesId == playbackEvent.SeriesId
            && tracker.NowPlayingItemId != playbackEvent.ItemId;
    }

    private static bool IsSeek(PlaybackTracker tracker, PlaybackEvent playbackEvent, DateTimeOffset now)
    {
        if (tracker.LastPositionTicks is null || playbackEvent.PositionTicks is null || tracker.LastTickUtc is null)
        {
            return false;
        }

        var wallclockTicks = Math.Max(0, (now - tracker.LastTickUtc.Value).Ticks);
        var playbackDelta = playbackEvent.PositionTicks.Value - tracker.LastPositionTicks.Value;

        if (playbackDelta < -SessionConstants.SeekTolerance.Ticks)
        {
            return true;
        }

        if (wallclockTicks == 0)
        {
            return playbackDelta > SessionConstants.SeekTolerance.Ticks;
        }

        return playbackDelta > wallclockTicks * 1.5
            && playbackDelta - wallclockTicks > SessionConstants.SeekTolerance.Ticks;
    }
}
