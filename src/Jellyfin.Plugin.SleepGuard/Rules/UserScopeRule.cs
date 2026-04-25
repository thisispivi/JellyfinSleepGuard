using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Rules;

public sealed class UserScopeRule : ISleepRule
{
    public string Name => nameof(UserScopeRule);

    public SleepRuleResult Evaluate(PlaybackTracker tracker, PluginConfiguration configuration, DateTimeOffset nowUtc)
    {
        var listed = configuration.UserIds.Contains(tracker.UserId);
        var allowed = configuration.UserMode switch
        {
            SleepGuardUserMode.AllUsers => true,
            SleepGuardUserMode.Whitelist => listed,
            SleepGuardUserMode.Blacklist => !listed,
            _ => true
        };

        return allowed ? SleepRuleResult.None(Name) : SleepRuleResult.Blocked(Name);
    }
}
