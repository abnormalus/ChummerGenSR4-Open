#nullable enable
using System.Collections.Generic;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Retained adapter around Gear.Create's control-shaped constructor contract.
// Keep custom catalog fields, prompts/improvements and failure behavior until
// that constructor has an independently characterized model-only replacement.
internal static class VehicleSensorCatalog
{
    internal static Gear? Apply(Character character, Vehicle vehicle, bool increase)
    {
        // Load even for an endpoint/no-match, as the original editors did.
        var catalog = XmlManager.Instance.Load("gear.xml");
        return VehicleSensorTransition.Apply(vehicle, increase, name =>
        {
            var replacement = new Gear(character);
            var definition = catalog.SelectSingleNode("/chummer/gears/gear[name = \"" + name + "\" and category = \"Sensors\"]");
            // Missing definitions retain the legacy constructor's rejection;
            // do not silently substitute a differently sized sensor.
            replacement.Create(definition!, character, new Forms.TreeNode(), 0,
                new List<Weapon>(), new List<Forms.TreeNode>());
            return replacement;
        });
    }
}
