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
    public static readonly Guid PluginId = Guid.Parse("7f4c92b7-bec2-4a9f-95c6-ff3f17bcd58a");

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
