#nullable enable
using System;
namespace Chummer;

// Existing nexus persistence encodes ratings in localized module names. Keep this
// representation while moving calculation and accepted construction out of UI.
internal sealed record NexusConfiguration(int Processor, int System, int Response, int Firewall, int Persona, int Signal, bool Free)
{
    internal NexusQuote Calculate()
    {
        string forbiddenResponse = Response > 6 ? "F" : "";
        // The legacy high-response branch multiplies a zero accumulator. Preserve
        // that observed price here pending a separately sourced rules correction;
        // a control migration must not silently invent a new charge.
        int responseCost = Response <= 3 ? Response * Processor * 50 : Response <= 6 ? Response * Processor * 100 : 0;
        int systemCost = System * Persona * (System <= 3 ? 25 : System <= 6 ? 50 : 300);
        int firewallCost = Firewall * Processor * (Firewall <= 3 ? 25 : Firewall <= 6 ? 50 : 250);
        int signalCost = Signal switch { 2 => 50, 3 => 150, 4 => 500, 5 => 1000, 6 => 3000, 7 => 6500, 8 => 8750, 9 => 12250, 10 => 17250, _ => 10 };
        return new NexusQuote(Free ? 0 : responseCost + systemCost + firewallCost + signalCost,
            (Response * 4).ToString() + forbiddenResponse,
            Convert.ToInt32(Math.Ceiling(Persona / (System <= 3 ? 5.0 : 2.0))).ToString() + (System > 6 ? "F" : ""),
            Convert.ToInt32(Math.Ceiling(Processor / (Firewall <= 3 ? 10.0 : 5.0))).ToString() + (Firewall > 6 ? "F" : ""));
    }
    internal Gear CreateGear(Character character)
    {
        var quote = Calculate();
        string T(string key) => LanguageManager.Instance.GetString(key);
        var nexus = new Gear(character)
        {
            Name = T("String_SelectNexus_Nexus") + " (" + T("String_SelectNexus_Processor") + " " + Processor + ")",
            Cost = quote.Cost.ToString(),
            Avail = "0",
            Category = "Nexus",
            Source = "UN",
            Page = "50"
        };
        void Module(string name, string availability) => nexus.Children.Add(new Gear(character)
        {
            Name = name,
            Category = "Nexus Module",
            Cost = "0",
            Avail = availability,
            Source = "UN",
            Page = "50"
        });
        string Rated(string key, int value) => T(key) + " (" + T("String_Rating") + " " + value + ")";
        Module(Rated("String_Response", Response), quote.ResponseAvailability);
        Module(Rated("String_Signal", Signal), "0");
        Module(Rated("String_System", System), quote.SystemAvailability);
        Module(Rated("String_Firewall", Firewall), quote.FirewallAvailability);
        Module(T("String_SelectNexus_PersonaLimit") + " (" + Persona + ")", "0");
        return nexus;
    }
}
internal sealed record NexusQuote(int Cost, string ResponseAvailability, string SystemAvailability, string FirewallAvailability);
