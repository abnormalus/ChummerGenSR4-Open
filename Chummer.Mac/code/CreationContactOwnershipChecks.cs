using System;
using System.Linq;
using System.Reflection;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CreationContactOwnershipChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);
    internal static void Run(Character source, Action<bool, string> check)
    {
        foreach (var method in new[] { CharacterBuildMethod.BP, CharacterBuildMethod.Karma })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
            character.Created = false; character.IgnoreRules = true; character.BuildMethod = method;
            character.Contacts.Clear();
            foreach (bool enemy in new[] { false, true })
            foreach (bool free in new[] { false, true })
                character.Contacts.Add(new Contact(character) { Name = "Synthetic owned " + enemy + free,
                    EntityType = enemy ? ContactType.Enemy : ContactType.Contact, Connection = 3, Loyalty = 2,
                    Membership = 1, AreaOfInfluence = 1, Free = free });
            using var editor = new frmCreate(character) { Loading = true };
            editor.Show(); editor.Loading = false;
            var contacts = Field<Forms.Panel>(editor, "panContacts");
            var enemies = Field<Forms.Panel>(editor, "panEnemies");
            var contactRows = contacts.Controls.OfType<ContactControl>().ToArray();
            var enemyRows = enemies.Controls.OfType<ContactControl>().ToArray();
            var allRows = contactRows.Concat(enemyRows).ToArray();
            bool accepted = false;
            try
            {
                editor.UpdateCharacterInfo();
                var contactSummary = Field<Forms.Label>(editor, "lblContactsBP").Text;
                var enemySummary = Field<Forms.Label>(editor, "lblEnemiesBP").Text;
                byte[] before = character.CaptureSnapshot();
                foreach (var row in contactRows) contacts.Controls.Remove(row);
                foreach (var row in enemyRows) enemies.Controls.Remove(row);
                editor.UpdateCharacterInfo();
                check(contactSummary == Field<Forms.Label>(editor, "lblContactsBP").Text
                    && enemySummary == Field<Forms.Label>(editor, "lblEnemiesBP").Text
                    && character.CaptureSnapshot().SequenceEqual(before),
                    "Creation contact accounting survives presentation detachment with paid free and group rows in " + method);
                accepted = true;
            }
            finally
            {
                // The intentionally failing legacy baseline must still release its rows.
                if (!accepted)
                {
                    foreach (var row in contactRows) contacts.Controls.Add(row);
                    foreach (var row in enemyRows) enemies.Controls.Add(row);
                }
                typeof(frmCreate).GetField("_blnIsDirty", Flags).SetValue(editor, false); editor.Close();
            }
            check(allRows.All(row => row.IsDisposed), "Creation owner disposes detached contact and enemy controllers in " + method);
            check(allRows.All(row => !(typeof(ContactControl).GetField("ContactNameChanged", Flags).GetValue(row) is Delegate handlers)
                || !handlers.GetInvocationList().Any(handler => ReferenceEquals(handler.Target, editor))),
                "Creation owner releases detached contact name callbacks in " + method);
        }
    }
}
