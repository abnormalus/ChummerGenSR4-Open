using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Fixed expected summaries exercise the original editor methods before extraction.
// Controls hold unattached synthetic powers: the check must not purchase or save one.
internal static class PowerPointSummaryChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);

    internal static void Run(Character source, Action<bool, string> check)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (string culture in new[] { "en-US", "de-DE" })
            foreach (bool career in new[] { false, true })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
                character.Created = career; character.Improvements.Clear(); character.Powers.Clear();
                character.AdeptEnabled = true; character.MagicianEnabled = true; character.MAGAdept = 3;
                using Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
                var panel = Field<Forms.Panel>(editor, "panPowers");
                // No Show/Load event is needed: directly exercise the actual summary
                // against controlled rows without unrelated editor initialization.
                var snapshot = character.CaptureSnapshot();
                foreach (decimal[] costs in new[] { Array.Empty<decimal>(), new[] { 1.50m, 0.25m }, new[] { 4m } })
                {
                    foreach (Forms.Control row in panel.Controls.Cast<Forms.Control>().ToArray()) row.Dispose();
                    panel.Controls.Clear();
                    foreach (decimal cost in costs)
                    {
                        var row = new PowerControl();
                        typeof(PowerControl).GetField("_objPower", Flags).SetValue(row,
                            new Power(character) { PointsPerLevel = cost, Rating = 1 });
                        panel.Controls.Add(row);
                    }
                    editor.GetType().GetMethod("CalculatePowerPoints", Flags).Invoke(editor, null);
                    // The legacy length-limited trimming preserves the scale of -1.0.
                    // Keep this observed format rather than normalizing it during extraction.
                    string remaining = costs.Length == 0 ? "3" : costs.Length == 2 ? (culture == "de-DE" ? "1,25" : "1.25") : (culture == "de-DE" ? "-1,0" : "-1.0");
                    string expected = "3 (" + remaining + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
                    string actual = Field<Forms.Label>(editor, "lblPowerPoints").Text;
                    check(actual == expected,
                        "Legacy power-point summary preserves fixed mystic budget/fraction/overspend culture=" + culture + " career=" + career + " rows=" + costs.Length + " actual=" + actual + " expected=" + expected);
                    check(snapshot.SequenceEqual(character.CaptureSnapshot()),
                        "Power-point summary leaves character snapshot unchanged culture=" + culture + " career=" + career + " rows=" + costs.Length);
                }
                foreach (Forms.Control row in panel.Controls.Cast<Forms.Control>().ToArray()) row.Dispose();
                panel.Controls.Clear();
                character.MagicianEnabled = false;
                editor.GetType().GetMethod("CalculatePowerPoints", Flags).Invoke(editor, null);
                string ordinaryBudget = character.MAG.TotalValue.ToString();
                check(Field<Forms.Label>(editor, "lblPowerPoints").Text == ordinaryBudget + " (" + ordinaryBudget + " " + LanguageManager.Instance.GetString("String_Remaining") + ")",
                    "Legacy ordinary adept summary uses full Magic instead of mystic split culture=" + culture + " career=" + career);

                character.MagicianEnabled = true;
                character.Improvements.Add(new Improvement { ImproveType = Improvement.ImprovementType.AdeptPowerPoints, Value = 2, Enabled = true });
                character.Improvements.Add(new Improvement { ImproveType = Improvement.ImprovementType.AdeptPowerPoints, Value = 7, Enabled = false });
                var discounted = new PowerControl();
                typeof(PowerControl).GetField("_objPower", Flags).SetValue(discounted,
                    new Power(character) { PointsPerLevel = 2m, Rating = 1, DiscountedAdeptWay = true, DiscountedGeas = true });
                panel.Controls.Add(discounted);
                Field<Forms.Label>(discounted, "lblPowerPoints").Text = "stale synthetic points";
                var dirty = editor.GetType().GetField("_blnIsDirty", Flags);
                dirty.SetValue(editor, false);
                snapshot = character.CaptureSnapshot();
                editor.GetType().GetMethod("CalculatePowerPoints", Flags).Invoke(editor, null);
                string discountedRemaining = culture == "de-DE" ? "4,00" : "4.00";
                check(Field<Forms.Label>(editor, "lblPowerPoints").Text == "5 (" + discountedRemaining + " " + LanguageManager.Instance.GetString("String_Remaining") + ")",
                    "Legacy power summary combines enabled improvement and both discounts but ignores disabled improvement culture=" + culture + " career=" + career);
                check(snapshot.SequenceEqual(character.CaptureSnapshot()),
                    "Discounted power summary preserves character snapshot culture=" + culture + " career=" + career);
                check(!(bool)dirty.GetValue(editor),
                    "Power-point summary refresh preserves clean editor state culture=" + culture + " career=" + career);
                string onePoint = culture == "de-DE" ? "1,00" : "1.00";
                check(Field<Forms.Label>(discounted, "lblPowerPoints").Text == onePoint + " / " + LanguageManager.Instance.GetString("Label_Power_Level") + " = " + onePoint,
                    "Power-point summary refreshes stale per-power labels culture=" + culture + " career=" + career);
            }
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }
}
