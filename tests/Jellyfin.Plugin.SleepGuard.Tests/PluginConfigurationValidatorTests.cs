using Jellyfin.Plugin.SleepGuard.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SleepGuard.Tests;

/// <summary>
/// Verifies that <see cref="PluginConfigurationValidator"/> correctly clamps out-of-range
/// values and logs warnings without throwing.
/// </summary>
public sealed class PluginConfigurationValidatorTests
{
    // ---------------------------------------------------------------------------
    // Minimal ILogger<T> stub — captures warning messages for assertion
    // ---------------------------------------------------------------------------

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

    // ---------------------------------------------------------------------------
    // PromptTimeoutSeconds
    // ---------------------------------------------------------------------------

    [Fact]
    public void PromptTimeoutSeconds_ZeroIsClamped_ToOne()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration { PromptTimeoutSeconds = 0 };

        validator.Validate(config);

        Assert.Equal(1, config.PromptTimeoutSeconds);
        Assert.Single(logger.Warnings);
    }

    [Fact]
    public void PromptTimeoutSeconds_ValidValue_NotChanged()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration { PromptTimeoutSeconds = 8 };

        validator.Validate(config);

        Assert.Equal(8, config.PromptTimeoutSeconds);
        Assert.Empty(logger.Warnings);
    }

    // ---------------------------------------------------------------------------
    // PromptGraceSeconds
    // ---------------------------------------------------------------------------

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

    // ---------------------------------------------------------------------------
    // ActionRepeatCount
    // ---------------------------------------------------------------------------

    [Fact]
    public void ActionRepeatCount_Zero_IsClampedToOne()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration { ActionRepeatCount = 0 };

        validator.Validate(config);

        Assert.Equal(1, config.ActionRepeatCount);
        Assert.Single(logger.Warnings);
    }

    [Fact]
    public void ActionRepeatCount_Six_IsClampedToFive()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration { ActionRepeatCount = 6 };

        validator.Validate(config);

        Assert.Equal(5, config.ActionRepeatCount);
        Assert.Single(logger.Warnings);
    }

    // ---------------------------------------------------------------------------
    // OverlayBackgroundOpacity
    // ---------------------------------------------------------------------------

    [Fact]
    public void OverlayBackgroundOpacity_Negative_IsClampedToZero()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration { OverlayBackgroundOpacity = -10 };

        validator.Validate(config);

        Assert.Equal(0, config.OverlayBackgroundOpacity);
        Assert.Single(logger.Warnings);
    }

    [Fact]
    public void OverlayBackgroundOpacity_Over100_IsClampedTo100()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration { OverlayBackgroundOpacity = 101 };

        validator.Validate(config);

        Assert.Equal(100, config.OverlayBackgroundOpacity);
        Assert.Single(logger.Warnings);
    }

    [Fact]
    public void OverlayBackgroundOpacity_ValidBoundaryValues_NotChanged()
    {
        var validator = CreateValidator(out _);
        var configMin = new PluginConfiguration { OverlayBackgroundOpacity = 0 };
        var configMax = new PluginConfiguration { OverlayBackgroundOpacity = 100 };

        validator.Validate(configMin);
        validator.Validate(configMax);

        Assert.Equal(0, configMin.OverlayBackgroundOpacity);
        Assert.Equal(100, configMax.OverlayBackgroundOpacity);
    }

    // ---------------------------------------------------------------------------
    // No warnings on a fully valid default config
    // ---------------------------------------------------------------------------

    [Fact]
    public void DefaultConfiguration_ProducesNoWarnings()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration();  // all defaults

        validator.Validate(config);

        Assert.Empty(logger.Warnings);
    }

    // ---------------------------------------------------------------------------
    // Multiple violations in one call
    // ---------------------------------------------------------------------------

    [Fact]
    public void MultipleViolations_AllClamped_AndAllWarned()
    {
        var validator = CreateValidator(out var logger);
        var config = new PluginConfiguration
        {
            PromptTimeoutSeconds        = 0,   // invalid
            PromptGraceSeconds          = -5,  // invalid
            ActionRepeatCount           = 10,  // invalid
            OverlayBackgroundOpacity    = 200, // invalid
        };

        validator.Validate(config);

        Assert.Equal(1, config.PromptTimeoutSeconds);
        Assert.Equal(0, config.PromptGraceSeconds);
        Assert.Equal(5, config.ActionRepeatCount);
        Assert.Equal(100, config.OverlayBackgroundOpacity);
        Assert.Equal(4, logger.Warnings.Count);
    }
}
