using System.Reflection;
using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.SleepGuard.Configuration;

namespace Jellyfin.Plugin.SleepGuard.Api;

/// <summary>
/// Reads the embedded <c>overlay.js</c> template, serializes overlay-relevant
/// configuration as <c>window.__SLEEPGUARD_CONFIG__</c>, and returns the combined script string.
/// </summary>
public static class OverlayScriptBuilder
{
    private const string EmbeddedResourceName = "Jellyfin.Plugin.SleepGuard.Api.overlay.js";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Builds the full overlay script with server configuration prepended.
    /// </summary>
    /// <param name="config">Configuration snapshot to bake into the script.</param>
    /// <returns>The script, or <c>null</c> if the embedded resource is missing (build misconfiguration).</returns>
    public static string? Build(PluginConfiguration config)
    {
        var assembly = typeof(OverlayScriptBuilder).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var template = reader.ReadToEnd();

        var overlayConfig = new
        {
            language = config.Language,
            promptMessage = config.PromptMessage,
            promptHeader = config.PromptHeader,
            overlayBackgroundMode = config.OverlayBackgroundMode.ToString(),
            overlayArtworkPreference = config.OverlayArtworkPreference.ToString(),
            overlayBackgroundColor = config.OverlayBackgroundColor,
            overlayTextColor = config.OverlayTextColor,
            overlayPrimaryButtonColor = config.OverlayPrimaryButtonColor,
            overlayPrimaryButtonTextColor = config.OverlayPrimaryButtonTextColor,
            overlaySecondaryButtonColor = config.OverlaySecondaryButtonColor,
            overlaySecondaryButtonTextColor = config.OverlaySecondaryButtonTextColor,
            overlayBackgroundDimPercent = config.OverlayBackgroundDimPercent,
            overlayArtworkBlurPixels = config.OverlayArtworkBlurPixels,
            overlayPanelOpacityPercent = config.OverlayPanelOpacityPercent,
            overlayCustomBackgroundUrl = config.OverlayCustomBackgroundUrl,
        };

        var json = JsonSerializer.Serialize(overlayConfig, JsonOptions);
        return $"window.__SLEEPGUARD_CONFIG__ = {json};\n{template}";
    }
}
