#nullable enable
using System;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace Chummer;

internal static class VehicleModPreviewChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var context = new VehicleModPurchaseContext(13579, 357, 1499, 2, 123, 31, 17, 2);
        int count = 0, evaluated = 0, variable = 0;
        foreach (string file in new[] { "vehicles.xml", "weapons.xml" })
        {
            XmlDocument catalog = XmlManager.Instance.Load(file);
            XPathNavigator navigator = catalog.CreateNavigator()!;
            foreach (XmlNode definition in catalog.SelectNodes("/chummer/mods/mod")!)
            {
                count++;
                string expression = definition["cost"]!.InnerText;
                if (expression.StartsWith("Variable", StringComparison.Ordinal)) { variable++; continue; }
                string rawRating = definition["rating"]!.InnerText;
                int maximum = rawRating == "qty" ? 20 : Convert.ToInt32(rawRating, CultureInfo.InvariantCulture);
                foreach (int rating in new[] { maximum == 0 ? 0 : 1, maximum })
                    foreach (int markup in new[] { -99, 0, 25, 1000 })
                        foreach (bool free in new[] { false, true })
                        {
                            // Independent transcription of the released picker, including its
                            // integer-before-markup boundary; do not derive expected values
                            // through the new context/preview implementation.
                            string expanded = expression.Replace("Rating", rating.ToString())
                                .Replace("Vehicle Cost", "13579").Replace("Weapon Cost", "357")
                                .Replace("Total Cost", "1499").Replace("Body", "2").Replace("Speed", "123")
                                .Replace("Accel(Running)", "31").Replace("Accel", "17");
                            if (free) expanded = "0";
                            string legacyValue = string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(expanded));
                            int legacy = Convert.ToInt32(Convert.ToDouble(legacyValue, GlobalOptions.Instance.CultureInfo));
                            legacy *= 2;
                            legacy = Convert.ToInt32(legacy * (1 + markup / 100.0));
                            if (VehicleModPurchasePreview.FixedCost(navigator, expression, context, rating, markup, free) != legacy)
                                throw new InvalidOperationException("Modification cost characterization differs.");
                            evaluated++;
                        }
            }
        }
        check(count == 420 && variable == 2 && evaluated == 6688,
            "all420 vehicle and weapon modification costs preserve rounding markup and free preview across rated boundaries");
        var probe = new XmlDocument(); probe.LoadXml("<probe/>");
        var nav = probe.CreateNavigator()!;
        check(VehicleModPurchasePreview.FixedCost(nav, "2.5", context, 0, 25, false) == 5 &&
            VehicleModPurchasePreview.FixedCost(nav, "3.5", context, 0, 25, false) == 10,
            "modification cost rounds midpoint to even before multiplier and markup");
    }
}
