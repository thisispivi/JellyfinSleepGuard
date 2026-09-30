using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Rules;

/// <summary>
/// A gate rule runs before all trigger rules and can prevent the entire evaluation pipeline
/// from proceeding. A <see cref="SleepRuleOutcome.Blocked"/> outcome short-circuits all
/// subsequent gate rules and all trigger rules for the current event.
/// </summary>
/// <remarks>
/// Implement this interface for rules that restrict <em>when</em> SleepGuard is active,
/// such as user-scope filtering or time-window restrictions.
/// </remarks>
public interface IGateRule
{
    /// <summary>Gets the rule name used in logs.</summary>
    string Name { get; }

    /// <summary>Evaluates the rule against a session.</summary>
    /// <param name="tracker">Accumulated playback state of the session.</param>
    /// <param name="configuration">Configuration snapshot for this evaluation.</param>
    /// <param name="nowUtc">Current UTC time.</param>
    /// <returns><see cref="SleepRuleOutcome.Blocked"/> to stop the evaluation, otherwise <see cref="SleepRuleOutcome.None"/>.</returns>
    SleepRuleResult Evaluate(PlaybackTracker tracker, PluginConfiguration configuration, DateTimeOffset nowUtc);
}
