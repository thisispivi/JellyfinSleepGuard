using System.Reflection;
using Jellyfin.Plugin.SleepGuard.Configuration;

namespace Jellyfin.Plugin.SleepGuard.Tests;

/// <summary>
/// Verifies that every public settable property on <see cref="PluginConfiguration"/>
/// is annotated with exactly one <see cref="SettingsGroupAttribute"/>.
/// This guards against forgetting to annotate new properties when they are added.
/// </summary>
public sealed class PluginConfigurationGroupingTests
{
    private static readonly IReadOnlyList<PropertyInfo> SettableProperties =
        typeof(PluginConfiguration)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

    [Fact]
    public void AllSettablePropertiesHaveExactlyOneSettingsGroupAttribute()
    {
        var violations = new List<string>();

        foreach (var property in SettableProperties)
        {
            var count = property.GetCustomAttributes<SettingsGroupAttribute>().Count();
            if (count != 1)
            {
                violations.Add($"{property.Name}: found {count} [SettingsGroup] attribute(s), expected 1");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Properties missing or duplicating [SettingsGroup]:\n  {string.Join("\n  ", violations)}");
    }

    [Fact]
    public void DeveloperModeIsInBehaviorGroup()
    {
        var property = typeof(PluginConfiguration)
            .GetProperty(nameof(PluginConfiguration.DeveloperMode))!;

        var group = property.GetCustomAttribute<SettingsGroupAttribute>()?.Group;

        Assert.Equal("Behavior", group);
    }

    [Fact]
    public void AllGroupValuesAreKnown()
    {
        var knownGroups = new HashSet<string>(StringComparer.Ordinal) { "Behavior", "Customization", "Developer" };
        var unknownGroups = new List<string>();

        foreach (var property in SettableProperties)
        {
            var group = property.GetCustomAttribute<SettingsGroupAttribute>()?.Group;
            if (group is not null && !knownGroups.Contains(group))
            {
                unknownGroups.Add($"{property.Name}: \"{group}\"");
            }
        }

        Assert.True(
            unknownGroups.Count == 0,
            $"Properties with unknown group values:\n  {string.Join("\n  ", unknownGroups)}");
    }

    [Fact]
    public void DeveloperPropertiesAreInDeveloperGroup()
    {
        // These properties must remain in the "Developer" group so they stay hidden
        // behind the DeveloperMode gate in the settings UI.
        var expectedDeveloperProperties = new[]
        {
            nameof(PluginConfiguration.MaxContinuousSeconds),
            nameof(PluginConfiguration.DryRun),
            nameof(PluginConfiguration.ActionRepeatCount),
            nameof(PluginConfiguration.ActionRepeatIntervalSeconds),
            nameof(PluginConfiguration.LogProgressEvents),
            nameof(PluginConfiguration.LogRuleChecks),
        };

        var violations = new List<string>();

        foreach (var name in expectedDeveloperProperties)
        {
            var property = typeof(PluginConfiguration).GetProperty(name);
            if (property is null)
            {
                violations.Add($"{name}: property not found");
                continue;
            }

            var group = property.GetCustomAttribute<SettingsGroupAttribute>()?.Group;
            if (group != "Developer")
            {
                violations.Add($"{name}: expected \"Developer\" but found \"{group ?? "(none)"}\"");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Developer properties not in the \"Developer\" group:\n  {string.Join("\n  ", violations)}");
    }
}
