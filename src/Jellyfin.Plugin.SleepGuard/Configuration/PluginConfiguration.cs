using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SleepGuard.Configuration;

/// <summary>
/// SleepGuard plugin settings serialized by Jellyfin.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = true;

    public SleepGuardAction Action { get; set; } = SleepGuardAction.Pause;

    public int MaxContinuousMinutes { get; set; } = 120;

    public int MaxAutoplayEpisodes { get; set; } = 3;

    public bool OnlyWithinTimeWindow { get; set; }

    public string TimeWindowStart { get; set; } = "22:00:00";

    public string TimeWindowEnd { get; set; } = "07:00:00";

    public bool IncludeMovies { get; set; } = true;

    public bool IncludeMusic { get; set; }

    public bool IncludeLiveTv { get; set; }

    public SleepGuardUserMode UserMode { get; set; } = SleepGuardUserMode.AllUsers;

    public Guid[] UserIds { get; set; } = [];

    public bool SendPrompt { get; set; } = true;

    public string PromptMessage { get; set; } = "Are you still watching?";

    public int PromptGraceSeconds { get; set; } = 30;
}

public enum SleepGuardAction
{
    Pause,
    Stop
}

public enum SleepGuardUserMode
{
    AllUsers,
    Whitelist,
    Blacklist
}
