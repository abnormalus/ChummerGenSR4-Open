#nullable enable
using System;
using System.Xml;
using System.Xml.XPath;

namespace Chummer;

// Fixed expectations for the original installed-vehicle statistics, independent
// of picker preview formulas. Do not derive expected values from a new helper.
internal static class VehicleMovementChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var character = new Character();
        VehicleMod Mod(string body, bool installed = true, bool included = false)
        {
            var xml = new XmlDocument(); xml.LoadXml("<bonus>" + body + "</bonus>");
            return new VehicleMod(character) { Bonus = xml.DocumentElement, Rating = 2, Installed = installed, IncludedInVehicle = included };
        }
        void Case(string name, string accel, int speed, int baseArmor, params VehicleMod[] mods)
        {
            var vehicle = new Vehicle(character) { Accel = "10/20", Speed = 100, Body = 4, Armor = baseArmor };
            vehicle.Mods.AddRange(mods);
            string before = string.Join("|", vehicle.Mods.ConvertAll(m => m.Bonus?.OuterXml + ":" + m.Rating + ":" + m.Installed + ":" + m.IncludedInVehicle));
            check(vehicle.TotalAccel == accel && vehicle.TotalSpeed == speed,
                "Vehicle movement baseline " + name + " [accel=" + vehicle.TotalAccel + "; speed=" + vehicle.TotalSpeed + "]");
            check(vehicle.Accel == "10/20" && vehicle.Speed == 100 && vehicle.Body == 4 && vehicle.Armor == baseArmor &&
                before == string.Join("|", vehicle.Mods.ConvertAll(m => m.Bonus?.OuterXml + ":" + m.Rating + ":" + m.Installed + ":" + m.IncludedInVehicle)),
                "Vehicle movement reads preserve base statistics and modification inputs " + name);
        }
        Case("unmodified", "10/20", 100, 1);
        Case("fractional base multipliers", "15/30", 150, 1, Mod("<accel>0.5</accel><speed>0.5</speed>"));
        Case("separate rating expressions", "12/24", 100, 1, Mod("<accel>+Rating/+Rating*2</accel>"));
        Case("single expression changes walking only", "12/20", 100, 1, Mod("<accel>+Rating</accel>"));
        Case("minus selects expression branch", "8/20", 100, 1, Mod("<accel>-2</accel>"));
        Case("uninstalled ignored", "10/20", 100, 1, Mod("<accel>0.5</accel><speed>0.5</speed>", false));
        Case("included ignored", "10/20", 100, 1, Mod("<accel>0.5</accel><speed>0.5</speed>", true, true));
        Case("combined additions use original base", "17/34", 150, 1,
            Mod("<accel>+Rating/+Rating*2</accel>"), Mod("<accel>0.5</accel><speed>0.5</speed>"));
        Case("negative totals clamp", "0/0", 100, 1, Mod("<accel>-100/-100</accel>"));
        Case("armor penalty subtracts base fraction", "13/26", 130, 1,
            Mod("<accel>0.5</accel><speed>0.5</speed><armor>5</armor>"));
        Case("modified body prevents armor penalty", "15/30", 150, 1,
            Mod("<accel>0.5</accel><speed>0.5</speed><armor>5</armor><body>2</body>"));
        Case("base armor alone avoids penalty", "10/20", 100, 5);
        Case("fractional totals round upward", "11/21", 101, 1, Mod("<accel>0.01</accel><speed>0.01</speed>"));
        Case("negative speed clamps", "10/20", 0, 1, Mod("<speed>-2</speed>"));
        Case("armor equal to body avoids penalty", "15/30", 150, 1,
            Mod("<accel>0.5</accel><speed>0.5</speed><armor>4</armor>"));
        var malformed = new Vehicle(character) { Accel = "10/20", Speed = 100, Body = 4, Armor = 1 };
        var invalidMod = Mod("<accel>+</accel>"); malformed.Mods.Add(invalidMod);
        string invalidBonus = invalidMod.Bonus.OuterXml;
        bool rejected = false;
        try { _ = malformed.TotalAccel; }
        catch (XPathException) { rejected = true; }
        check(rejected && malformed.Accel == "10/20" && invalidMod.Bonus.OuterXml == invalidBonus,
            "Vehicle invalid acceleration expression fails without changing its inputs");
    }
}
