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
        config.PromptTimeoutSeconds = Clamp(config.PromptTimeoutSeconds, min: 1, max: int.MaxValue, nameof(config.PromptTimeoutSeconds));
        config.PromptGraceSeconds = ClampMin(config.PromptGraceSeconds, min: 0, nameof(config.PromptGraceSeconds));
        config.ActionRepeatCount = Clamp(config.ActionRepeatCount, min: 1, max: 5, nameof(config.ActionRepeatCount));
        config.ActionRepeatIntervalSeconds = Clamp(config.ActionRepeatIntervalSeconds, min: 0, max: 30, nameof(config.ActionRepeatIntervalSeconds));
        config.MaxContinuousMinutes = ClampMin(config.MaxContinuousMinutes, min: 0, nameof(config.MaxContinuousMinutes));
        config.MaxAutoplayEpisodes = ClampMin(config.MaxAutoplayEpisodes, min: 0, nameof(config.MaxAutoplayEpisodes));
        config.OverlayBackgroundOpacity = Clamp(config.OverlayBackgroundOpacity, min: 0, max: 100, nameof(config.OverlayBackgroundOpacity));
    }

    private int Clamp(int value, int min, int max, string name)
    {
        if (value < min || value > max)
        {
            var clamped = Math.Clamp(value, min, max);
            _logger.LogWarning(
                "SleepGuard configuration: {Name}={Value} is outside the valid range [{Min}, {Max}]; clamped to {Clamped}.",
                name, value, min, max, clamped);
            return clamped;
        }

        return value;
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
