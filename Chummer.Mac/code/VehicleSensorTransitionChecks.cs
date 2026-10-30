#nullable enable
using System;
using System.Collections.Generic;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Fixed custom-definition and failure contracts complement the unchanged
// ninety recorded editor expectations; no expected values use the new policy.
internal static class VehicleSensorTransitionChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var character = new Character();
        var definition = new XmlDocument();
        definition.LoadXml("<gear><name>Minidrone Sensor</name><category>Sensors</category><rating>0</rating><capacity>42</capacity><signal>7</signal><avail>17R</avail><cost>123</cost><source>SR4</source><page>777</page></gear>");
        Gear Construct(XmlNode? node)
        {
            var gear = new Gear(character);
            gear.Create(node!, character, new Forms.TreeNode(), 0, new List<Weapon>(), new List<Forms.TreeNode>());
            return gear;
        }
        byte[] before = character.CaptureSnapshot();
        var custom = Construct(definition.DocumentElement);
        check(custom.Name == "Minidrone Sensor" && custom.Rating == 0 && custom.Capacity == "42" && custom.Signal == 7 && custom.Avail == "17R" && custom.Cost == "123" && custom.Source == "SR4" && custom.Page == "777" && before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
            "Legacy sensor constructor preserves fixed custom fields without character mutation");
        bool missingRejected = false;
        try { _ = Construct(null); }
        catch (NullReferenceException) { missingRejected = true; }
        check(missingRejected && before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
            "Legacy missing sensor definition rejects before changing the character");
        var vehicle = new Vehicle(character);
        var sensor = new Gear(character) { Name = "Microdrone Sensor", Capacity = "99", Notes = "Retain identity" };
        vehicle.Gear.Add(sensor); string id = sensor.InternalId;
        var failure = new InvalidOperationException("Synthetic constructor failure"); bool propagated = false;
        try { VehicleSensorTransition.Apply(vehicle, true, _ => throw failure); }
        catch (InvalidOperationException error) { propagated = ReferenceEquals(error, failure); }
        check(propagated && sensor.Name == "Microdrone Sensor" && sensor.Capacity == "99" && sensor.InternalId == id && sensor.Notes == "Retain identity" && ReferenceEquals(vehicle.Gear[0], sensor),
            "Sensor factory failure propagates unchanged before existing sensor writes");
        int calls = 0;
        vehicle.Gear.Add(new Gear(character) { Name = "Medium Drone Sensor", Capacity = "88" });
        var changed = VehicleSensorTransition.Apply(vehicle, false, _ => { calls++; return custom; });
        check(changed == null && calls == 0 && vehicle.Gear[1].Capacity == "88",
            "Sensor boundary skips construction and does not continue to a later recognized sensor");
        var unknown = new Gear(character) { Name = "Unknown sensor", Capacity = "55" }; vehicle.Gear.Insert(0, unknown);
        string requested = "";
        changed = VehicleSensorTransition.Apply(vehicle, true, name => { requested = name; return Construct(definition.DocumentElement); });
        check(requested == "Minidrone Sensor" && ReferenceEquals(changed, sensor) && sensor.InternalId == id && sensor.Capacity == "42" && sensor.Signal == 7 && sensor.Avail == "17R" && sensor.Cost == "123" && sensor.Page == "777" && unknown.Capacity == "55" && vehicle.Gear[2].Capacity == "88",
            "Sensor model transition skips unknown entries and applies custom constructor fields in place");
    }
}
