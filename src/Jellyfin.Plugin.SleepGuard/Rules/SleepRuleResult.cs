namespace Jellyfin.Plugin.SleepGuard.Rules;

/// <summary>Outcome of a single rule evaluation.</summary>
public enum SleepRuleOutcome
{
    /// <summary>The rule has no opinion; evaluation continues with the next rule.</summary>
    None,

    /// <summary>A trigger rule reached its threshold; SleepGuard should act.</summary>
    Fired,

    /// <summary>A gate rule vetoed the evaluation; no trigger rule runs for this event.</summary>
    Blocked
}

/// <summary>Result of evaluating one rule against a session.</summary>
/// <param name="Outcome">What the rule decided.</param>
/// <param name="RuleName">Name of the rule that produced the result, used in logs.</param>
public sealed record SleepRuleResult(SleepRuleOutcome Outcome, string RuleName)
{
    /// <summary>Creates a <see cref="SleepRuleOutcome.None"/> result.</summary>
    /// <param name="ruleName">Name of the rule.</param>
    /// <returns>The result.</returns>
    public static SleepRuleResult None(string ruleName) => new(SleepRuleOutcome.None, ruleName);

    /// <summary>Creates a <see cref="SleepRuleOutcome.Fired"/> result.</summary>
    /// <param name="ruleName">Name of the rule.</param>
    /// <returns>The result.</returns>
    public static SleepRuleResult Fired(string ruleName) => new(SleepRuleOutcome.Fired, ruleName);

    /// <summary>Creates a <see cref="SleepRuleOutcome.Blocked"/> result.</summary>
    /// <param name="ruleName">Name of the rule.</param>
    /// <returns>The result.</returns>
    public static SleepRuleResult Blocked(string ruleName) => new(SleepRuleOutcome.Blocked, ruleName);
}
