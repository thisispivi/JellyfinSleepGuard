using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Actions;

public sealed class PromptAction : ISleepAction
{
    private readonly ISessionCommandGateway _gateway;

    public PromptAction(ISessionCommandGateway gateway)
    {
        _gateway = gateway;
    }

    public Task ExecuteAsync(PlaybackTracker tracker, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        return _gateway.SendPromptAsync(
            tracker.SessionId,
            string.IsNullOrWhiteSpace(configuration.PromptMessage) ? "Are you still watching?" : configuration.PromptMessage,
            TimeSpan.FromSeconds(Math.Max(1, configuration.PromptGraceSeconds)),
            cancellationToken);
    }
}
