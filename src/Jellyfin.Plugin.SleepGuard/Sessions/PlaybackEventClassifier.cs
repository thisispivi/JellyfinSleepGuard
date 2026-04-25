using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.SleepGuard.Sessions;

public sealed class PlaybackEventClassifier
{
    private static readonly TimeSpan AutoplayWindow = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LongPauseWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SeekTolerance = TimeSpan.FromSeconds(10);

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
            return tracker.LastActionAtUtc is not null || tracker.LastPausedAtUtc is not null && now - tracker.LastPausedAtUtc.Value > LongPauseWindow
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
            && now - tracker.LastStoppedAtUtc.Value <= AutoplayWindow
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

        if (playbackDelta < -SeekTolerance.Ticks)
        {
            return true;
        }

        if (wallclockTicks == 0)
        {
            return playbackDelta > SeekTolerance.Ticks;
        }

        return playbackDelta > wallclockTicks * 1.5
            && playbackDelta - wallclockTicks > SeekTolerance.Ticks;
    }
}
