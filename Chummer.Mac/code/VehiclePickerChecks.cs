#nullable enable
using System;
using System.Linq;
using System.Xml;
namespace Chummer;
internal static class VehiclePickerChecks
{
    internal static void Run(Character character, Action<bool,string> check)
    {
        var original = character.CaptureSnapshot(); var sources = character.Options.Books.ToArray(); string category = VehiclePickerModel.LastCategory;
        try
        {
            character.Options.Books.Clear(); character.Options.Books.Add("SR4");
            var model = new VehiclePickerModel(character, true) { SearchText = "Dodge Scoot" };
            check(model.CanAccept && model.SelectedEntry?.Name == "Dodge Scoot (Scooter)", "vehicle catalog model finds enabled-source canonical identity");
            int baseCost = Convert.ToInt32(model.SelectedEntry!.Definition["cost"]!.InnerText);
            model.UsedVehicle = true; model.UsedDiscount = 35; model.Markup = 25;
            int expectedUsed = Convert.ToInt32(Math.Ceiling((double)baseCost * (double)(1 - 35m/100)));
            check(model.UsedCost == expectedUsed && model.Details!.Cost == string.Format("{0:###,###,##0¥}", Convert.ToInt32(expectedUsed * 1.25)),
                "vehicle used price remains separate from rounded career markup");
            check(VehiclePurchasePreview.UsedAvailability("8R") == "12R" && VehiclePurchasePreview.UsedAvailability("12F") == "16F" && VehiclePurchasePreview.UsedAvailability("0") == "4",
                "used vehicle availability adds four while preserving restricted and forbidden suffixes");
            model.UsedDiscount = 0; check(model.UsedDiscount == 20, "used vehicle discount clips at historical minimum");
            model.UsedDiscount = 100; check(model.UsedDiscount == 50, "used vehicle discount clips at historical maximum");
            model.FreeCost = true; check(model.Details!.Cost == string.Format("{0:###,###,##0¥}", 0) && model.Accept(true)!.AddAgain,
                "vehicle free preview and Add More retain caller intent");
            model.SearchText = "nonexistent \"quoted\" vehicle";
            check(model.IsEmpty && model.Details == null && !model.CanAccept && model.Accept(false) == null, "vehicle literal quote search clears stale selection and details");
            model.SearchText = "Dodge Scoot"; character.Options.Books.Clear();
            check(model.Accept(false) == null, "vehicle acceptance revalidates a disabled catalog source");
            var catalog = XmlManager.Instance.Load("vehicles.xml"); int count = 0;
            foreach (XmlNode vehicle in catalog.SelectNodes("/chummer/vehicles/vehicle")!)
            {
                int cost = Convert.ToInt32(vehicle["cost"]!.InnerText);
                foreach (int discount in new[] { 20,35,50 })
                {
                    int legacy = Convert.ToInt32(Math.Ceiling((double)cost * (double)(1 - (decimal)discount / 100)));
                    if (VehiclePurchasePreview.UsedCost(cost, discount) != legacy) throw new InvalidOperationException("Vehicle price characterization differs.");
                }
                count++;
            }
            check(count == 483, "all 483 catalog vehicle prices retain legacy used-discount ceiling boundaries");
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(sources); VehiclePickerModel.LastCategory = category; }
        check(character.CaptureSnapshot().AsSpan().SequenceEqual(original), "vehicle catalog preparation never mutates the live character");
    }
}
