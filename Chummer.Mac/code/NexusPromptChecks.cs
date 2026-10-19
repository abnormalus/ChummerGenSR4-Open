#nullable enable
using System;
using System.Linq;
namespace Chummer;

// Retain the contracts characterized against the former form, including120
// component tier boundaries. Existing high-response pricing remains unchanged.
internal static class NexusPromptChecks
{
    internal static void Run(Character character, Action<bool, string> check)
    {
        var before = character.CaptureSnapshot(); var draft = new NexusDraft(true);
        check(draft.Processor == 10 && draft.System == 1 && draft.Response == 1 && draft.Firewall == 1 && draft.Signal == 3 && draft.Persona == 3 && draft.PersonaMinimum == 3,
            "nexus draft retains all six initial values and persona bound");
        var defaultGear = draft.Selection!.CreateGear(character);
        check(defaultGear.Cost == "975" && defaultGear.Category == "Nexus" && defaultGear.Avail == "0" && defaultGear.Children.Count == 5 &&
            defaultGear.Children.All(item => item.Cost == "0" && item.Category == "Nexus Module" && item.Source == "UN" && item.Page == "50"),
            "default accepted nexus retains cost and five zero-cost UN module children");
        draft.System = 10;
        check(draft.PersonaMinimum == 30 && draft.Persona == 30, "nexus system raises persona minimum to three times system");
        draft.Persona = 50; draft.System = 1;
        check(draft.PersonaMinimum == 3 && draft.Persona == 50, "lower nexus system preserves an explicitly higher persona limit");
        int cases = 0;
        foreach (int p in new[] { 10, 50 })
            foreach (int tier in new[] { 1, 3, 4, 6, 7, 10 })
                foreach (int sig in Enumerable.Range(1, 10))
                {
                    draft.Processor = p; draft.System = tier; draft.Response = tier; draft.Firewall = tier; draft.Persona = Math.Max(30, tier * 3); draft.Signal = sig;
                    int people = Math.Max(30, tier * 3);
                    int responseCost = tier <= 3 ? tier * p * 50 : tier <= 6 ? tier * p * 100 : 0;
                    int systemCost = tier * people * (tier <= 3 ? 25 : tier <= 6 ? 50 : 300);
                    int firewallCost = tier * p * (tier <= 3 ? 25 : tier <= 6 ? 50 : 250);
                    int signalCost = new[] { 10, 50, 150, 500, 1000, 3000, 6500, 8750, 12250, 17250 }[sig - 1];
                    var gear = draft.Selection!.CreateGear(character);
                    if (gear.Cost != (responseCost + systemCost + firewallCost + signalCost).ToString()) throw new InvalidOperationException("Nexus price characterization differs.");
                    string suffix = tier > 6 ? "F" : "";
                    if (gear.Children[0].Avail != tier * 4 + suffix || gear.Children[2].Avail != Convert.ToInt32(Math.Ceiling(people / (tier <= 3 ? 5.0 : 2.0))) + suffix ||
                        gear.Children[3].Avail != Convert.ToInt32(Math.Ceiling(p / (tier <= 3 ? 10.0 : 5.0))) + suffix) throw new InvalidOperationException("Nexus availability characterization differs.");
                    cases++;
                }
        check(cases == 120, "nexus costs and module availability retain120 component tier boundary combinations");
        draft.Free = true;
        check(draft.Selection!.CreateGear(character).Cost == "0" && draft.Selection.Calculate().ResponseAvailability.EndsWith("F"), "free nexus retains component availability while zeroing its parent cost");
        draft.System = null;
        check(!draft.CanAccept && draft.Selection == null && draft.Cost == "" && draft.PersonaMinimum == 30, "cleared nexus system blocks construction without guessing a persona bound");
        draft.System = 1; draft.Persona = null;
        check(!draft.CanAccept && draft.Selection == null, "cleared nexus persona blocks construction");
        draft.Persona = 3; draft.Processor = 0; draft.Response = 100; draft.Signal = 0; draft.Firewall = 100;
        check(draft.Processor == 10 && draft.Response == 10 && draft.Signal == 1 && draft.Firewall == 10, "nexus draft clips integer fields at established bounds");
        var creation = new NexusDraft(false) { Free = true };
        check(!creation.Free && creation.Quote!.Cost == 975, "nexus caller without free purchase retains paid draft behavior");
        check(character.CaptureSnapshot().AsSpan().SequenceEqual(before), "nexus preparation never changes owned character data");
    }
}
