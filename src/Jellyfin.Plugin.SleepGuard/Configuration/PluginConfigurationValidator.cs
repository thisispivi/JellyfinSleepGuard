using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SleepGuard.Configuration;

/// <summary>
/// Validates and sanitises a <see cref="PluginConfiguration"/> at startup.
/// Invalid values are clamped to safe defaults and a warning is logged; the plugin never crashes on bad config.
/// </summary>
public sealed class PluginConfigurationValidator
{
    private readonly ILogger<PluginConfigurationValidator> _logger;

    public PluginConfigurationValidator(ILogger<PluginConfigurationValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates <paramref name="config"/> in-place, clamping any out-of-range values and logging warnings.
    /// </summary>
    public void Validate(PluginConfiguration config)
    {
        config.PromptGraceSeconds = ClampMin(config.PromptGraceSeconds, min: 0, nameof(config.PromptGraceSeconds));
        config.MaxContinuousMinutes = ClampMin(config.MaxContinuousMinutes, min: 0, nameof(config.MaxContinuousMinutes));
        config.MaxAutoplayEpisodes = ClampMin(config.MaxAutoplayEpisodes, min: 0, nameof(config.MaxAutoplayEpisodes));
    }

    private int ClampMin(int value, int min, string name)
    {
        if (value < min)
        {
            _logger.LogWarning(
                "SleepGuard configuration: {Name}={Value} must be >= {Min}; clamped.",
                name, value, min);
            return min;
        }

        return value;
    }
}
