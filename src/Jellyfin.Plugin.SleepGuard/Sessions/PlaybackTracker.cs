using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.SleepGuard.Sessions;

public sealed class PlaybackTracker
{
    public PlaybackTracker(PlaybackEvent playbackEvent, DateTimeOffset now)
    {
        SessionId = playbackEvent.SessionId;
        ApplyNewChain(playbackEvent, now);
    }

    public string SessionId { get; }

    public Guid UserId { get; private set; }

    public string? DeviceId { get; private set; }

    public Guid? NowPlayingItemId { get; private set; }

    public BaseItemKind ItemKind { get; private set; }

    public Guid? SeriesId { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public TimeSpan ContinuousElapsed { get; private set; }

    public int EpisodesInChain { get; private set; }

    public DateTimeOffset? LastTickUtc { get; private set; }

    public long? LastPositionTicks { get; private set; }

    public DateTimeOffset? LastUserActionUtc { get; private set; }

    public DateTimeOffset? LastPausedAtUtc { get; private set; }

    public DateTimeOffset? LastStoppedAtUtc { get; private set; }

    public DateTimeOffset? PendingPromptUntilUtc { get; private set; }

    public DateTimeOffset? SuppressNextPauseEventUntilUtc { get; private set; }

    public DateTimeOffset? LastActionAtUtc { get; private set; }

    public bool IsPaused { get; private set; }

    public bool HasPendingOrCompletedAction => PendingPromptUntilUtc is not null || LastActionAtUtc is not null;

    public void ApplyStart(PlaybackEvent playbackEvent, PlaybackTransition transition, DateTimeOffset now)
    {
        switch (transition)
        {
            case PlaybackTransition.AutoplayNext:
                UpdateIdentity(playbackEvent);
                EpisodesInChain++;
                IsPaused = playbackEvent.IsPaused;
                LastPausedAtUtc = playbackEvent.IsPaused ? now : null;
                LastStoppedAtUtc = null;
                LastTickUtc = now;
                LastPositionTicks = playbackEvent.PositionTicks;
                break;
            case PlaybackTransition.Tick:
                ApplyProgress(playbackEvent, transition, now);
                break;
            default:
                LastUserActionUtc = now;
                ApplyNewChain(playbackEvent, now);
                break;
        }
    }

    public void ApplyProgress(PlaybackEvent playbackEvent, PlaybackTransition transition, DateTimeOffset now)
    {
        if (transition is PlaybackTransition.Seek or PlaybackTransition.ManualPause or PlaybackTransition.ManualResume or PlaybackTransition.ManualSwitch)
        {
            LastUserActionUtc = now;
            ResetCounters(now);
        }
        else
        {
            AddElapsedUntil(now);
        }

        UpdateIdentity(playbackEvent);
        IsPaused = playbackEvent.IsPaused;
        LastPausedAtUtc = playbackEvent.IsPaused ? now : null;
        LastStoppedAtUtc = null;
        LastTickUtc = now;
        LastPositionTicks = playbackEvent.PositionTicks;

        if (transition == PlaybackTransition.SelfPause)
        {
            SuppressNextPauseEventUntilUtc = null;
        }
    }

    public void ApplyStopped(DateTimeOffset now)
    {
        AddElapsedUntil(now);
        LastStoppedAtUtc = now;
        LastTickUtc = now;
        LastPositionTicks = null;
    }

    public void MarkPromptPending(DateTimeOffset untilUtc)
    {
        PendingPromptUntilUtc = untilUtc;
    }

    public void ClearPromptPending()
    {
        PendingPromptUntilUtc = null;
    }

    public void MarkActionIssued(DateTimeOffset now, bool suppressPauseEvent)
    {
        LastActionAtUtc = now;
        PendingPromptUntilUtc = null;
        if (suppressPauseEvent)
        {
            SuppressNextPauseEventUntilUtc = now.AddSeconds(15);
        }
    }

    private void ApplyNewChain(PlaybackEvent playbackEvent, DateTimeOffset now)
    {
        UpdateIdentity(playbackEvent);
        StartedAtUtc = now;
        ContinuousElapsed = TimeSpan.Zero;
        EpisodesInChain = playbackEvent.ItemKind == BaseItemKind.Episode ? 1 : 0;
        LastTickUtc = now;
        LastPositionTicks = playbackEvent.PositionTicks;
        LastUserActionUtc = null;
        LastPausedAtUtc = playbackEvent.IsPaused ? now : null;
        LastStoppedAtUtc = null;
        PendingPromptUntilUtc = null;
        SuppressNextPauseEventUntilUtc = null;
        LastActionAtUtc = null;
        IsPaused = playbackEvent.IsPaused;
    }

    private void ResetCounters(DateTimeOffset now)
    {
        StartedAtUtc = now;
        ContinuousElapsed = TimeSpan.Zero;
        EpisodesInChain = ItemKind == BaseItemKind.Episode ? 1 : 0;
        PendingPromptUntilUtc = null;
        LastActionAtUtc = null;
    }

    private void AddElapsedUntil(DateTimeOffset now)
    {
        if (IsPaused || LastTickUtc is null || now <= LastTickUtc.Value)
        {
            return;
        }

        ContinuousElapsed += now - LastTickUtc.Value;
    }

    private void UpdateIdentity(PlaybackEvent playbackEvent)
    {
        UserId = playbackEvent.UserId;
        DeviceId = playbackEvent.DeviceId;
        NowPlayingItemId = playbackEvent.ItemId;
        ItemKind = playbackEvent.ItemKind;
        SeriesId = playbackEvent.SeriesId;
    }
}
