#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeWeaponDeletionChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static async Task RunAsync(frmMain owner, Character fixture, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character();
            character.LoadSnapshot(fixture.CaptureSnapshot());
            character.Created = career;
            using Forms.Form editor = career
                ? new frmCareer(character) { Loading = true, MdiParent = owner }
                : new frmCreate(character) { Loading = true, MdiParent = owner };
            editor.Show();
            Field<Forms.TabControl>("tabCharacterTabs").SelectedTab = Field<Forms.TabPage>("tabStreetGear");
            Field<Forms.TabControl>("tabStreetGearTabs").SelectedTab = Field<Forms.TabPage>("tabWeapons");
            var tree = Field<Forms.TreeView>("treWeapons");
            var dirty = editor.GetType().GetField("_blnIsDirty", Private)!;
            T Field<T>(string name) => (T)editor.GetType().GetField(name, Private)!.GetValue(editor)!;
            void Delete() => editor.GetType().GetMethod("cmdDeleteWeapon_Click", Private)!.Invoke(editor, new object[] { editor, EventArgs.Empty });

            (Weapon Weapon, Forms.TreeNode Node) AddWeapon(string category)
            {
                var weapon = new Weapon(character);
                var node = new Forms.TreeNode();
                weapon.Create(XmlManager.Instance.Load("weapons.xml").SelectSingleNode("/chummer/weapons/weapon[name='AK-97']")!,
                    character, node, null, null, null, false);
                weapon.Category = category;
                character.Weapons.Add(weapon);
                tree.Nodes[0].Nodes.Add(node);
                return (weapon, node);
            }

            async Task AnswerDelete(Forms.DialogResult answer)
            {
                var answered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(async () =>
                {
                    var deadline = DateTime.UtcNow.AddSeconds(10);
                    while (DateTime.UtcNow < deadline)
                    {
                        var dialog = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().LastOrDefault();
                        if (dialog != null)
                        {
                            dialog.DialogResult = answer;
                            dialog.Close();
                            answered.SetResult();
                            return;
                        }
                        await Task.Delay(20);
                    }
                    answered.SetException(new TimeoutException("Weapon deletion notice did not open."));
                });
                Delete();
                await answered.Task;
            }

            try
            {
                var (weapon, node) = AddWeapon("Pistols");
                var (generated, _) = AddWeapon("Gear");
                var accessory = new WeaponAccessory(character) { Parent = weapon };
                accessory.Create(XmlManager.Instance.Load("weapons.xml").SelectSingleNode("/chummer/accessories/accessory[name='Spare Clips']")!,
                    new Forms.TreeNode(), "");
                Gear MakeGear()
                {
                    var result = new Gear(character);
                    result.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Medkit']")!,
                        character, new Forms.TreeNode(), 1, new List<Weapon>(), new List<Forms.TreeNode>());
                    return result;
                }
                var gear = MakeGear();
                var child = MakeGear();
                child.WeaponID = generated.InternalId;
                gear.Children.Add(child);
                accessory.Gear.Add(gear);
                weapon.WeaponAccessories.Add(accessory);
                tree.SelectedNode = node;
                dirty.SetValue(editor, false);
                byte[] before = character.CaptureSnapshot();
                character.Options.ConfirmDelete = true;
                await AnswerDelete(Forms.DialogResult.No);
                check(before.SequenceEqual(character.CaptureSnapshot()) && !(bool)dirty.GetValue(editor)!,
                    "Cancel weapon deletion preserves the snapshot and dirty state career=" + career);

                character.Options.ConfirmDelete = false;
                foreach (string category in new[] { "Gear", "Cyberware Weapons", "Quality Weapons" })
                {
                    var (protectedWeapon, protectedNode) = AddWeapon(category);
                    tree.SelectedNode = protectedNode;
                    before = character.CaptureSnapshot();
                    await AnswerDelete(Forms.DialogResult.OK);
                    check(character.Weapons.Contains(protectedWeapon) && before.SequenceEqual(character.CaptureSnapshot()) &&
                        !(bool)dirty.GetValue(editor)!, "Protected weapon cannot be deleted " + category + " career=" + career);
                }

                int nuyen = character.Nuyen, expenses = character.ExpenseEntries.Count;
                tree.SelectedNode = node;
                Delete();
                check(!character.Weapons.Contains(weapon) && !character.Weapons.Contains(generated) && node.TreeView == null &&
                    (bool)dirty.GetValue(editor)! && character.Nuyen == nuyen && character.ExpenseEntries.Count == expenses,
                    "Delete removes a weapon and its nested gear weapon without a sale or expense career=" + career);
            }
            finally
            {
                dirty.SetValue(editor, false);
                editor.Close();
            }
        }
    }
}
