using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Sessions;

namespace Jellyfin.Plugin.SleepGuard.Actions;

/// <summary>
/// A command SleepGuard can send to a playback session once a trigger rule has fired.
/// </summary>
public interface ISleepAction
{
    /// <summary>
    /// Executes the action against the session tracked by <paramref name="tracker"/>.
    /// </summary>
    /// <param name="tracker">Tracker of the session to act on.</param>
    /// <param name="configuration">Configuration snapshot for this evaluation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the command has been dispatched.</returns>
    Task ExecuteAsync(PlaybackTracker tracker, PluginConfiguration configuration, CancellationToken cancellationToken);
}
