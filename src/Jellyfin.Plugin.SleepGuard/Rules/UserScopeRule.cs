using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Rules;

/// <summary>
/// Gate rule that blocks evaluation when the session's user is not in scope.
/// Supports all-users, whitelist, and blacklist modes.
/// </summary>
public sealed class UserScopeRule : IGateRule
{
    /// <inheritdoc />
    public string Name => nameof(UserScopeRule);

    /// <inheritdoc />
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
