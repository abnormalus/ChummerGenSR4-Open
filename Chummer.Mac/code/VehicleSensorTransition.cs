#nullable enable
using System;

namespace Chummer;

// Mutates only the first recognized sensor's legacy eight-field projection.
// Catalog construction is supplied by the caller; no controls, files, accounting
// or persistence are owned here. A null result means no sensor changed.
internal static class VehicleSensorTransition
{
    private static readonly string[] Sizes = { "Microdrone Sensor", "Minidrone Sensor", "Small Drone Sensor", "Medium Drone Sensor", "Large Drone Sensor", "Vehicle Sensor", "Extra-Large Vehicle Sensor" };

    internal static Gear? Apply(Vehicle vehicle, bool increase, Func<string, Gear> createReplacement)
    {
        foreach (var sensor in vehicle.Gear)
        {
            int current = Array.IndexOf(Sizes, sensor.Name);
            if (current < 0) continue;
            int target = current + (increase ? 1 : -1);
            // Preserve first-recognized ordering even at an immovable endpoint.
            if (target < 0 || target >= Sizes.Length) return null;
            // Constructor failures precede all writes to the existing sensor.
            // Legacy factory side effects remain caller-owned, not rolled back.
            var replacement = createReplacement(Sizes[target]);
            sensor.Name = replacement.Name;
            sensor.Rating = replacement.Rating;
            sensor.Capacity = replacement.Capacity;
            sensor.Signal = replacement.Signal;
            sensor.Avail = replacement.Avail;
            sensor.Cost = replacement.Cost;
            sensor.Source = replacement.Source;
            sensor.Page = replacement.Page;
            return sensor;
        }
        return null;
    }
}
