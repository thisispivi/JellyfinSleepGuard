namespace Jellyfin.Plugin.SleepGuard.Rules;

public enum SleepRuleOutcome
{
    None,
    Fired,
    Blocked
}

public sealed record SleepRuleResult(SleepRuleOutcome Outcome, string RuleName)
{
    public static SleepRuleResult None(string ruleName) => new(SleepRuleOutcome.None, ruleName);

    public static SleepRuleResult Fired(string ruleName) => new(SleepRuleOutcome.Fired, ruleName);

    public static SleepRuleResult Blocked(string ruleName) => new(SleepRuleOutcome.Blocked, ruleName);
}
