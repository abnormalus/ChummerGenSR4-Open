using System;
using System.Linq;
using System.Reflection;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class ContactEditChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);
    internal static void Run(Character source, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        foreach (bool enemy in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career;
            character.Contacts.Clear();
            var contact = new Contact(character) { Name = "Synthetic initial", EntityType = enemy ? ContactType.Enemy : ContactType.Contact, Connection = 1, Loyalty = 1 };
            character.Contacts.Add(contact);
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            editor.Show();
            try
            {
                editor.GetType().GetProperty("Loading").SetValue(editor, false);
                var row = Field<Forms.Panel>(editor, enemy ? "panEnemies" : "panContacts").Controls.OfType<ContactControl>().Single();
                var input = Field<Forms.TextBox>(row, "txtContactName");
                var dirty = editor.GetType().GetField("_blnIsDirty", Flags); dirty.SetValue(editor, false);
                const string edited = "Synthetic literal [O'Brien]";
                input.Text = edited;
                string label = " career=" + career + " enemy=" + enemy;
                check(contact.Name == edited && (bool)dirty.GetValue(editor), "Contact name editing changes model and marks owner dirty" + label);
                var restored = new Character(); restored.LoadSnapshot(character.CaptureSnapshot());
                check(restored.Contacts.Single().Name == edited, "Contact name editing preserves literal name through save format" + label);
                dirty.SetValue(editor, false); input.Text = edited;
                check(!(bool)dirty.GetValue(editor), "Unchanged contact name preserves clean state" + label);
                Field<Forms.Button>(editor, enemy ? "cmdAddEnemy" : "cmdAddContact").PerformClick();
                var added = Field<Forms.Panel>(editor, enemy ? "panEnemies" : "panContacts").Controls.OfType<ContactControl>().Last();
                dirty.SetValue(editor, false);
                Field<Forms.TextBox>(added, "txtContactName").Text = edited + " added";
                check(!ReferenceEquals(added, row) && character.Contacts.Contains(added.ContactObject)
                    && added.ContactObject.Name == edited + " added" && (bool)dirty.GetValue(editor),
                    "Dynamically added contact name edit marks its actual owner dirty" + label);
                restored.LoadSnapshot(character.CaptureSnapshot());
                check(restored.Contacts.Select(c => c.Name).SequenceEqual(new[] { edited, edited + " added" }),
                    "Loaded and dynamically added contact names retain save order" + label);
            }
            finally
            {
                var owned = Field<Forms.Panel>(editor, enemy ? "panEnemies" : "panContacts").Controls.OfType<ContactControl>().ToArray();
                editor.GetType().GetField("_blnIsDirty", Flags).SetValue(editor, false); editor.Close();
                check(owned.All(row => !(typeof(ContactControl).GetField("ContactNameChanged", Flags).GetValue(row) is Delegate handlers)
                    || !handlers.GetInvocationList().Any(handler => ReferenceEquals(handler.Target, editor))),
                    "Contact editor close releases loaded and dynamic name callbacks career=" + career + " enemy=" + enemy);
            }
        }
    }
}
