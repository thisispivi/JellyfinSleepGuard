using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SleepGuard.Configuration;

/// <summary>
/// SleepGuard plugin settings serialized by Jellyfin as XML.
/// Properties are grouped by their <see cref="SettingsGroupAttribute"/> for the settings-page UI.
/// The flat class structure is intentional — nesting into sub-classes would change the XML
/// element hierarchy and break existing saved configurations.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    // =========================================================================
    // Behavior group
    // =========================================================================

    /// <summary>Master on/off switch. When false, no rules are evaluated.</summary>
    [SettingsGroup("Behavior")]
    public bool Enabled { get; set; } = true;

    /// <summary>The playstate command sent after a rule fires and the prompt grace period expires.</summary>
    [SettingsGroup("Behavior")]
    public SleepGuardAction Action { get; set; } = SleepGuardAction.Pause;

    /// <summary>Continuous playing minutes before action. Set to 0 to disable this rule.</summary>
    [SettingsGroup("Behavior")]
    public int MaxContinuousMinutes { get; set; } = 120;

    /// <summary>Maximum consecutive same-series episodes before action. Set to 0 to disable.</summary>
    [SettingsGroup("Behavior")]
    public int MaxAutoplayEpisodes { get; set; } = 3;

    /// <summary>Gate all rules by the server-local time window when true.</summary>
    [SettingsGroup("Behavior")]
    public bool OnlyWithinTimeWindow { get; set; }

    /// <summary>Server-local time window start in <c>HH:mm:ss</c> form.</summary>
    [SettingsGroup("Behavior")]
    public string TimeWindowStart { get; set; } = "22:00:00";

    /// <summary>Server-local time window end in <c>HH:mm:ss</c> form. Values earlier than the start wrap midnight.</summary>
    [SettingsGroup("Behavior")]
    public string TimeWindowEnd { get; set; } = "07:00:00";

    /// <summary>Apply continuous-time limits to movies.</summary>
    [SettingsGroup("Behavior")]
    public bool IncludeMovies { get; set; } = true;

    /// <summary>Apply continuous-time limits to audio and music videos.</summary>
    [SettingsGroup("Behavior")]
    public bool IncludeMusic { get; set; }

    /// <summary>Apply continuous-time limits to Live TV items.</summary>
    [SettingsGroup("Behavior")]
    public bool IncludeLiveTv { get; set; }

    /// <summary>How <see cref="UserIds"/> is applied.</summary>
    [SettingsGroup("Behavior")]
    public SleepGuardUserMode UserMode { get; set; } = SleepGuardUserMode.AllUsers;

    /// <summary>User IDs for whitelist and blacklist modes.</summary>
    [SettingsGroup("Behavior")]
    public Guid[] UserIds { get; set; } = [];

    /// <summary>Send a best-effort client message before the pause/stop action.</summary>
    [SettingsGroup("Behavior")]
    public bool SendPrompt { get; set; } = true;

    /// <summary>
    /// When true, the Developer Tools tab is visible in the settings page and the
    /// overlay keyboard shortcut (<c>Ctrl+Shift+Alt+S</c>) is active in the browser.
    /// Disable on production servers.
    /// </summary>
    [SettingsGroup("Behavior")]
    public bool DeveloperMode { get; set; }

    // =========================================================================
    // Customization group
    // =========================================================================

    /// <summary>Language used for the configuration page and default prompt text.</summary>
    [SettingsGroup("Customization")]
    public string Language { get; set; } = "en";

    /// <summary>Prompt toast header text.</summary>
    [SettingsGroup("Customization")]
    public string PromptHeader { get; set; } = "SleepGuard";

    /// <summary>Prompt toast body text.</summary>
    [SettingsGroup("Customization")]
    public string PromptMessage { get; set; } = "Are you still watching?";

    /// <summary>Suggested client toast display duration in seconds.</summary>
    [SettingsGroup("Customization")]
    public int PromptTimeoutSeconds { get; set; } = 8;

    /// <summary>Delay in seconds after the prompt before pause or stop is sent. 0 acts immediately.</summary>
    [SettingsGroup("Customization")]
    public int PromptGraceSeconds { get; set; } = 30;

    /// <summary>CSS hex accent color for the overlay continue button.</summary>
    [SettingsGroup("Customization")]
    public string OverlayAccentColor { get; set; } = "#00a4dc";

    /// <summary>Overlay background darkness, 0–100.</summary>
    [SettingsGroup("Customization")]
    public int OverlayBackgroundOpacity { get; set; } = 92;

    /// <summary>Use the current Jellyfin backdrop image behind the overlay.</summary>
    [SettingsGroup("Customization")]
    public bool OverlayUseBackdropImage { get; set; }

    /// <summary>Blur the backdrop image (only visible when backdrop is enabled).</summary>
    [SettingsGroup("Customization")]
    public bool OverlayBlurBackdrop { get; set; } = true;

    /// <summary>Show the continue-watching button on the overlay.</summary>
    [SettingsGroup("Customization")]
    public bool OverlayShowContinueButton { get; set; } = true;

    /// <summary>Show the stay-paused (dismiss) button on the overlay.</summary>
    [SettingsGroup("Customization")]
    public bool OverlayShowDismissButton { get; set; } = true;

    /// <summary>English override for the continue button label. Null uses the built-in default.</summary>
    [SettingsGroup("Customization")]
    public string? OverlayContinueButtonTextEn { get; set; }

    /// <summary>Italian override for the continue button label. Null uses the built-in default.</summary>
    [SettingsGroup("Customization")]
    public string? OverlayContinueButtonTextIt { get; set; }

    /// <summary>English override for the dismiss button label. Null uses the built-in default.</summary>
    [SettingsGroup("Customization")]
    public string? OverlayDismissButtonTextEn { get; set; }

    /// <summary>Italian override for the dismiss button label. Null uses the built-in default.</summary>
    [SettingsGroup("Customization")]
    public string? OverlayDismissButtonTextIt { get; set; }

    // =========================================================================
    // Developer group (hidden unless DeveloperMode = true)
    // =========================================================================

    /// <summary>
    /// Testing override: continuous-time threshold in seconds.
    /// When > 0, overrides <see cref="MaxContinuousMinutes"/> so rules fire quickly during development.
    /// Set to 0 to use the production minutes threshold.
    /// </summary>
    [SettingsGroup("Developer")]
    public int MaxContinuousSeconds { get; set; }

    /// <summary>
    /// When true, logs the final pause/stop action without actually sending it.
    /// Safe to enable on a live server for observation.
    /// </summary>
    [SettingsGroup("Developer")]
    public bool DryRun { get; set; }

    /// <summary>
    /// Send the pause/stop command this many times. Useful for clients that
    /// intermittently miss the first command during testing.
    /// </summary>
    [SettingsGroup("Developer")]
    public int ActionRepeatCount { get; set; } = 1;

    /// <summary>Delay in seconds between repeated action attempts.</summary>
    [SettingsGroup("Developer")]
    public int ActionRepeatIntervalSeconds { get; set; } = 2;

    /// <summary>Log every playback progress event at Information level.</summary>
    [SettingsGroup("Developer")]
    public bool LogProgressEvents { get; set; }

    /// <summary>Log every rule evaluation and its outcome at Information level.</summary>
    [SettingsGroup("Developer")]
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
