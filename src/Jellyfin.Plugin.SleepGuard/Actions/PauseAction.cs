using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Actions;

/// <summary>
/// Sends the pause playstate command to a session.
/// </summary>
public sealed class PauseAction : ISleepAction
{
    private readonly ISessionCommandGateway _gateway;

    /// <summary>
    /// Initializes a new instance of the <see cref="PauseAction"/> class.
    /// </summary>
    /// <param name="gateway">Gateway used to reach the client session.</param>
    public PauseAction(ISessionCommandGateway gateway)
    {
        _gateway = gateway;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(PlaybackTracker tracker, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        return _gateway.SendPlaystateAsync(tracker.SessionId, SleepGuardAction.Pause, cancellationToken);
    }
}
