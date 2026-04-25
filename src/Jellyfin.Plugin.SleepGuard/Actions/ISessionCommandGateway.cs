using Jellyfin.Plugin.SleepGuard.Configuration;

namespace Jellyfin.Plugin.SleepGuard.Actions;

public interface ISessionCommandGateway
{
    Task SendPromptAsync(string sessionId, string message, TimeSpan timeout, CancellationToken cancellationToken);

    Task SendPlaystateAsync(string sessionId, SleepGuardAction action, CancellationToken cancellationToken);
}
