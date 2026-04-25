using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SleepGuard.Actions;
using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Tests;

public sealed class PromptActionTests
{
    [Fact]
    public async Task UsesItalianPromptFallbackWhenLanguageIsItalian()
    {
        var gateway = new CapturingGateway();
        var action = new PromptAction(gateway);
        var configuration = new PluginConfiguration
        {
            Language = "it",
            PromptMessage = ""
        };

        await action.ExecuteAsync(CreateTracker(), configuration, CancellationToken.None);

        Assert.Equal("Stai ancora guardando?", gateway.Message);
    }

    [Fact]
    public async Task UsesEnglishPromptFallbackForUnknownLanguage()
    {
        var gateway = new CapturingGateway();
        var action = new PromptAction(gateway);
        var configuration = new PluginConfiguration
        {
            Language = "de",
            PromptMessage = ""
        };

        await action.ExecuteAsync(CreateTracker(), configuration, CancellationToken.None);

        Assert.Equal("Are you still watching?", gateway.Message);
    }

    private static PlaybackTracker CreateTracker()
    {
        return new PlaybackTracker(
            new PlaybackEvent(
                "session-1",
                Guid.NewGuid(),
                "device-1",
                Guid.NewGuid(),
                BaseItemKind.Episode,
                Guid.NewGuid(),
                0,
                false),
            DateTimeOffset.UtcNow);
    }

    private sealed class CapturingGateway : ISessionCommandGateway
    {
        public string? Message { get; private set; }

        public Task SendPromptAsync(string sessionId, string header, string message, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Message = message;
            return Task.CompletedTask;
        }

        public Task SendPlaystateAsync(string sessionId, SleepGuardAction action, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
