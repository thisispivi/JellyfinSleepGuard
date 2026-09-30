using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SleepGuard.Configuration;

/// <summary>
/// Validates and sanitises a <see cref="PluginConfiguration"/> at startup.
/// Invalid values are clamped to safe defaults and a warning is logged; the plugin never crashes on bad config.
/// </summary>
public sealed class PluginConfigurationValidator
{
    private static readonly PluginConfiguration Defaults = new();

    private readonly ILogger<PluginConfigurationValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfigurationValidator"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    public PluginConfigurationValidator(ILogger<PluginConfigurationValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates <paramref name="config"/> in-place, clamping any out-of-range values and logging warnings.
    /// </summary>
    /// <param name="config">Configuration to sanitise.</param>
    public void Validate(PluginConfiguration config)
    {
        config.PromptGraceSeconds = ClampMin(config.PromptGraceSeconds, min: 0, nameof(config.PromptGraceSeconds));
        config.MaxContinuousMinutes = ClampMin(config.MaxContinuousMinutes, min: 0, nameof(config.MaxContinuousMinutes));
        config.MaxAutoplayEpisodes = ClampMin(config.MaxAutoplayEpisodes, min: 0, nameof(config.MaxAutoplayEpisodes));
        config.OverlayBackgroundDimPercent = ClampRange(config.OverlayBackgroundDimPercent, min: 0, max: 95, nameof(config.OverlayBackgroundDimPercent));
        config.OverlayArtworkBlurPixels = ClampRange(config.OverlayArtworkBlurPixels, min: 0, max: 40, nameof(config.OverlayArtworkBlurPixels));
        config.OverlayPanelOpacityPercent = ClampRange(config.OverlayPanelOpacityPercent, min: 0, max: 100, nameof(config.OverlayPanelOpacityPercent));

        config.OverlayBackgroundColor = NormalizeHexColor(config.OverlayBackgroundColor, Defaults.OverlayBackgroundColor, nameof(config.OverlayBackgroundColor));
        config.OverlayTextColor = NormalizeHexColor(config.OverlayTextColor, Defaults.OverlayTextColor, nameof(config.OverlayTextColor));
        config.OverlayPrimaryButtonColor = NormalizeHexColor(config.OverlayPrimaryButtonColor, Defaults.OverlayPrimaryButtonColor, nameof(config.OverlayPrimaryButtonColor));
        config.OverlayPrimaryButtonTextColor = NormalizeHexColor(config.OverlayPrimaryButtonTextColor, Defaults.OverlayPrimaryButtonTextColor, nameof(config.OverlayPrimaryButtonTextColor));
        config.OverlaySecondaryButtonColor = NormalizeHexColor(config.OverlaySecondaryButtonColor, Defaults.OverlaySecondaryButtonColor, nameof(config.OverlaySecondaryButtonColor));
        config.OverlaySecondaryButtonTextColor = NormalizeHexColor(config.OverlaySecondaryButtonTextColor, Defaults.OverlaySecondaryButtonTextColor, nameof(config.OverlaySecondaryButtonTextColor));
        config.OverlayCustomBackgroundUrl = NormalizeCustomBackgroundUrl(config.OverlayCustomBackgroundUrl);
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

    private int ClampRange(int value, int min, int max, string name)
    {
        if (value < min)
        {
            _logger.LogWarning(
                "SleepGuard configuration: {Name}={Value} must be >= {Min}; clamped.",
                name, value, min);
            return min;
        }

        if (value > max)
        {
            _logger.LogWarning(
                "SleepGuard configuration: {Name}={Value} must be <= {Max}; clamped.",
                name, value, max);
            return max;
        }

        return value;
    }

    private string NormalizeHexColor(string? value, string defaultValue, string name)
    {
        var color = value?.Trim();
        if (IsHexColor(color))
        {
            return color!.ToUpperInvariant();
        }

        _logger.LogWarning(
            "SleepGuard configuration: {Name}={Value} is not a valid #RRGGBB color; reset to default.",
            name, value);
        return defaultValue;
    }

    private string NormalizeCustomBackgroundUrl(string? value)
    {
        var url = value?.Trim() ?? string.Empty;
        if (url.Length == 0)
        {
            return string.Empty;
        }

        if (url.StartsWith("/", StringComparison.Ordinal) &&
            !url.StartsWith("//", StringComparison.Ordinal) &&
            !url.Contains('\\', StringComparison.Ordinal))
        {
            return url;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri.ToString();
        }

        _logger.LogWarning(
            "SleepGuard configuration: {Name}={Value} is not an allowed http(s) or server-relative URL; cleared.",
            nameof(PluginConfiguration.OverlayCustomBackgroundUrl), value);
        return string.Empty;
    }

    private static bool IsHexColor(string? value)
    {
        if (value is null || value.Length != 7 || value[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            var isHex =
                (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F');
            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }
}
