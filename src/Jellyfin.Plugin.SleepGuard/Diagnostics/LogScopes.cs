using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SleepGuard.Diagnostics;

public static class LogScopes
{
    public static IDisposable? BeginSessionScope(this ILogger logger, string sessionId, string? deviceId)
    {
        return logger.BeginScope(new Dictionary<string, object?>
        {
            ["SessionId"] = sessionId,
            ["DeviceId"] = deviceId
        });
    }
}
