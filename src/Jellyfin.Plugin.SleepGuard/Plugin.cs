using Jellyfin.Plugin.SleepGuard.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.SleepGuard;

/// <summary>
/// Main Jellyfin plugin entrypoint.
/// Implements <see cref="IPluginConfigurationAccessor"/> so the singleton instance can be
/// injected into services without any other code referencing <c>Plugin.Instance</c> directly.
/// </summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IPluginConfigurationAccessor
{
    /// <summary>Stable plugin identifier; must match <c>guid</c> in <c>build.yaml</c> and <c>manifest.json</c>.</summary>
    public static readonly Guid PluginId = Guid.Parse("7bb5959b-5a11-45da-b9db-52eed4456090");

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Jellyfin application paths.</param>
    /// <param name="xmlSerializer">Serializer used for the configuration file.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// The active plugin instance. Set once during construction; used only by
    /// <see cref="PluginServiceRegistrator"/> to register the <see cref="IPluginConfigurationAccessor"/>
    /// singleton. No other code should access this property.
    /// </summary>
    internal static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "SleepGuard";

    /// <inheritdoc />
    public override string Description => "Pauses or stops playback after configurable sleep-friendly thresholds.";

    /// <inheritdoc />
    public override Guid Id => PluginId;

    /// <inheritdoc />
    public PluginConfiguration GetConfiguration() => Configuration;

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "sleepguardconfiguration",
            DisplayName = "SleepGuard",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
            EnableInMainMenu = false,
            MenuSection = "server",
            MenuIcon = "timer"
        };

        yield return new PluginPageInfo
        {
            Name = "sleepguardlogo.png",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.logo.png",
            EnableInMainMenu = false
        };
    }
}
