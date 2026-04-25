using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SleepGuard.Configuration;

/// <summary>
/// SleepGuard plugin settings serialized by Jellyfin.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether SleepGuard evaluates playback sessions.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the playstate command sent after a rule fires and the prompt grace period expires.
    /// </summary>
    public SleepGuardAction Action { get; set; } = SleepGuardAction.Pause;

    /// <summary>
    /// Gets or sets the production continuous-playback threshold in minutes. A value of 0 disables this rule.
    /// </summary>
    public int MaxContinuousMinutes { get; set; } = 120;

    /// <summary>
    /// Gets or sets a diagnostics-only continuous-playback threshold in seconds. A value of 0 uses <see cref="MaxContinuousMinutes"/>.
    /// </summary>
    public int MaxContinuousSeconds { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of episodes allowed in a same-series autoplay chain. A value of 0 disables this rule.
    /// </summary>
    public int MaxAutoplayEpisodes { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether all sleep rules are gated by the server-local time window.
    /// </summary>
    public bool OnlyWithinTimeWindow { get; set; }

    /// <summary>
    /// Gets or sets the server-local time window start in HH:mm:ss form.
    /// </summary>
    public string TimeWindowStart { get; set; } = "22:00:00";

    /// <summary>
    /// Gets or sets the server-local time window end in HH:mm:ss form. Values earlier than the start wrap midnight.
    /// </summary>
    public string TimeWindowEnd { get; set; } = "07:00:00";

    /// <summary>
    /// Gets or sets a value indicating whether continuous-time limits apply to movies.
    /// </summary>
    public bool IncludeMovies { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether continuous-time limits apply to audio and music videos.
    /// </summary>
    public bool IncludeMusic { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether continuous-time limits apply to Live TV items.
    /// </summary>
    public bool IncludeLiveTv { get; set; }

    /// <summary>
    /// Gets or sets how <see cref="UserIds"/> is applied.
    /// </summary>
    public SleepGuardUserMode UserMode { get; set; } = SleepGuardUserMode.AllUsers;

    /// <summary>
    /// Gets or sets user IDs for whitelist and blacklist modes.
    /// </summary>
    public Guid[] UserIds { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether SleepGuard sends a best-effort client message before action.
    /// </summary>
    public bool SendPrompt { get; set; } = true;

    /// <summary>
    /// Gets or sets the language used for the configuration page and default prompt text.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Gets or sets the prompt toast header.
    /// </summary>
    public string PromptHeader { get; set; } = "SleepGuard";

    /// <summary>
    /// Gets or sets the prompt toast body.
    /// </summary>
    public string PromptMessage { get; set; } = "Are you still watching?";

    /// <summary>
    /// Gets or sets how long clients should keep the prompt toast visible.
    /// </summary>
    public int PromptTimeoutSeconds { get; set; } = 8;

    /// <summary>
    /// Gets or sets how long SleepGuard waits after the prompt before sending the configured action. A value of 0 acts immediately.
    /// </summary>
    public int PromptGraceSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a diagnostics-only value that logs the final action without sending media-control commands.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets a diagnostics-only number of repeated pause/stop commands to send when testing clients.
    /// </summary>
    public int ActionRepeatCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the delay between repeated diagnostics action attempts.
    /// </summary>
    public int ActionRepeatIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether every playback progress event is logged.
    /// </summary>
    public bool LogProgressEvents { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether every rule evaluation is logged.
    /// </summary>
    public bool LogRuleChecks { get; set; }
}

/// <summary>
/// Final media-control command sent when SleepGuard acts.
/// </summary>
public enum SleepGuardAction
{
    /// <summary>
    /// Pause the active session.
    /// </summary>
    Pause,

    /// <summary>
    /// Stop the active session.
    /// </summary>
    Stop
}

/// <summary>
/// User scoping mode for SleepGuard rule evaluation.
/// </summary>
public enum SleepGuardUserMode
{
    /// <summary>
    /// Evaluate all users.
    /// </summary>
    AllUsers,

    /// <summary>
    /// Evaluate only users in <see cref="PluginConfiguration.UserIds"/>.
    /// </summary>
    Whitelist,

    /// <summary>
    /// Evaluate every user except users in <see cref="PluginConfiguration.UserIds"/>.
    /// </summary>
    Blacklist
}
