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
        var start = ParseTime(configuration.TimeWindowStart, new TimeSpan(22, 0, 0));
        var end = ParseTime(configuration.TimeWindowEnd, new TimeSpan(7, 0, 0));

        var inside = start <= end
            ? localTime >= start && localTime <= end
            : localTime >= start || localTime <= end;

        return inside ? SleepRuleResult.None(Name) : SleepRuleResult.Blocked(Name);
    }

    private static TimeSpan ParseTime(string? value, TimeSpan fallback)
    {
        if (TimeSpan.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
