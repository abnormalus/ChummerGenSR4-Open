#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer;

internal static class NativeWeaponFocusChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    private static async Task ChooseAsync(Action command, DialogResult answer)
    {
        var answered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (!Application.OpenForms.OfType<MessageBoxForm>().Any())
                {
                    if (DateTime.UtcNow >= deadline) throw new TimeoutException("Focus confirmation did not open.");
                    await Task.Delay(20);
                }
                var dialog = Application.OpenForms.OfType<MessageBoxForm>().Last();
                dialog.DialogResult = answer; dialog.Close(); answered.TrySetResult();
            }
            catch (Exception error) { answered.TrySetException(error); }
        });
        command(); await answered.Task;
    }
    internal static async Task RunAsync(frmMain main, Character source, Action<bool, string> check)
    {
        WeaponFocusBindingChecks.Run(source, check, main);
        var (character, _, modification) = WeaponFocusBindingChecks.Sample(source);
        character.Created = true; character.Karma = 100;
        bool originalConfirm = character.Options.ConfirmKarmaExpense;
        character.Options.ConfirmKarmaExpense = true;
        using var editor = new frmCareer(character) { Loading = true, MdiParent = main };
        try
        {
            editor.Show();
            TreeNode Node() => Field<TreeView>(editor, "treFoci").Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == modification.InternalId);
            int priorExpenses = character.ExpenseEntries.Count;
            await ChooseAsync(() => Node().Checked = true, DialogResult.Cancel);
            check(!Node().Checked && character.Karma == 100 && character.ExpenseEntries.Count == priorExpenses,
                "Native closing weapon focus confirmation cancels without spending Karma");
            await ChooseAsync(() => Node().Checked = true, DialogResult.No);
            check(!Node().Checked && character.Karma == 100 && character.ExpenseEntries.Count == priorExpenses && character.Foci.Count == 0,
                "Native declining weapon focus binding preserves Karma and inventory");
            await ChooseAsync(() => Node().Checked = true, DialogResult.Yes);
            int cost = 2 * character.Options.KarmaWeaponFocus;
            var expense = character.ExpenseEntries.Last();
            check(Node().Checked && character.Karma == 100 - cost && expense.Undo.ObjectId == modification.InternalId,
                "Native accepting weapon focus binding charges one canonical Karma expense");
            await ChooseAsync(() => Node().Checked = false, DialogResult.Cancel);
            check(Node().Checked && character.Foci.Any(focus => focus.GearId == modification.InternalId),
                "Native closing unbind confirmation preserves the weapon focus binding");
            await ChooseAsync(() => Node().Checked = false, DialogResult.No);
            check(Node().Checked && character.Foci.Any(focus => focus.GearId == modification.InternalId),
                "Native declining unbind keeps the weapon focus bound");
            var row = Field<ListView>(editor, "lstKarma").Items.Cast<ListViewItem>().Single(item => item.SubItems[3].Text == expense.InternalId);
            row.Selected = true;
            await ChooseAsync(() => Field<ToolStripMenuItem>(editor, "tsUndoKarmaExpense").PerformClick(), DialogResult.Cancel);
            check(Node().Checked && character.Karma == 100 - cost && character.ExpenseEntries.Contains(expense),
                "Native closing expense undo confirmation preserves focus and Karma");
            await ChooseAsync(() => Field<ToolStripMenuItem>(editor, "tsUndoKarmaExpense").PerformClick(), DialogResult.Yes);
            check(!Node().Checked && character.Karma == 100 && !character.Foci.Any(focus => focus.GearId == modification.InternalId) &&
                !character.ExpenseEntries.Contains(expense),
                "Native expense undo refunds weapon focus Karma and clears its existing binding");
            character.Karma = 0;
            await ChooseAsync(() => Node().Checked = true, DialogResult.OK);
            check(!Node().Checked && character.Karma == 0 && character.ExpenseEntries.Count == priorExpenses,
                "Native unaffordable weapon focus binding fails without a partial purchase");
            character.MAG.Value = 1; modification.Rating = 6; character.Karma = 100;
            editor.UpdateCharacterInfo();
            await ChooseAsync(() => Node().Checked = true, DialogResult.OK);
            check(!Node().Checked && character.Karma == 100 && character.ExpenseEntries.Count == priorExpenses,
                "Native excessive weapon focus force is rejected before Karma is spent");
            modification.Rating = 1; editor.UpdateCharacterInfo();
            await ChooseAsync(() => Node().Checked = true, DialogResult.Yes);
            var catalog = XmlManager.Instance.Load("weapons.xml");
            var otherWeapon = new Weapon(character);
            otherWeapon.Create(catalog.SelectNodes("/chummer/weapons/weapon")!.Cast<System.Xml.XmlNode>().First(node => node["name"]?.InnerText == "Katana"),
                character, new TreeNode(), null, null, null);
            var otherModification = new WeaponMod(character);
            otherModification.Create(catalog.SelectNodes("/chummer/mods/mod")!.Cast<System.Xml.XmlNode>().Single(node => node["name"]?.InnerText == "Weapon Focus"), new TreeNode());
            otherModification.Rating = 1; otherModification.Parent = otherWeapon;
            otherWeapon.WeaponMods.Add(otherModification); character.Weapons.Add(otherWeapon); editor.UpdateCharacterInfo();
            var otherNode = Field<TreeView>(editor, "treFoci").Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == otherModification.InternalId);
            int beforeLimitKarma = character.Karma;
            await ChooseAsync(() => otherNode.Checked = true, DialogResult.OK);
            check(!otherNode.Checked && character.Karma == beforeLimitKarma && character.Foci.Count == 1,
                "Native weapon focus count limit includes an already bound modification");
            check(main.ToAvaloniaWindow().IsEnabled && NativeDialogHost.Count == 0 && !Application.OpenForms.OfType<MessageBoxForm>().Any(),
                "Native focus confirmation paths release modal ownership completely");
        }
        finally
        {
            character.Options.ConfirmKarmaExpense = originalConfirm;
            typeof(frmCareer).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
            editor.Close();
        }
    }
}
