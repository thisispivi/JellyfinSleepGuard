using Jellyfin.Plugin.SleepGuard.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SleepGuard.Tests;

public sealed class PluginConfigurationValidatorTests
{
    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }

    private static PluginConfigurationValidator CreateValidator(out CapturingLogger<PluginConfigurationValidator> logger)
    {
        logger = new CapturingLogger<PluginConfigurationValidator>();
        return new PluginConfigurationValidator(logger);
    }

    [Fact]
    public void PromptGraceSeconds_Negative_IsClampedToZero()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration { PromptGraceSeconds = -1 };

        validator.Validate(config);

        Assert.Equal(0, config.PromptGraceSeconds);
        Assert.Single(logger.Warnings);
    }

    [Fact]
    public void PromptGraceSeconds_Zero_IsValid()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration { PromptGraceSeconds = 0 };

        validator.Validate(config);

        Assert.Equal(0, config.PromptGraceSeconds);
        Assert.Empty(logger.Warnings);
    }

    [Fact]
    public void DefaultConfiguration_ProducesNoWarnings()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration();

        validator.Validate(config);

        Assert.Empty(logger.Warnings);
    }

    [Fact]
    public void MultipleViolations_AllClamped_AndAllWarned()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration
        {
            PromptGraceSeconds = -5,
            MaxContinuousMinutes = -1,
            MaxAutoplayEpisodes = -2,
        };

        validator.Validate(config);

        Assert.Equal(0, config.PromptGraceSeconds);
        Assert.Equal(0, config.MaxContinuousMinutes);
        Assert.Equal(0, config.MaxAutoplayEpisodes);
        Assert.Equal(3, logger.Warnings.Count);
    }

    [Fact]
    public void AppearanceNumbers_OutOfRange_AreClamped()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration
        {
            OverlayBackgroundDimPercent = 120,
            OverlayArtworkBlurPixels = -3,
            OverlayPanelOpacityPercent = 125
        };

        validator.Validate(config);

        Assert.Equal(95, config.OverlayBackgroundDimPercent);
        Assert.Equal(0, config.OverlayArtworkBlurPixels);
        Assert.Equal(100, config.OverlayPanelOpacityPercent);
        Assert.Equal(3, logger.Warnings.Count);
    }

    [Fact]
    public void InvalidAppearanceColors_ResetToDefaults()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration
        {
            OverlayBackgroundColor = "red",
            OverlayTextColor = "#12",
            OverlayPrimaryButtonColor = "#GGGGGG",
            OverlayPrimaryButtonTextColor = "javascript:alert(1)",
            OverlaySecondaryButtonColor = "",
            OverlaySecondaryButtonTextColor = "#FFFFFX"
        };

        validator.Validate(config);

        Assert.Equal("#05080D", config.OverlayBackgroundColor);
        Assert.Equal("#FFFFFF", config.OverlayTextColor);
        Assert.Equal("#00A4DC", config.OverlayPrimaryButtonColor);
        Assert.Equal("#FFFFFF", config.OverlayPrimaryButtonTextColor);
        Assert.Equal("#2B3038", config.OverlaySecondaryButtonColor);
        Assert.Equal("#FFFFFF", config.OverlaySecondaryButtonTextColor);
        Assert.Equal(6, logger.Warnings.Count);
    }

    [Theory]
    [InlineData("https://example.test/backdrop.jpg", "https://example.test/backdrop.jpg")]
    [InlineData("http://example.test/backdrop.jpg", "http://example.test/backdrop.jpg")]
    [InlineData("/Items/abc/Images/Backdrop/0", "/Items/abc/Images/Backdrop/0")]
    public void CustomBackgroundUrl_AllowsHttpHttpsAndServerRelativeValues(string input, string expected)
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration { OverlayCustomBackgroundUrl = input };

        validator.Validate(config);

        Assert.Equal(expected, config.OverlayCustomBackgroundUrl);
        Assert.Empty(logger.Warnings);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,abc")]
    [InlineData("//example.test/backdrop.jpg")]
    [InlineData("/bad\\path.jpg")]
    public void CustomBackgroundUrl_RejectsUnsafeValues(string input)
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration { OverlayCustomBackgroundUrl = input };

        validator.Validate(config);

        Assert.Empty(config.OverlayCustomBackgroundUrl);
        Assert.Single(logger.Warnings);
    }
}
