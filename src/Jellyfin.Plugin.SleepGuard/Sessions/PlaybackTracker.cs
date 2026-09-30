using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.SleepGuard.Sessions;

/// <summary>
/// Maintains accumulated playback state for a single Jellyfin session.
/// One tracker exists per active session; it is updated on every playback event
/// and read by the rule evaluation pipeline.
/// </summary>
public sealed class PlaybackTracker
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackTracker"/> class from the first event seen for a session.
    /// </summary>
    /// <param name="playbackEvent">First event seen for the session.</param>
    /// <param name="now">Current UTC time.</param>
    public PlaybackTracker(PlaybackEvent playbackEvent, DateTimeOffset now)
    {
        SessionId = playbackEvent.SessionId;
        LastAccessedUtc = now;
        ApplyNewChain(playbackEvent, now);
    }

    /// <summary>Gets the Jellyfin session ID this tracker belongs to.</summary>
    public string SessionId { get; }

    /// <summary>Gets the user that owns the session.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the client device ID, when reported.</summary>
    public string? DeviceId { get; private set; }

    /// <summary>Gets the ID of the item currently playing.</summary>
    public Guid? NowPlayingItemId { get; private set; }

    /// <summary>Gets the kind of the item currently playing.</summary>
    public BaseItemKind ItemKind { get; private set; }

    /// <summary>Gets the series of the current item when it is an episode.</summary>
    public Guid? SeriesId { get; private set; }

    /// <summary>
    /// Gets the total wall-clock time the session has been actively playing since the last counter reset.
    /// Paused periods are excluded.
    /// </summary>
    public TimeSpan ContinuousElapsed { get; private set; }

    /// <summary>Gets the number of same-series episodes played back-to-back, including the current one.</summary>
    public int EpisodesInChain { get; private set; }

    /// <summary>Gets when the last event was applied; the base for accumulating elapsed time.</summary>
    public DateTimeOffset? LastTickUtc { get; private set; }

    /// <summary>Gets the playback position of the last event, used for seek detection.</summary>
    public long? LastPositionTicks { get; private set; }

    /// <summary>Gets when playback was paused, or <c>null</c> while playing.</summary>
    public DateTimeOffset? LastPausedAtUtc { get; private set; }

    /// <summary>Gets when the last item stopped; used to detect autoplay of the next episode.</summary>
    public DateTimeOffset? LastStoppedAtUtc { get; private set; }

    /// <summary>Gets when the prompt grace period ends, or <c>null</c> when no prompt is pending.</summary>
    public DateTimeOffset? PendingPromptUntilUtc { get; private set; }

    /// <summary>Gets the instant until which a pause event is attributed to SleepGuard's own pause command.</summary>
    public DateTimeOffset? SuppressNextPauseEventUntilUtc { get; private set; }

    /// <summary>Gets when SleepGuard last issued its pause/stop action for the current counting period.</summary>
    public DateTimeOffset? LastActionAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the session is currently paused.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last event applied to this tracker.
    /// Used by the eviction policy to remove orphaned trackers after a period of inactivity.
    /// </summary>
    public DateTimeOffset LastAccessedUtc { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a prompt has been sent and the grace timer is running,
    /// or when the final pause/stop action has already been issued.
    /// Rules do not fire again while an action is pending or complete.
    /// </summary>
    public bool HasPendingOrCompletedAction => PendingPromptUntilUtc is not null || LastActionAtUtc is not null;

    /// <summary>Applies a playback-start event.</summary>
    /// <param name="playbackEvent">The incoming event.</param>
    /// <param name="transition">How the classifier interpreted the event.</param>
    /// <param name="now">Current UTC time.</param>
    public void ApplyStart(PlaybackEvent playbackEvent, PlaybackTransition transition, DateTimeOffset now)
    {
        LastAccessedUtc = now;
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
                ApplyNewChain(playbackEvent, now);
                break;
        }
    }

    /// <summary>Applies a playback-progress event, accumulating elapsed time or resetting counters on user interaction.</summary>
    /// <param name="playbackEvent">The incoming event.</param>
    /// <param name="transition">How the classifier interpreted the event.</param>
    /// <param name="now">Current UTC time.</param>
    public void ApplyProgress(PlaybackEvent playbackEvent, PlaybackTransition transition, DateTimeOffset now)
    {
        LastAccessedUtc = now;
        if (transition is PlaybackTransition.Seek or PlaybackTransition.ManualPause or PlaybackTransition.ManualResume or PlaybackTransition.ManualSwitch)
        {
            ResetCounters();
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

    /// <summary>Records that the current item stopped, keeping counters so an autoplay follow-up can continue the chain.</summary>
    /// <param name="now">Current UTC time.</param>
    public void ApplyStopped(DateTimeOffset now)
    {
        LastAccessedUtc = now;
        AddElapsedUntil(now);
        LastStoppedAtUtc = now;
        LastTickUtc = now;
        LastPositionTicks = null;
    }

    /// <summary>Marks the prompt as sent with its grace period running.</summary>
    /// <param name="untilUtc">When the grace period ends.</param>
    public void MarkPromptPending(DateTimeOffset untilUtc)
    {
        PendingPromptUntilUtc = untilUtc;
    }

    /// <summary>Clears the pending prompt so rules can fire again.</summary>
    public void ClearPromptPending()
    {
        PendingPromptUntilUtc = null;
    }

    /// <summary>Records that the final pause/stop action was issued.</summary>
    /// <param name="now">Current UTC time.</param>
    /// <param name="suppressPauseEvent">Whether the echoed pause event should be attributed to SleepGuard rather than the user.</param>
    public void MarkActionIssued(DateTimeOffset now, bool suppressPauseEvent)
    {
        LastActionAtUtc = now;
        PendingPromptUntilUtc = null;
        if (suppressPauseEvent)
        {
            SuppressNextPauseEventUntilUtc = now.Add(SessionConstants.PauseSuppression);
        }
    }

    private void ApplyNewChain(PlaybackEvent playbackEvent, DateTimeOffset now)
    {
        UpdateIdentity(playbackEvent);
        ContinuousElapsed = TimeSpan.Zero;
        EpisodesInChain = playbackEvent.ItemKind == BaseItemKind.Episode ? 1 : 0;
        LastTickUtc = now;
        LastPositionTicks = playbackEvent.PositionTicks;
        LastPausedAtUtc = playbackEvent.IsPaused ? now : null;
        LastStoppedAtUtc = null;
        PendingPromptUntilUtc = null;
        SuppressNextPauseEventUntilUtc = null;
        LastActionAtUtc = null;
        IsPaused = playbackEvent.IsPaused;
    }

    private void ResetCounters()
    {
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
