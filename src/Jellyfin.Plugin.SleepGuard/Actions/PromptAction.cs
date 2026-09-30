using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Actions;

/// <summary>
/// Sends the "are you still watching?" message to a session before the final pause/stop action.
/// </summary>
public sealed class PromptAction : ISleepAction
{
    private static readonly TimeSpan PromptTimeout = TimeSpan.FromSeconds(8);

    private readonly ISessionCommandGateway _gateway;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptAction"/> class.
    /// </summary>
    /// <param name="gateway">Gateway used to reach the client session.</param>
    public PromptAction(ISessionCommandGateway gateway)
    {
        _gateway = gateway;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(PlaybackTracker tracker, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        var language = NormalizeLanguage(configuration.Language);

        return _gateway.SendPromptAsync(
            tracker.SessionId,
            string.IsNullOrWhiteSpace(configuration.PromptHeader) ? "SleepGuard" : configuration.PromptHeader,
            string.IsNullOrWhiteSpace(configuration.PromptMessage) ? GetDefaultPromptMessage(language) : configuration.PromptMessage,
            PromptTimeout,
            cancellationToken);
    }

    private static string NormalizeLanguage(string? language)
    {
        return string.Equals(language, "it", StringComparison.OrdinalIgnoreCase) ? "it" : "en";
    }

    private static string GetDefaultPromptMessage(string language)
    {
        return language == "it" ? "Stai ancora guardando?" : "Are you still watching?";
    }
}
