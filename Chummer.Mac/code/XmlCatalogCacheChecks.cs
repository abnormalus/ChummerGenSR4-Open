#nullable enable
using System;
using System.IO;
using System.Xml;

namespace Chummer;

// Characterize the public loader before changing its process-wide cache lifetime.
internal static class XmlCatalogCacheChecks
{
    internal static void Run(Action<bool, string> check)
    {
        const string item = "Synthetic cache isolation item";
        const string query = "/chummer/gears/gear[name='" + item + "']";
        string file = Path.Combine(MacPaths.CustomDataDirectory, "custom_cache_" + Guid.NewGuid().ToString("N") + "_gear.xml");
        Directory.CreateDirectory(MacPaths.CustomDataDirectory);
        XmlDocument baseline = XmlManager.Instance.Load("gear.xml");
        string original = baseline.OuterXml;
        try
        {
            baseline.DocumentElement!.SetAttribute("synthetic-consumer-edit", "true");
            check(XmlManager.Instance.Load("gear.xml").OuterXml == original,
                "Catalog consumers cannot mutate the cached merged document");
            File.WriteAllText(file, "<chummer><gears><gear><name>" + item + "</name><cost>17</cost></gear></gears></chummer>");
            XmlDocument first = XmlManager.Instance.Load("gear.xml");
            check(first.SelectNodes(query)!.Count == 1 && first.SelectSingleNode(query + "/cost")!.InnerText == "17",
                "Warm catalog cache reads newly added custom data");
            check(XmlManager.Instance.Load("gear.xml").SelectNodes(query)!.Count == 1,
                "Repeated custom catalog loads do not accumulate duplicate items");
            File.WriteAllText(file, "<chummer><gears><gear><name>" + item + "</name><cost>23</cost></gear></gears></chummer>");
            check(XmlManager.Instance.Load("gear.xml").SelectSingleNode(query + "/cost")!.InnerText == "23" &&
                first.SelectSingleNode(query + "/cost")!.InnerText == "17",
                "Custom edits refresh future catalog loads without mutating prior consumers");
            string medkit = new XmlDocument { InnerXml = original }.SelectSingleNode("/chummer/gears/gear[name='Medkit']")!.OuterXml;
            File.WriteAllText(file, "<chummer><gears><gear><name>Medkit</name><cost>999999</cost></gear></gears></chummer>");
            XmlDocument collision = XmlManager.Instance.Load("gear.xml");
            check(collision.SelectNodes("/chummer/gears/gear[name='Medkit']")!.Count == 1 &&
                collision.SelectSingleNode("/chummer/gears/gear[name='Medkit']")!.OuterXml == medkit,
                "Custom catalog additions preserve existing canonical definitions on name collision");
            File.WriteAllText(file, "<chummer><gears>");
            bool malformedRejected = false;
            try { XmlManager.Instance.Load("gear.xml"); }
            catch (XmlException) { malformedRejected = true; }
            check(malformedRejected && first.SelectSingleNode(query + "/cost")!.InnerText == "17",
                "Malformed custom catalog fails explicitly without altering prior consumer documents");
            File.Delete(file);
            check(XmlManager.Instance.Load("gear.xml").OuterXml == original,
                "Removing custom data restores the unchanged cached base catalog");
        }
        finally { if (File.Exists(file)) File.Delete(file); }
    }
}
