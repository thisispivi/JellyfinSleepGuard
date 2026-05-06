using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SleepGuard.Configuration;

/// <summary>
/// SleepGuard plugin settings serialized by Jellyfin as XML.
/// The flat class structure is intentional; nesting into sub-classes would change
/// the XML element hierarchy and break existing saved configurations.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Master on/off switch. When false, no rules are evaluated.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The playstate command sent after a rule fires and the prompt grace period expires.</summary>
    public SleepGuardAction Action { get; set; } = SleepGuardAction.Pause;

    /// <summary>Continuous playing minutes before action. Set to 0 to disable this rule.</summary>
    public int MaxContinuousMinutes { get; set; } = 120;

    /// <summary>Maximum consecutive same-series episodes before action. Set to 0 to disable.</summary>
    public int MaxAutoplayEpisodes { get; set; } = 3;

    /// <summary>Gate all rules by the server-local time window when true.</summary>
    public bool OnlyWithinTimeWindow { get; set; }

    /// <summary>Server-local time window start in <c>HH:mm:ss</c> form.</summary>
    public string TimeWindowStart { get; set; } = "22:00:00";

    /// <summary>Server-local time window end in <c>HH:mm:ss</c> form. Values earlier than the start wrap midnight.</summary>
    public string TimeWindowEnd { get; set; } = "07:00:00";

    /// <summary>Apply continuous-time limits to movies.</summary>
    public bool IncludeMovies { get; set; } = true;

    /// <summary>Apply continuous-time limits to audio and music videos.</summary>
    public bool IncludeMusic { get; set; }

    /// <summary>Apply continuous-time limits to Live TV items.</summary>
    public bool IncludeLiveTv { get; set; }

    /// <summary>How <see cref="UserIds"/> is applied.</summary>
    public SleepGuardUserMode UserMode { get; set; } = SleepGuardUserMode.AllUsers;

    /// <summary>User IDs for whitelist and blacklist modes.</summary>
    public Guid[] UserIds { get; set; } = [];

    /// <summary>Send a best-effort client message before the pause/stop action.</summary>
    public bool SendPrompt { get; set; } = true;

    /// <summary>Language used for the configuration page and default prompt text.</summary>
    public string Language { get; set; } = "en";

    /// <summary>Prompt toast and overlay header text.</summary>
    public string PromptHeader { get; set; } = "SleepGuard";

    /// <summary>Prompt toast and overlay body text.</summary>
    public string PromptMessage { get; set; } = "Are you still watching?";

    /// <summary>Delay in seconds after the prompt before pause or stop is sent. 0 acts immediately.</summary>
    public int PromptGraceSeconds { get; set; } = 30;

    /// <summary>
    /// Testing override: continuous-time threshold in seconds.
    /// When greater than 0, overrides <see cref="MaxContinuousMinutes"/>.
    /// </summary>
    public int MaxContinuousSeconds { get; set; }

    /// <summary>When true, logs the final pause/stop action without actually sending it.</summary>
    public bool DryRun { get; set; }

    /// <summary>Log every rule evaluation and its outcome at Information level.</summary>
    public bool LogRuleChecks { get; set; }
}

/// <summary>Final media-control command sent when SleepGuard acts.</summary>
public enum SleepGuardAction
{
    /// <summary>Pause the active session.</summary>
    Pause,

    /// <summary>Stop the active session.</summary>
    Stop
}

/// <summary>User scoping mode for SleepGuard rule evaluation.</summary>
public enum SleepGuardUserMode
{
    /// <summary>Evaluate all users.</summary>
    AllUsers,

    /// <summary>Evaluate only users in <see cref="PluginConfiguration.UserIds"/>.</summary>
    Whitelist,

    /// <summary>Evaluate every user except those in <see cref="PluginConfiguration.UserIds"/>.</summary>
    Blacklist
}
