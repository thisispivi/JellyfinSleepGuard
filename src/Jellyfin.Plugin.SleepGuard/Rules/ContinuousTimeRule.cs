using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Rules;

/// <summary>
/// Trigger rule that fires when a session's accumulated uninterrupted playing time reaches the configured threshold.
/// <para>
/// The threshold is controlled by <see cref="PluginConfiguration.MaxContinuousMinutes"/> in production.
/// <see cref="PluginConfiguration.MaxContinuousSeconds"/> provides a seconds-precision override for
/// testing without having to wait for full minutes.
/// </para>
/// </summary>
public sealed class ContinuousTimeRule : ITriggerRule
{
    /// <inheritdoc />
    public string Name => nameof(ContinuousTimeRule);

    /// <inheritdoc />
    public SleepRuleResult Evaluate(PlaybackTracker tracker, PluginConfiguration configuration, DateTimeOffset nowUtc)
    {
        var threshold = GetThreshold(configuration);
        if (threshold <= TimeSpan.Zero || !IsIncluded(tracker.ItemKind, configuration))
        {
            return SleepRuleResult.None(Name);
        }

        return tracker.ContinuousElapsed >= threshold
            ? SleepRuleResult.Fired(Name)
            : SleepRuleResult.None(Name);
    }

    private static TimeSpan GetThreshold(PluginConfiguration configuration)
    {
        if (configuration.MaxContinuousSeconds > 0)
        {
            return TimeSpan.FromSeconds(configuration.MaxContinuousSeconds);
        }

        return configuration.MaxContinuousMinutes > 0
            ? TimeSpan.FromMinutes(configuration.MaxContinuousMinutes)
            : TimeSpan.Zero;
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
