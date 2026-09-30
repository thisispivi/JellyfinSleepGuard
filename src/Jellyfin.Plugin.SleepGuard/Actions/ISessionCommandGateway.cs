using Jellyfin.Plugin.SleepGuard.Configuration;

namespace Jellyfin.Plugin.SleepGuard.Actions;

/// <summary>
/// Thin abstraction over Jellyfin's session manager for the remote commands SleepGuard sends.
/// Exists so actions can be unit-tested without a running Jellyfin server.
/// </summary>
public interface ISessionCommandGateway
{
    /// <summary>
    /// Sends a message (toast/dialog) to the client that owns <paramref name="sessionId"/>.
    /// </summary>
    /// <param name="sessionId">Target Jellyfin session ID.</param>
    /// <param name="header">Message title.</param>
    /// <param name="message">Message body.</param>
    /// <param name="timeout">How long the client should keep the message visible.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the command has been dispatched.</returns>
    Task SendPromptAsync(string sessionId, string header, string message, TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Sends the pause or stop playstate command to the client that owns <paramref name="sessionId"/>.
    /// </summary>
    /// <param name="sessionId">Target Jellyfin session ID.</param>
    /// <param name="action">The playstate command to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the command has been dispatched.</returns>
    Task SendPlaystateAsync(string sessionId, SleepGuardAction action, CancellationToken cancellationToken);
}
