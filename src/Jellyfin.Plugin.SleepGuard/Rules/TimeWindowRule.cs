using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Rules;

public sealed class TimeWindowRule : ISleepRule
{
    public string Name => nameof(TimeWindowRule);

    public SleepRuleResult Evaluate(PlaybackTracker tracker, PluginConfiguration configuration, DateTimeOffset nowUtc)
    {
        if (!configuration.OnlyWithinTimeWindow)
        {
            return SleepRuleResult.None(Name);
        }

        var localTime = nowUtc.ToLocalTime().TimeOfDay;
        var start = configuration.TimeWindowStart;
        var end = configuration.TimeWindowEnd;

        var inside = start <= end
            ? localTime >= start && localTime <= end
            : localTime >= start || localTime <= end;

        return inside ? SleepRuleResult.None(Name) : SleepRuleResult.Blocked(Name);
    }
}
