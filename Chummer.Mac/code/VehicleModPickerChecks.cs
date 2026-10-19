#nullable enable
using System;
using System.Linq;
using System.Xml;
using System.Globalization;
namespace Chummer;

internal static class VehicleModPickerChecks
{
    internal static void Run(Character character, Action<bool, string> check)
    {
        var books = character.Options.Books.ToArray();
        try
        {
            character.Options.Books.Clear(); character.Options.Books.Add("AR"); character.Options.Books.Add("SR4");
            using var prompt = new frmSelectVehicleMod(character, true) { VehicleCost = 13579, Body = 0, DeviceRating = 3 };
            var model = prompt.CreateModel(); model.SearchText = "Armor, Normal";
            check(model.CanAccept && model.MaximumRating == 20 && model.MinimumRating == 1 && model.IsRatingEnabled, "vehicle modification model retains catalog rating range");
            model.Rating = 100; check(model.Rating == 20, "vehicle modification rating clips at catalog maximum");
            model.SearchText = "Off-Road Tires"; model.Rating = 7;
            check(model.CanAccept && model.MaximumRating == 20 && model.Rating == 7 && model.RatingLabel == LanguageManager.Instance.GetString("Label_Qty"), "vehicle tires use the existing one-to-twenty quantity semantics");
            model.SearchText = "no \"quoted\" modification";
            check(model.IsEmpty && !model.CanAccept && model.Details == null && model.Accept(false) == null, "modification literal empty search clears stale rating details and acceptance");
            model.SearchText = "Armor, Normal"; character.Options.Books.Clear();
            check(model.Accept(false) == null, "modification acceptance revalidates changed sources");
            character.Options.Books.Add("AR"); character.Options.Books.Add("SR4");
            using var weapon = new frmSelectVehicleMod(character, true) { InputFile = "weapons", WeaponType = "Melee" };
            var weaponModel = weapon.CreateModel(); weaponModel.SearchText = "Weapon Focus"; weaponModel.Rating = 3;
            check(weaponModel.CanAccept && weaponModel.Details!.Cost == string.Format("{0:###,###,##0¥}", 30000) && weaponModel.Details.Availability.Contains("15"), "weapon focus native model retains rating cost and availability");
            int definitions = 0;
            foreach (string file in new[] { "vehicles", "weapons" })
            {
                var catalog = XmlManager.Instance.Load(file + ".xml");
                foreach (XmlNode node in catalog.SelectNodes("/chummer/mods/mod")!)
                {
                    if (node["category"]?.InnerText == "Special") continue;
                    string book = node["source"]!.InnerText;
                    if (!character.Options.Books.Contains(book)) character.Options.Books.Add(book);
                    // Isolate each definition so stale details or a different automatic
                    // first selection cannot hide an unsupported catalog expression.
                    var single = new XmlDocument(); single.LoadXml("<chummer><mods>" + node.OuterXml + "</mods></chummer>");
                    var one = new VehicleModPickerModel(character, true, file,
                        node["name"]!.InnerText == WeaponModificationNames.MeleePersonalizedGrip ? "Melee" : "Ranged",
                        new VehicleModPurchaseContext(13579, 357, 1499, 2, 123, 31, 17, 2), 0, true, single);
                    int maximum = node["rating"]!.InnerText == "qty" ? 20 : Convert.ToInt32(node["rating"]!.InnerText, CultureInfo.InvariantCulture);
                    one.Rating = maximum;
                    if (!one.CanAccept || one.SelectedEntry?.Name != node["name"]!.InnerText || one.MaximumRating != maximum || one.Rating != maximum)
                        throw new InvalidOperationException("Modification catalog model cannot prepare a canonical definition.");
                    definitions++;
                }
            }
            var vehicleCatalog = XmlManager.Instance.Load("vehicles.xml");
            int contexts = 0;
            foreach (int? device in new int?[] { null, 0, 1, 3, 6, 10 })
                foreach (bool modular in new[] { false, true })
                {
                    int current = device ?? 0, limit = modular ? 10 : device.HasValue ? device.Value + 2 : 0;
                    var expected = vehicleCatalog.SelectNodes("/chummer/mods/mod")!.Cast<XmlNode>().Where(node =>
                    {
                        if (node["category"]?.InnerText == "Special") return false;
                        foreach (string field in new[] { "response", "signal", "system", "firewall" })
                        {
                            if (node[field] == null) continue;
                            int value = Convert.ToInt32(node[field]!.InnerText, CultureInfo.InvariantCulture);
                            if (value <= current || (field == "response" || field == "signal") && value > limit) return false;
                        }
                        return true;
                    }).Select(node => node["name"]!.InnerText).OrderBy(name => name, StringComparer.Ordinal).ToArray();
                    var electronics = new VehicleModPickerModel(character, false, "vehicles", "",
                        new VehicleModPurchaseContext(13579, 357, 1499, 2, 123, 31, 17), device, modular);
                    var actual = electronics.Items.Select(item => item.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
                    if (!actual.SequenceEqual(expected)) throw new InvalidOperationException("Modification electronics eligibility differs from the legacy policy.");
                    contexts++;
                }
            check(contexts == 12, "vehicle modification electronic eligibility preserves all twelve unset ordinary and modular device contexts");
            check(definitions == 416, "all416 ordinary vehicle and weapon modifications prepare canonical details at their rating limits");
            weaponModel.Markup = 25; weaponModel.FreeCost = true; var accepted = weaponModel.Accept(true)!;
            check(accepted.Name == "Weapon Focus" && accepted.Rating == 3 && accepted.Markup == 25 && accepted.FreeCost && accepted.AddAgain,
                "shared modification accepted draft retains canonical rating and purchase choices");
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(books); }
    }
}
