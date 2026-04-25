using Jellyfin.Plugin.SleepGuard.Actions;
using Jellyfin.Plugin.SleepGuard.Rules;
using Jellyfin.Plugin.SleepGuard.Sessions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.SleepGuard;

/// <summary>
/// Registers SleepGuard services in Jellyfin's dependency injection container.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<PlaybackTrackerStore>();
        serviceCollection.AddSingleton<PlaybackEventClassifier>();
        serviceCollection.AddSingleton<ISleepRule, UserScopeRule>();
        serviceCollection.AddSingleton<ISleepRule, TimeWindowRule>();
        serviceCollection.AddSingleton<ISleepRule, ContinuousTimeRule>();
        serviceCollection.AddSingleton<ISleepRule, AutoplayEpisodeRule>();
        serviceCollection.AddSingleton<ISessionCommandGateway, SessionCommandGateway>();
        serviceCollection.AddSingleton<PromptAction>();
        serviceCollection.AddSingleton<PauseAction>();
        serviceCollection.AddSingleton<StopAction>();
        serviceCollection.AddHostedService<SessionMonitorService>();
    }
}
