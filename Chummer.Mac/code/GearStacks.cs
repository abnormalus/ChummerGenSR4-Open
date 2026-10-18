using System;
using Majorsilence.Forms;

namespace Chummer
{
    internal static class GearStacks
    {
        internal const int Maximum = 100000;
        internal static bool Apply(Character character, Gear gear, int total, bool career)
        {
            if (gear == null || total < 1 || total > Maximum) throw new ArgumentOutOfRangeException(nameof(total));
            int previous = gear.Quantity;
            if (total == previous) return false;
            int additional = total - previous;
            int cost = 0;
            if (career && additional > 0)
            {
                cost = checked((gear.TotalCost / Math.Max(1, previous)) * additional);
                string avail = gear.TotalAvail();
                if (character.Options.MultiplyRestrictedCost && avail.EndsWith(LanguageManager.Instance.GetString("String_AvailRestricted")))
                    cost = checked(cost * character.Options.RestrictedCostMultiplier);
                if (character.Options.MultiplyForbiddenCost && avail.EndsWith(LanguageManager.Instance.GetString("String_AvailForbidden")))
                    cost = checked(cost * character.Options.ForbiddenCostMultiplier);
                if (cost > character.Nuyen) throw new InvalidOperationException("GearStack_NotEnoughNuyen");
            }
            // Children describe one configured unit; the existing gear model multiplies their
            // costs by the root quantity. Changing child quantities would count add-ins twice.
            gear.Quantity = total;
            try
            {
                if (additional > 0 && character.Options.EnforceCapacity && gear.Parent != null && gear.Parent.CapacityRemaining < 0)
                    throw new InvalidOperationException("GearStack_Capacity");
                _ = gear.TotalCost;
            }
            catch { gear.Quantity = previous; throw; }
            if (cost > 0)
            {
                var expense = new ExpenseLogEntry();
                expense.Create(-cost, LanguageManager.Instance.GetString("String_ExpensePurchaseGear") + " " + gear.DisplayNameShort, ExpenseType.Nuyen, DateTime.Now);
                var undo = new ExpenseUndo();
                // Expense undo subtracts purchased units; existing stock must remain owned.
                undo.CreateNuyen(NuyenExpenseType.AddGear, gear.InternalId, additional);
                expense.Undo = undo;
                character.ExpenseEntries.Add(expense);
                character.Nuyen -= cost;
            }
            return true;
        }

        internal static void Attach(Form owner, Character character, TreeView tree, Func<string, Gear> find,
            Action changed, bool career, params ContextMenuStrip[] menus)
        {
            Gear Selected() => tree.SelectedNode?.Tag == null ? null : find(tree.SelectedNode.Tag.ToString());
            foreach (var menu in menus)
            {
                var command = new ToolStripMenuItem(LanguageManager.Instance.GetString("GearStack_Command")) { Name = "createGearStack" };
                menu.Items.Add(command);
                menu.Opening += (_, _) => command.Visible = Selected() != null;
                command.Click += (_, _) => {
                    var gear = Selected();
                    if (gear == null) return;
                    using var prompt = new frmSelectNumber { Text = LanguageManager.Instance.GetString("GearStack_Title"), Minimum = 1,
                        Maximum = Maximum, SelectedValue = Math.Min(Maximum, Math.Max(1, gear.Quantity)),
                        Description = LanguageManager.Instance.GetString(career ? "GearStack_CareerPrompt" : "GearStack_Prompt") };
                    if (prompt.ShowDialog(owner) != DialogResult.OK) return;
                    try
                    {
                        if (!Apply(character, gear, prompt.SelectedValue, career)) return;
                        tree.SelectedNode.Text = gear.DisplayName;
                        changed();
                        AppDiagnostics.Record("gear.create-stack");
                    }
                    catch (Exception error)
                    {
                        AppDiagnostics.Record("gear.create-stack-failed", error);
                        string key = error.Message == "GearStack_NotEnoughNuyen" || error.Message == "GearStack_Capacity" ? error.Message : "GearStack_Failed";
                        AppErrors.Inform(owner, key);
                    }
                };
            }
            tree.NodeMouseClick += (_, e) => { if (e.Button == MouseButtons.Right) tree.SelectedNode = e.Node; };
        }
    }
}
