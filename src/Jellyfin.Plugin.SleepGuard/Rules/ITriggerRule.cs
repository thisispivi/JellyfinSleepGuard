using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Rules;

/// <summary>
/// A trigger rule is evaluated only after all <see cref="IGateRule"/> instances have returned
/// <see cref="SleepRuleOutcome.None"/>. A <see cref="SleepRuleOutcome.Fired"/> outcome causes
/// SleepGuard to execute the configured prompt and pause/stop action.
/// </summary>
/// <remarks>
/// Implement this interface for rules that detect <em>why</em> SleepGuard should act,
/// such as continuous-time thresholds or autoplay episode limits.
/// </remarks>
public interface ITriggerRule
{
    string Name { get; }

    SleepRuleResult Evaluate(PlaybackTracker tracker, PluginConfiguration configuration, DateTimeOffset nowUtc);
}
