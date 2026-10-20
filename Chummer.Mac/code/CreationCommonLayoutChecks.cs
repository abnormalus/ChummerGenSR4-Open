#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CreationCommonLayoutChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    internal static void Run(Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = false;
        character.Contacts.Clear();
        character.Contacts.Add(new Contact(character) { Name = "Synthetic layout contact", EntityType = ContactType.Contact, Connection = 2, Loyalty = 1 });
        character.Contacts.Add(new Contact(character) { Name = "Synthetic layout enemy", EntityType = ContactType.Enemy, Connection = 2, Loyalty = 1 });
        using var editor = new frmCreate(character) { Loading = true };
        editor.Show();
        try
        {
            byte[] before = character.CaptureSnapshot(); bool dirty = Field<bool>(editor, "_blnIsDirty");
            typeof(frmCreate).GetMethod("InitializeCreationCommonLayout", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);
            var common = Field<Forms.TabPage>(editor, "tabCommon"); common.Size = new System.Drawing.Size(754, 440);
            foreach (string name in new[] { "panContacts", "panEnemies" })
            {
                var panel = Field<Forms.Panel>(editor, name);
                var rows = panel.Controls.OfType<ContactControl>().ToArray();
                check(rows.Length == 1 && rows.All(row => row.Right <= panel.ClientSize.Width && row.Bottom <= panel.ClientSize.Height &&
                    row.Controls.Cast<Forms.Control>().Where(child => child.Visible).All(child => child.Left >= 0 && child.Right <= row.ClientSize.Width && child.Bottom <= row.ClientSize.Height)),
                    "Creation right-column populated contact and enemy fields fit without horizontal scrolling: " + name);
            }
            editor.Size = new System.Drawing.Size(1800, 1000);
            check(new[] { "panContacts", "panEnemies" }.All(name =>
                Field<Forms.Panel>(editor, name).Controls.OfType<ContactControl>().All(row =>
                    row.Height == 26 && row.Right <= row.Parent!.ClientSize.Width)),
                "Creation wide common page uses available width for single-line contact and enemy rows " + string.Join(";", new[] { "panContacts", "panEnemies" }.Select(name =>
                    name + ":" + Field<Forms.Panel>(editor, name).ClientSize + "/" + string.Join(",", Field<Forms.Panel>(editor, name).Controls.OfType<ContactControl>().Select(row => row.Bounds.ToString())))));
            check(character.CaptureSnapshot().SequenceEqual(before) && Field<bool>(editor, "_blnIsDirty") == dirty,
                "Creation common responsive layout preserves contact model and dirty state");
        }
        finally { typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false); editor.Close(); }
    }
}
