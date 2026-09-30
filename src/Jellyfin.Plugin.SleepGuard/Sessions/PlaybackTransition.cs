namespace Jellyfin.Plugin.SleepGuard.Sessions;

/// <summary>
/// What a playback event means relative to the session's previous state.
/// Produced by <see cref="PlaybackEventClassifier"/>.
/// </summary>
public enum PlaybackTransition
{
    /// <summary>Playback started with no previous item; a new chain begins.</summary>
    StartNew,

    /// <summary>Ordinary progress; playing time keeps accumulating.</summary>
    Tick,

    /// <summary>The user jumped to another position; counters reset.</summary>
    Seek,

    /// <summary>The user paused playback; counters reset.</summary>
    ManualPause,

    /// <summary>The pause echoed back from SleepGuard's own pause command; counters are kept.</summary>
    SelfPause,

    /// <summary>The user resumed after a SleepGuard action or a long pause; counters reset.</summary>
    ManualResume,

    /// <summary>The next episode of the same series started right after the previous one stopped.</summary>
    AutoplayNext,

    /// <summary>The user started a different item; a new chain begins.</summary>
    ManualSwitch,

    /// <summary>Playback of the current item stopped.</summary>
    End
}
