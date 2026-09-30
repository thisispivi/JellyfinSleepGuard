using Jellyfin.Plugin.SleepGuard.Configuration;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace Jellyfin.Plugin.SleepGuard.Actions;

/// <summary>
/// <see cref="ISessionCommandGateway"/> implementation backed by Jellyfin's <see cref="ISessionManager"/>.
/// </summary>
public sealed class SessionCommandGateway : ISessionCommandGateway
{
    private readonly ISessionManager _sessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionCommandGateway"/> class.
    /// </summary>
    /// <param name="sessionManager">Jellyfin session manager.</param>
    public SessionCommandGateway(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    /// <inheritdoc />
    public Task SendPromptAsync(string sessionId, string header, string message, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var command = new MessageCommand
        {
            Header = header,
            Text = message,
            TimeoutMs = (long)timeout.TotalMilliseconds
        };

        // No controlling session: the command originates from the server itself.
        return _sessionManager.SendMessageCommand(null!, sessionId, command, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendPlaystateAsync(string sessionId, SleepGuardAction action, CancellationToken cancellationToken)
    {
        var command = new PlaystateRequest
        {
            Command = action == SleepGuardAction.Stop ? PlaystateCommand.Stop : PlaystateCommand.Pause
        };

        return _sessionManager.SendPlaystateCommand(null!, sessionId, command, cancellationToken);
    }
}
