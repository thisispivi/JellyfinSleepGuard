namespace Jellyfin.Plugin.SleepGuard.Configuration;

/// <summary>
/// Marks a <see cref="PluginConfiguration"/> property with the settings-page tab it belongs to.
/// This is a documentation and tooling aid — Jellyfin's XML serializer ignores it.
/// </summary>
/// <remarks>
/// Valid group values: <c>"Behavior"</c>, <c>"Customization"</c>, <c>"Developer"</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingsGroupAttribute : Attribute
{
    public SettingsGroupAttribute(string group) => Group = group;

    /// <summary>The settings tab this property belongs to.</summary>
    public string Group { get; }
}
