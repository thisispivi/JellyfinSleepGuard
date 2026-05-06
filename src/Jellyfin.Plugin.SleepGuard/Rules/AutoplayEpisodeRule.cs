using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Rules;

/// <summary>
/// Trigger rule that fires when the number of consecutive episodes in the current autoplay chain
/// reaches or exceeds <see cref="PluginConfiguration.MaxAutoplayEpisodes"/>.
/// </summary>
public sealed class AutoplayEpisodeRule : ITriggerRule
{
    public string Name => nameof(AutoplayEpisodeRule);

    public SleepRuleResult Evaluate(PlaybackTracker tracker, PluginConfiguration configuration, DateTimeOffset nowUtc)
    {
        if (configuration.MaxAutoplayEpisodes <= 0)
        {
            return SleepRuleResult.None(Name);
        }

        return tracker.EpisodesInChain >= configuration.MaxAutoplayEpisodes
            ? SleepRuleResult.Fired(Name)
            : SleepRuleResult.None(Name);
    }
}
