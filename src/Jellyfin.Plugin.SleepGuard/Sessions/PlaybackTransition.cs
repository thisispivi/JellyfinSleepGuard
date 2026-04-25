namespace Jellyfin.Plugin.SleepGuard.Sessions;

public enum PlaybackTransition
{
    StartNew,
    Tick,
    Seek,
    ManualPause,
    SelfPause,
    ManualResume,
    AutoplayNext,
    ManualSwitch,
    End
}
