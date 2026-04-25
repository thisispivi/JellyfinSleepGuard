using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Rules;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Tests;

public sealed class RulesTests
{
    [Fact]
    public void ContinuousTimeRuleFiresAtConfiguredThreshold()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = Tracker(BaseItemKind.Movie, now);
        tracker.ApplyProgress(Event(BaseItemKind.Movie, positionTicks: TimeSpan.FromMinutes(120).Ticks), PlaybackTransition.Tick, now.AddMinutes(120));
        var rule = new ContinuousTimeRule();

        var result = rule.Evaluate(tracker, new PluginConfiguration { MaxContinuousMinutes = 120 }, now.AddMinutes(120));

        Assert.Equal(SleepRuleOutcome.Fired, result.Outcome);
    }

    [Fact]
    public void ContinuousTimeRuleUsesSecondsOverrideForTesting()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = Tracker(BaseItemKind.Episode, now);
        tracker.ApplyProgress(Event(BaseItemKind.Episode, positionTicks: TimeSpan.FromSeconds(15).Ticks), PlaybackTransition.Tick, now.AddSeconds(15));
        var rule = new ContinuousTimeRule();

        var result = rule.Evaluate(tracker, new PluginConfiguration { MaxContinuousMinutes = 120, MaxContinuousSeconds = 10 }, now.AddSeconds(15));

        Assert.Equal(SleepRuleOutcome.Fired, result.Outcome);
    }

    [Fact]
    public void ContinuousTimeRuleHonorsMovieOptOut()
    {
        var now = DateTimeOffset.UtcNow;
        var tracker = Tracker(BaseItemKind.Movie, now);
        tracker.ApplyProgress(Event(BaseItemKind.Movie, positionTicks: TimeSpan.FromMinutes(120).Ticks), PlaybackTransition.Tick, now.AddMinutes(120));
        var rule = new ContinuousTimeRule();

        var result = rule.Evaluate(tracker, new PluginConfiguration { MaxContinuousMinutes = 1, IncludeMovies = false }, now.AddMinutes(120));

        Assert.Equal(SleepRuleOutcome.None, result.Outcome);
    }

    [Fact]
    public void AutoplayEpisodeRuleFiresWhenEpisodeLimitReached()
    {
        var tracker = Tracker(BaseItemKind.Episode, DateTimeOffset.UtcNow);
        var rule = new AutoplayEpisodeRule();

        var result = rule.Evaluate(tracker, new PluginConfiguration { MaxAutoplayEpisodes = 1 }, DateTimeOffset.UtcNow);

        Assert.Equal(SleepRuleOutcome.Fired, result.Outcome);
    }

    [Fact]
    public void UserScopeWhitelistBlocksUnlistedUser()
    {
        var tracker = Tracker(BaseItemKind.Episode, DateTimeOffset.UtcNow);
        var rule = new UserScopeRule();

        var result = rule.Evaluate(tracker, new PluginConfiguration { UserMode = SleepGuardUserMode.Whitelist, UserIds = [Guid.NewGuid()] }, DateTimeOffset.UtcNow);

        Assert.Equal(SleepRuleOutcome.Blocked, result.Outcome);
    }

    [Fact]
    public void TimeWindowHandlesMidnightWrap()
    {
        var tracker = Tracker(BaseItemKind.Episode, DateTimeOffset.UtcNow);
        var rule = new TimeWindowRule();
        var localToday = DateTime.Today.AddHours(23);

        var result = rule.Evaluate(
            tracker,
            new PluginConfiguration
            {
                OnlyWithinTimeWindow = true,
                TimeWindowStart = "22:00:00",
                TimeWindowEnd = "07:00:00"
            },
            new DateTimeOffset(localToday).ToUniversalTime());

        Assert.Equal(SleepRuleOutcome.None, result.Outcome);
    }

    private static PlaybackTracker Tracker(BaseItemKind kind, DateTimeOffset now)
    {
        return new PlaybackTracker(Event(kind), now);
    }

    private static PlaybackEvent Event(BaseItemKind kind, long? positionTicks = null)
    {
        return new PlaybackEvent(
            "session-1",
            Guid.NewGuid(),
            "device-1",
            Guid.NewGuid(),
            kind,
            kind == BaseItemKind.Episode ? Guid.NewGuid() : null,
            positionTicks,
            false);
    }
}
