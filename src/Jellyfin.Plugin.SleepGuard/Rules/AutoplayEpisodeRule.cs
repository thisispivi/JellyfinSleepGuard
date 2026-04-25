using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Rules;

public sealed class AutoplayEpisodeRule : ISleepRule
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
