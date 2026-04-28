using System.Reflection;
using System.Text;
using System.Text.Json;
using Jellyfin.Plugin.SleepGuard.Configuration;

namespace Jellyfin.Plugin.SleepGuard.Api;

/// <summary>
/// Reads the embedded <c>overlay.js</c> template, serializes overlay-relevant
/// configuration as JSON, and returns the combined script string.
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
    /// Returns <c>null</c> if the embedded resource is missing.
    /// </summary>
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
            accentColor = config.OverlayAccentColor,
            backgroundOpacity = config.OverlayBackgroundOpacity,
            useBackdropImage = config.OverlayUseBackdropImage,
            blurBackdrop = config.OverlayBlurBackdrop,
            showContinueButton = config.OverlayShowContinueButton,
            showDismissButton = config.OverlayShowDismissButton,
            continueTextEn = config.OverlayContinueButtonTextEn,
            continueTextIt = config.OverlayContinueButtonTextIt,
            dismissTextEn = config.OverlayDismissButtonTextEn,
            dismissTextIt = config.OverlayDismissButtonTextIt,
            language = config.Language,
            promptMessage = config.PromptMessage,
            promptHeader = config.PromptHeader,
        };

        var json = JsonSerializer.Serialize(overlayConfig, JsonOptions);
        return $"window.__SLEEPGUARD_CONFIG__ = {json};\n{template}";
    }
}
