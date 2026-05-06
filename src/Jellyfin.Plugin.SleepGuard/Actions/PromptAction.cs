using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Actions;

public sealed class PromptAction : ISleepAction
{
    private static readonly TimeSpan PromptTimeout = TimeSpan.FromSeconds(8);

    private readonly ISessionCommandGateway _gateway;

    public PromptAction(ISessionCommandGateway gateway)
    {
        _gateway = gateway;
    }

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
