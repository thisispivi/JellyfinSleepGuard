using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Tests;

public sealed class PlaybackTrackerTests
{
    [Fact]
    public void ProgressAccumulatesContinuousTime()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = new PlaybackTracker(Event(positionTicks: 0), now);

        tracker.ApplyProgress(Event(positionTicks: TimeSpan.FromMinutes(5).Ticks), PlaybackTransition.Tick, now.AddMinutes(5));

        Assert.Equal(TimeSpan.FromMinutes(5), tracker.ContinuousElapsed);
    }

    [Fact]
    public void ManualPauseResetsCounters()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = new PlaybackTracker(Event(positionTicks: 0), now);
        tracker.ApplyProgress(Event(positionTicks: TimeSpan.FromMinutes(20).Ticks), PlaybackTransition.Tick, now.AddMinutes(20));

        tracker.ApplyProgress(Event(paused: true), PlaybackTransition.ManualPause, now.AddMinutes(21));

        Assert.Equal(TimeSpan.Zero, tracker.ContinuousElapsed);
        Assert.Null(tracker.LastActionAtUtc);
    }

    [Fact]
    public void AutoplayIncrementsEpisodeChainWithoutResettingElapsed()
    {
        var now = DateTimeOffset.UtcNow;
        var seriesId = Guid.NewGuid();
        var tracker = new PlaybackTracker(Event(itemId: Guid.NewGuid(), seriesId: seriesId), now);
        tracker.ApplyProgress(Event(seriesId: seriesId, positionTicks: TimeSpan.FromMinutes(30).Ticks), PlaybackTransition.Tick, now.AddMinutes(30));
        tracker.ApplyStopped(now.AddMinutes(31));

        tracker.ApplyStart(Event(itemId: Guid.NewGuid(), seriesId: seriesId), PlaybackTransition.AutoplayNext, now.AddMinutes(31).AddSeconds(2));

        Assert.Equal(2, tracker.EpisodesInChain);
        Assert.True(tracker.ContinuousElapsed > TimeSpan.FromMinutes(30));
    }

    private static PlaybackEvent Event(
        Guid? itemId = null,
        Guid? seriesId = null,
        long? positionTicks = null,
        bool paused = false)
    {
        return new PlaybackEvent(
            "session-1",
            Guid.NewGuid(),
            "device-1",
            itemId ?? Guid.NewGuid(),
            BaseItemKind.Episode,
            seriesId ?? Guid.NewGuid(),
            positionTicks,
            paused);
    }
}
