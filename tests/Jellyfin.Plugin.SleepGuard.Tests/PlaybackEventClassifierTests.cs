using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Tests;

public sealed class PlaybackEventClassifierTests
{
    private readonly PlaybackEventClassifier _classifier = new();

    [Fact]
    public void DetectsForwardSeekWhenPositionOutrunsWallclock()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = new PlaybackTracker(Event(positionTicks: 0), now);

        var transition = _classifier.ClassifyProgress(tracker, Event(positionTicks: TimeSpan.FromMinutes(30).Ticks), now.AddSeconds(30));

        Assert.Equal(PlaybackTransition.Seek, transition);
    }

    [Fact]
    public void DetectsSelfPauseWhenSuppressionIsActive()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = new PlaybackTracker(Event(positionTicks: 0), now);
        tracker.MarkActionIssued(now, suppressPauseEvent: true);

        var transition = _classifier.ClassifyProgress(tracker, Event(positionTicks: TimeSpan.FromSeconds(5).Ticks, paused: true), now.AddSeconds(5));

        Assert.Equal(PlaybackTransition.SelfPause, transition);
    }

    [Fact]
    public void ResumeAfterSleepGuardPauseCountsAsManualResume()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = new PlaybackTracker(Event(positionTicks: 0), now);
        tracker.MarkActionIssued(now, suppressPauseEvent: true);
        tracker.ApplyProgress(Event(positionTicks: TimeSpan.FromSeconds(5).Ticks, paused: true), PlaybackTransition.SelfPause, now.AddSeconds(5));

        var transition = _classifier.ClassifyProgress(tracker, Event(positionTicks: TimeSpan.FromSeconds(6).Ticks), now.AddSeconds(6));

        Assert.Equal(PlaybackTransition.ManualResume, transition);
    }

    [Fact]
    public void DetectsAutoplayNextEpisodeInSameSeries()
    {
        var now = DateTimeOffset.UtcNow;
        var seriesId = Guid.NewGuid();
        var tracker = new PlaybackTracker(Event(itemId: Guid.NewGuid(), seriesId: seriesId), now);
        tracker.ApplyStopped(now.AddMinutes(45));

        var transition = _classifier.ClassifyStart(tracker, Event(itemId: Guid.NewGuid(), seriesId: seriesId), now.AddMinutes(45).AddSeconds(5));

        Assert.Equal(PlaybackTransition.AutoplayNext, transition);
    }

    [Fact]
    public void DifferentSeriesAfterStopIsManualSwitch()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = new PlaybackTracker(Event(seriesId: Guid.NewGuid()), now);
        tracker.ApplyStopped(now.AddMinutes(45));

        var transition = _classifier.ClassifyStart(tracker, Event(seriesId: Guid.NewGuid()), now.AddMinutes(45).AddSeconds(5));

        Assert.Equal(PlaybackTransition.ManualSwitch, transition);
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
