using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Actions;

public sealed class PauseAction : ISleepAction
{
    private readonly ISessionCommandGateway _gateway;

    public PauseAction(ISessionCommandGateway gateway)
    {
        _gateway = gateway;
    }

    public Task ExecuteAsync(PlaybackTracker tracker, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        return _gateway.SendPlaystateAsync(tracker.SessionId, SleepGuardAction.Pause, cancellationToken);
    }
}
