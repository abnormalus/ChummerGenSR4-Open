#nullable enable
using System;
using System.Linq;
using System.Xml;

namespace Chummer;

internal static class AccessoryPickerChecks
{
    internal static void Run(Character character, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot();
        var previousBooks = character.Options.Books.ToArray();
        try
        {
            character.Options.Books.Clear(); character.Options.Books.Add("SR4");
            var melee = new AccessoryPickerModel(character, false, "", 500);
            check(melee.Items.Count > 0 && melee.Items.All(item => item.Definition["mount"]!.InnerText == ""),
                "mount-free accessory browsing preserves melee compatibility");
            melee.SearchText = "Laser Sight";
            check(melee.IsEmpty && !melee.CanAccept, "accessory search cannot bypass weapon mount restrictions");
            var model = new AccessoryPickerModel(character, true, "Top/Under/", 501) { SearchText = "Laser Sight", Markup = 25 };
            check(model.CanAccept && model.Details!.Cost == string.Format("{0:###,###,##0¥}", 125) && model.SelectedEntry!.Definition["mount"]!.InnerText == "Top/Under",
                "accessory preview preserves mount alternatives, markup and cost rounding");
            model.SearchText = "Smartgun System, Internal";
            check(model.Details!.Cost == string.Format("{0:###,###,##0¥}", Convert.ToInt32(501 * 1.25)),
                "internal smartgun preview retains weapon-relative pricing");
            model.FreeCost = true;
            var accepted = model.Accept(true);
            check(model.Details!.Cost == string.Format("{0:###,###,##0¥}", 0) && accepted!.FreeCost && accepted.Markup == 25 && accepted.AddAgain,
                "accessory acceptance preserves free purchase, markup and Add More intent");
            model.SearchText = "no \"quoted\" accessory";
            check(model.IsEmpty && model.Details == null && !model.CanAccept && model.Accept(false) == null,
                "quoted accessory searches clear details and cannot accept a stale item");
            model.SearchText = "Laser Sight"; character.Options.Books.Clear();
            check(model.Accept(false) == null && model.IsEmpty, "accessory acceptance rechecks enabled sources");
            character.Options.Books.Add("SR4");
            var malformed = new XmlDocument(); malformed.LoadXml("<chummer><accessories><accessory><name>Broken</name><mount/><source>SR4</source></accessory></accessories></chummer>");
            var broken = new AccessoryPickerModel(character, true, "", 0, malformed);
            check(broken.HasError && broken.Details == null && !broken.CanAccept,
                "malformed accessory definitions show a recoverable error without accepting stale details");
            var all = new AccessoryPickerModel(character, false, "Top/Under/Barrel", 1000);
            foreach (var item in all.Items) { all.SelectedEntry = item; if (!all.CanAccept) throw new InvalidOperationException("Enabled accessory preview failed."); }
            check(all.Items.Count > 10, "enabled accessory definitions calculate successfully through the shared picker");
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(previousBooks); }
        check(character.CaptureSnapshot().SequenceEqual(original), "accessory browsing leaves the original character unchanged");
    }
}
