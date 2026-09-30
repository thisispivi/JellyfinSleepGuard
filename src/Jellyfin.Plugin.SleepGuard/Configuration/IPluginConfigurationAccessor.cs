namespace Jellyfin.Plugin.SleepGuard.Configuration;

/// <summary>
/// Provides a snapshot of the current plugin configuration.
/// Injected into services that need settings, replacing the <c>Plugin.Instance</c> static accessor.
/// </summary>
public interface IPluginConfigurationAccessor
{
    /// <summary>
    /// Returns the current plugin configuration.
    /// Callers should capture the result once per logical operation to ensure a consistent view.
    /// </summary>
    /// <returns>The live configuration object.</returns>
    PluginConfiguration GetConfiguration();
}
