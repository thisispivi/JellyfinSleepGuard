using Jellyfin.Plugin.SleepGuard.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.SleepGuard;

/// <summary>
/// Main Jellyfin plugin entrypoint.
/// </summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static readonly Guid PluginId = Guid.Parse("7bb5959b-5a11-45da-b9db-52eed4456090");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "SleepGuard";

    public override string Description => "Pauses or stops playback after configurable sleep-friendly thresholds.";

    public override Guid Id => PluginId;

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "sleepguard",
            DisplayName = "SleepGuard",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
            EnableInMainMenu = false,
            MenuSection = "server",
            MenuIcon = "timer"
        };
    }
}
