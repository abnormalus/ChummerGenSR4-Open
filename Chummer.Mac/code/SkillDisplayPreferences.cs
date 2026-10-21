#nullable enable
using System;
namespace Chummer;

// View preference only: never changes character data or its save format.
internal static class SkillDisplayPreferences
{
    internal static event Action? Changed;
    internal static bool PerceptionExpanded
    {
        get => bool.TryParse(SettingsStore.GetValue("Software\\Chummer", "perceptionsubcategoriesexpanded"), out var value) && value;
        set
        {
            if (value == PerceptionExpanded) return;
            SettingsStore.SetValue("Software\\Chummer", "perceptionsubcategoriesexpanded", value.ToString());
            Changed?.Invoke();
        }
    }
}
