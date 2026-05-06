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
}
