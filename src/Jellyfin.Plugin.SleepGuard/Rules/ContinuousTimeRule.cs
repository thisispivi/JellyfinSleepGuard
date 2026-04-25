using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Rules;

public sealed class ContinuousTimeRule : ISleepRule
{
    public string Name => nameof(ContinuousTimeRule);

    public SleepRuleResult Evaluate(PlaybackTracker tracker, PluginConfiguration configuration, DateTimeOffset nowUtc)
    {
        if (configuration.MaxContinuousMinutes <= 0 || !IsIncluded(tracker.ItemKind, configuration))
        {
            return SleepRuleResult.None(Name);
        }

        return tracker.ContinuousElapsed >= TimeSpan.FromMinutes(configuration.MaxContinuousMinutes)
            ? SleepRuleResult.Fired(Name)
            : SleepRuleResult.None(Name);
    }

    private static bool IsIncluded(BaseItemKind kind, PluginConfiguration configuration)
    {
        return kind switch
        {
            BaseItemKind.Movie => configuration.IncludeMovies,
            BaseItemKind.Audio or BaseItemKind.MusicAlbum or BaseItemKind.MusicArtist or BaseItemKind.MusicVideo => configuration.IncludeMusic,
            BaseItemKind.LiveTvChannel or BaseItemKind.LiveTvProgram or BaseItemKind.TvChannel or BaseItemKind.TvProgram => configuration.IncludeLiveTv,
            _ => true
        };
    }
}
