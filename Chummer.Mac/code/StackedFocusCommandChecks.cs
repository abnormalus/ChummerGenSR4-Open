#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Fixed success/Force references were recorded against the original handlers.
// One-selection cancellation now expects the explicit repair: no model edits.
internal static class StackedFocusCommandChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static void Run(Character source, Action<bool, string> check, Forms.Form? owner = null, Action<Forms.Form, Character, bool, int>? afterScenario = null)
    {
        foreach (bool career in new[] { false, true })
        foreach (int scenario in new[] { 0, 1, 2, 3, 4, 5 })
        {
            bool selectOne = scenario == 1, rejectForce = scenario == 2, success = scenario >= 3;
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career;
            character.Gear.Clear(); character.StackedFoci.Clear();
            var first = new Gear(character) { Name = "Synthetic first focus", Category = "Foci", MaxRating = 6, Rating = 2 };
            var second = new Gear(character) { Name = "Synthetic second focus", Category = "Foci", MaxRating = 6, Rating = 2 };
            if (rejectForce || scenario == 4) { first.Rating = 4; second.Rating = 3; }
            first.Cost = "100"; second.Cost = "200";
            var third = new Gear(character) { Name = "Synthetic third focus", Category = "Metamagic Foci", MaxRating = 6, Rating = 2, Cost = "300" };
            character.Gear.Add(first); character.Gear.Add(second);
            if (scenario == 5) character.Gear.Add(third);
            bool allowHigher = character.Options.AllowHigherStackedFoci;
            character.Options.AllowHigherStackedFoci = scenario == 4;
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true, MdiParent = owner } : new frmCreate(character) { Loading = true, MdiParent = owner };
            editor.Show(); editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
            var backend = Forms.Backends.Platform.Backend; int picks = 0, notices = 0; bool expired = false;
            bool finished = false;
            // The headless dispatcher drains newly posted work in the current
            // loop. Poll the active modal instead of queuing the next reply
            // from inside a closing picker (which races the next ShowDialog).
            using var replies = new Timer(_ => backend.Post(() =>
            {
                if (finished) return;
                var active = Forms.Application.OpenForms.LastOrDefault(f => f.Modal && f.Visible);
                if (active is Forms.MessageBoxForm notice && notice.DialogResult == Forms.DialogResult.None)
                {
                    notices++; notice.DialogResult = Forms.DialogResult.OK; notice.Close();
                }
                else if (active is frmSelectItem picker && picker.DialogResult == Forms.DialogResult.None)
                {
                    picks++; bool accept = rejectForce || success || (selectOne && picks == 1);
                    if (accept) Field<Forms.ComboBox>(picker, "cboAmmo").SelectedValue = picks == 1 ? first.InternalId : picks == 2 ? second.InternalId : third.InternalId;
                    picker.DialogResult = accept ? Forms.DialogResult.OK : Forms.DialogResult.Cancel;
                }
            }), null, 20, 20);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var cancel = deadline.Token.Register(() => backend.Post(() =>
            {
                expired = true;
                foreach (var form in Forms.Application.OpenForms.Where(f => f is frmSelectItem || f is Forms.MessageBoxForm).ToArray()) form.Close();
            }));
            try
            {
                byte[] before = character.CaptureSnapshot(); int money = character.Nuyen, expenses = character.ExpenseEntries.Count;
                editor.GetType().GetMethod("cmdCreateStackedFocus_Click", Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                if (expired) throw new TimeoutException("Stacked-focus cancellation characterization exceeded its deadline.");
                string context = " career=" + career + " scenario=" + scenario;
                if (success)
                {
                    int components = scenario == 5 ? 3 : 2;
                    var stack = character.StackedFoci.Single(); var item = character.Gear.Single();
                    var tree = Field<Forms.TreeView>(editor, "treGear");
                    check(picks == components && notices == 0 && editor.Enabled && first.Bonded && second.Bonded &&
                        (components == 2 || third.Bonded) && stack.Gear.Count == components &&
                        ReferenceEquals(stack.Gear[0], first) && ReferenceEquals(stack.Gear[1], second) &&
                        (components == 2 || ReferenceEquals(stack.Gear[2], third)) && !stack.Bonded,
                        "Shared stacked-focus success transfers ordered bonded components to an unbonded stack" + context);
                    check(stack.GearId == item.InternalId && item.Category == "Stacked Focus" && item.Cost == (components == 3 ? "600" : "300") &&
                        item.Source == "SM" && item.Page == "84" && item.Avail == "0" && item.Rating == 0 &&
                        new CommonFunctions(character).FindNode(item.InternalId, tree).Text == item.DisplayNameShort &&
                        character.Nuyen == money && character.ExpenseEntries.Count == expenses && Field<bool>(editor, "_blnIsDirty"),
                        "Shared stacked-focus success retains linked gear cost source tree and caller accounting" + context);
                    var restored = new Character(); restored.LoadSnapshot(character.CaptureSnapshot());
                    check(restored.Gear.Count == 1 && restored.StackedFoci.Count == 1 &&
                        restored.StackedFoci[0].GearId == restored.Gear[0].InternalId && restored.StackedFoci[0].Gear.Count == components &&
                        restored.StackedFoci[0].Gear[0].InternalId == first.InternalId && restored.StackedFoci[0].Gear[0].Bonded &&
                        restored.Gear[0].Cost == (components == 3 ? "600" : "300"),
                        "Shared stacked-focus linked gear and component identity survive the existing snapshot format" + context);
                }
                else
                {
                    check(picks == (scenario == 0 ? 1 : 2) && notices == 1 && editor.Enabled,
                        "Shared stacked-focus rejection returns through expected notice" + context);
                    check(!first.Bonded && !second.Bonded && character.Gear.Count == 2 &&
                        ReferenceEquals(character.Gear[0], first) && ReferenceEquals(character.Gear[1], second) && character.StackedFoci.Count == 0 &&
                        character.Nuyen == money && character.ExpenseEntries.Count == expenses && !Field<bool>(editor, "_blnIsDirty") &&
                        before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
                        "Shared stacked-focus rejection retains inventory and accounting and leaves rejected components unbonded" + context);
                }
                afterScenario?.Invoke(editor, character, career, scenario);
            }
            finally { character.Options.AllowHigherStackedFoci = allowHigher; finished = true; editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close(); }
        }
    }
}
