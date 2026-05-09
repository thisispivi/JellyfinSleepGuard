using Jellyfin.Plugin.SleepGuard.Api;
using Jellyfin.Plugin.SleepGuard.Configuration;

namespace Jellyfin.Plugin.SleepGuard.Tests;

public sealed class OverlayScriptBuilderTests
{
    [Fact]
    public void Build_SerializesAppearanceSettings()
    {
        var config = new PluginConfiguration
        {
            Language = "it",
            PromptHeader = "Rest check",
            PromptMessage = "Still awake?",
            OverlayBackgroundMode = SleepGuardOverlayBackgroundMode.CustomUrl,
            OverlayArtworkPreference = SleepGuardOverlayArtworkPreference.PrimaryImage,
            OverlayBackgroundColor = "#101820",
            OverlayTextColor = "#F8FAFC",
            OverlayPrimaryButtonColor = "#FFAA00",
            OverlayPrimaryButtonTextColor = "#111111",
            OverlaySecondaryButtonColor = "#334155",
            OverlaySecondaryButtonTextColor = "#EEEEEE",
            OverlayBackgroundDimPercent = 73,
            OverlayArtworkBlurPixels = 14,
            OverlayPanelOpacityPercent = 64,
            OverlayCustomBackgroundUrl = "https://example.test/backdrop.jpg"
        };

        var script = OverlayScriptBuilder.Build(config);

        Assert.NotNull(script);
        Assert.Contains("\"language\":\"it\"", script, StringComparison.Ordinal);
        Assert.Contains("\"overlayBackgroundMode\":\"CustomUrl\"", script, StringComparison.Ordinal);
        Assert.Contains("\"overlayArtworkPreference\":\"PrimaryImage\"", script, StringComparison.Ordinal);
        Assert.Contains("\"overlayBackgroundColor\":\"#101820\"", script, StringComparison.Ordinal);
        Assert.Contains("\"overlayTextColor\":\"#F8FAFC\"", script, StringComparison.Ordinal);
        Assert.Contains("\"overlayPrimaryButtonColor\":\"#FFAA00\"", script, StringComparison.Ordinal);
        Assert.Contains("\"overlayPrimaryButtonTextColor\":\"#111111\"", script, StringComparison.Ordinal);
        Assert.Contains("\"overlaySecondaryButtonColor\":\"#334155\"", script, StringComparison.Ordinal);
        Assert.Contains("\"overlaySecondaryButtonTextColor\":\"#EEEEEE\"", script, StringComparison.Ordinal);
        Assert.Contains("\"overlayBackgroundDimPercent\":73", script, StringComparison.Ordinal);
        Assert.Contains("\"overlayArtworkBlurPixels\":14", script, StringComparison.Ordinal);
        Assert.Contains("\"overlayPanelOpacityPercent\":64", script, StringComparison.Ordinal);
        Assert.Contains("\"overlayCustomBackgroundUrl\":\"https://example.test/backdrop.jpg\"", script, StringComparison.Ordinal);
    }
}
