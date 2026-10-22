using System;
using System.Linq;
using System.Reflection;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class ContactRemovalChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    internal static void Run(Character source, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        foreach (bool enemy in new[] { false, true })
        foreach (int index in new[] { 0, 1, 2 })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
            character.Contacts.Clear(); character.Created = career; character.Karma = 100;
            var records = Enumerable.Range(0, 3).Select(i => new Contact(character)
            { Name = "Synthetic contact removal " + i, EntityType = enemy ? ContactType.Enemy : ContactType.Contact,
                Connection = 1, Loyalty = 1, Notes = "Synthetic retained notes " + i }).ToArray();
            foreach (var record in records) character.Contacts.Add(record);
            bool confirm = character.Options.ConfirmDelete, karmaConfirm = character.Options.ConfirmKarmaExpense;
            Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            bool closed = false;
            try
            {
                editor.Show();
                var panel = Field<Forms.Panel>(editor, enemy ? "panEnemies" : "panContacts");
                var rows = panel.Controls.OfType<ContactControl>().ToArray();
                var deleted = rows.Single(r => ReferenceEquals(r.ContactObject, records[index]));
                character.Options.ConfirmDelete = false; character.Options.ConfirmKarmaExpense = false;
                Field<Forms.Button>(deleted, "cmdDelete").PerformClick();
                var expected = records.Where((_, i) => i != index).ToArray();
                string label = " career=" + career + " enemy=" + enemy + " index=" + index;
                check(character.Contacts.SequenceEqual(expected) && panel.Controls.OfType<ContactControl>()
                    .Select(r => r.ContactObject).SequenceEqual(expected),
                    "Contact deletion removes only the selected model and row" + label);
                check(deleted.IsDisposed && new[] { "ConnectionRatingChanged", "ConnectionGroupRatingChanged", "LoyaltyRatingChanged", "DeleteContact", "FileNameChanged", "ContactNameChanged" }
                    .All(name => !(typeof(ContactControl).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(deleted) is Delegate handlers) || !handlers.GetInvocationList().Any(h => ReferenceEquals(h.Target, editor))),
                    "Contact deletion disposes the removed row and releases owner callbacks" + label);
                var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
                check(reopened.Contacts.Select(c => c.Name + c.Notes).SequenceEqual(expected.Select(c => c.Name + c.Notes)),
                    "Contact deletion preserves other contacts through save-format reload" + label);
                editor.GetType().GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false);
                editor.Close(); closed = true;
            }
            finally
            {
                character.Options.ConfirmDelete = confirm; character.Options.ConfirmKarmaExpense = karmaConfirm;
                if (!closed) { editor.GetType().GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false); editor.Close(); }
                editor.Dispose();
            }
        }
    }
}
