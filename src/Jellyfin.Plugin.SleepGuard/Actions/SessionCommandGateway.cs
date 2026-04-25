using Jellyfin.Plugin.SleepGuard.Configuration;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace Jellyfin.Plugin.SleepGuard.Actions;

public sealed class SessionCommandGateway : ISessionCommandGateway
{
    private readonly ISessionManager _sessionManager;

    public SessionCommandGateway(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public Task SendPromptAsync(string sessionId, string header, string message, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var command = new MessageCommand
        {
            Header = header,
            Text = message,
            TimeoutMs = (long)timeout.TotalMilliseconds
        };

        return _sessionManager.SendMessageCommand(null!, sessionId, command, cancellationToken);
    }

    public Task SendPlaystateAsync(string sessionId, SleepGuardAction action, CancellationToken cancellationToken)
    {
        var command = new PlaystateRequest
        {
            Command = action == SleepGuardAction.Stop ? PlaystateCommand.Stop : PlaystateCommand.Pause
        };

        return _sessionManager.SendPlaystateCommand(null!, sessionId, command, cancellationToken);
    }
}
