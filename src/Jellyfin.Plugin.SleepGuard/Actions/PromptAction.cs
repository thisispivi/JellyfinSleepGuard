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
        var language = NormalizeLanguage(configuration.Language);

        return _gateway.SendPromptAsync(
            tracker.SessionId,
            string.IsNullOrWhiteSpace(configuration.PromptHeader) ? "SleepGuard" : configuration.PromptHeader,
            string.IsNullOrWhiteSpace(configuration.PromptMessage) ? GetDefaultPromptMessage(language) : configuration.PromptMessage,
            TimeSpan.FromSeconds(Math.Max(1, configuration.PromptTimeoutSeconds)),
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
