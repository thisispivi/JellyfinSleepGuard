using Jellyfin.Plugin.SleepGuard.Actions;
using Jellyfin.Plugin.SleepGuard.Configuration;
using Jellyfin.Plugin.SleepGuard.Rules;
using Jellyfin.Plugin.SleepGuard.Sessions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.SleepGuard;

/// <summary>
/// Registers all SleepGuard services into Jellyfin's dependency injection container.
/// </summary>
/// <remarks>
/// Rule registration order within each group is significant:
/// <list type="bullet">
///   <item><see cref="IGateRule"/> implementations are evaluated in registration order (UserScope first, then TimeWindow).</item>
///   <item><see cref="ITriggerRule"/> implementations are evaluated in registration order (ContinuousTime first, then AutoplayEpisode).</item>
/// </list>
/// Gate rules always run before trigger rules, regardless of registration order.
/// </remarks>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Configuration accessor — the only place that references Plugin.Instance.
        // All services that need config inject IPluginConfigurationAccessor instead.
        serviceCollection.AddSingleton<IPluginConfigurationAccessor>(
            _ => Plugin.Instance ?? throw new InvalidOperationException(
                "SleepGuard Plugin.Instance is null during service registration. This should not happen."));

        // Configuration validator — called by SessionMonitorService at startup to clamp bad values.
        serviceCollection.AddSingleton<PluginConfigurationValidator>();

        // Session tracking infrastructure
        serviceCollection.AddSingleton<PlaybackTrackerStore>();
        serviceCollection.AddSingleton<PlaybackEventClassifier>();

        // Gate rules (filter when SleepGuard is active)
        serviceCollection.AddSingleton<IGateRule, UserScopeRule>();
        serviceCollection.AddSingleton<IGateRule, TimeWindowRule>();

        // Trigger rules (detect why SleepGuard should act)
        serviceCollection.AddSingleton<ITriggerRule, ContinuousTimeRule>();
        serviceCollection.AddSingleton<ITriggerRule, AutoplayEpisodeRule>();

        // Session command gateway and actions
        serviceCollection.AddSingleton<ISessionCommandGateway, SessionCommandGateway>();
        serviceCollection.AddSingleton<PromptAction>();
        serviceCollection.AddSingleton<PauseAction>();
        serviceCollection.AddSingleton<StopAction>();

        // Main hosted service
        serviceCollection.AddHostedService<SessionMonitorService>();
    }
}
