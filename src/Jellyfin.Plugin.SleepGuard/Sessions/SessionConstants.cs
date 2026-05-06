namespace Jellyfin.Plugin.SleepGuard.Sessions;

/// <summary>
/// Central repository for session-timing constants used across the playback tracking pipeline.
/// Collecting them here makes the values visible and easy to adjust during testing.
/// </summary>
internal static class SessionConstants
{
    /// <summary>Maximum time between a Stop event and the next Start event to be classified as autoplay.</summary>
    public static readonly TimeSpan AutoplayWindow = TimeSpan.FromSeconds(10);

    /// <summary>Minimum pause duration before a resume is treated as a deliberate user action.</summary>
    public static readonly TimeSpan LongPauseWindow = TimeSpan.FromSeconds(30);

    /// <summary>Tolerance used when distinguishing normal playback progress from a seek.</summary>
    public static readonly TimeSpan SeekTolerance = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long after SleepGuard issues its own Pause command to suppress the echoed pause event.
    /// This prevents the incoming pause confirmation from being misclassified as a manual user pause
    /// that would reset the inactivity counters.
    /// </summary>
    public static readonly TimeSpan PauseSuppression = TimeSpan.FromSeconds(15);

    /// <summary>Age at which a tracker with no recent activity is eligible for eviction.</summary>
    public static readonly TimeSpan TrackerEvictionAge = TimeSpan.FromHours(4);

    /// <summary>How often the background eviction pass runs.</summary>
    public static readonly TimeSpan EvictionInterval = TimeSpan.FromMinutes(30);
}
