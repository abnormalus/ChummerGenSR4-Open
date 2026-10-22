#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Exercise the actual contact commands and confirmation modal with synthetic data.
internal static class NativeContactRemovalChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static Task ReplyToConfirmation(Forms.DialogResult? answer)
    {
        var reply = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Contact deletion confirmation did not open."); await Task.Delay(10); }
                var message = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last();
                if (answer.HasValue) message.DialogResult = answer.Value;
                message.Close(); reply.TrySetResult();
            }
            catch (Exception error)
            {
                Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().LastOrDefault()?.Close();
                reply.TrySetException(error);
            }
        });
        return reply.Task;
    }
    private static ContactControl[] Controllers(Forms.Form editor, bool enemy) => editor is frmCreate creation
        ? (enemy ? creation.CreationEnemyControllers : creation.CreationContactControllers).ToArray()
        : Field<Forms.Panel>(editor, enemy ? "panEnemies" : "panContacts").Controls.OfType<ContactControl>().ToArray();
    private static async Task DeleteAsync(Forms.Form editor, ContactControl controller, bool enemy)
    {
        if (editor is not frmCreate creation) { Field<Forms.Button>(controller, "cmdDelete").PerformClick(); return; }
        var section = creation.ContactPresentation(enemy)!; var row = section.Rows[controller];
        await NativeContactInteractions.ClickAsync(section, row.Delete);
    }
    internal static async Task RunAsync(Forms.Form owner, Character source, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        foreach (bool enemy in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
            character.Contacts.Clear(); character.Created = career; character.Karma = 100;
            var contacts = Enumerable.Range(0, 3).Select(i => new Contact(character)
            { Name = "Synthetic removal " + i, Notes = "Synthetic retained notes " + i,
                EntityType = enemy ? ContactType.Enemy : ContactType.Contact, Connection = 1, Loyalty = 1 }).ToArray();
            foreach (var contact in contacts) character.Contacts.Add(contact);
            bool confirm = character.Options.ConfirmDelete, karmaConfirm = character.Options.ConfirmKarmaExpense;
            using Forms.Form editor = career ? new frmCareer(character) { MdiParent = owner, Loading = true }
                : new frmCreate(character) { MdiParent = owner, Loading = true };
            editor.Show(); await Task.Delay(30);
            string label = " career=" + career + " enemy=" + enemy;
            try
            {
                var rows = Controllers(editor, enemy);
                var middle = rows.Single(r => ReferenceEquals(r.ContactObject, contacts[1]));
                var dirty = editor.GetType().GetField("_blnIsDirty", Flags)!; dirty.SetValue(editor, false);
                editor.GetType().GetProperty("Loading")!.SetValue(editor, false);
                var nameInput = Field<Forms.TextBox>(middle, "txtContactName");
                const string editedName = "Synthetic literal [O'Brien]";
                if (editor is frmCreate creationNameOwner) creationNameOwner.ContactPresentation(enemy)!.Rows[middle].Name.Text = editedName;
                else nameInput.Text = editedName;
                check(middle.ContactObject.Name == editedName && (bool)dirty.GetValue(editor)!,
                    "Native contact name edit updates model and marks the actual owner dirty" + label);
                dirty.SetValue(editor, false);
                if (editor is frmCreate creationNoopOwner) creationNoopOwner.ContactPresentation(enemy)!.Rows[middle].Name.Text = editedName;
                else nameInput.Text = editedName;
                check(!(bool)dirty.GetValue(editor)!, "Native unchanged contact name retains clean owner state" + label);
                character.Options.ConfirmDelete = true;
                character.Options.ConfirmKarmaExpense = false;
                foreach (bool closeWindow in new[] { false, true })
                {
                    byte[] before = character.CaptureSnapshot();
                    var reply = ReplyToConfirmation(closeWindow ? null : Forms.DialogResult.No);
                    await DeleteAsync(editor, middle, enemy); await reply;
                    check(before.SequenceEqual(character.CaptureSnapshot()) && Controllers(editor, enemy).Contains(middle)
                        && (editor is not frmCreate canceledCreation || canceledCreation.ContactPresentation(enemy)!.Rows.ContainsKey(middle)) && !middle.IsDisposed
                        && !(bool)dirty.GetValue(editor)! && editor.Enabled,
                        "Native canceled contact deletion preserves model rows clean state and editor availability" + label + " closeWindow=" + closeWindow);
                }
                character.Options.ConfirmDelete = false; character.Options.ConfirmKarmaExpense = false;
                foreach (int index in new[] { 1, 0, 2 })
                {
                    var row = rows.Single(r => ReferenceEquals(r.ContactObject, contacts[index]));
                    int karma = character.Karma, expenses = character.ExpenseEntries.Count;
                    var expected = character.Contacts.Where(c => !ReferenceEquals(c, contacts[index])).ToArray();
                    character.Options.ConfirmDelete = index == 1;
                    var consent = index == 1 ? ReplyToConfirmation(Forms.DialogResult.Yes) : Task.CompletedTask;
                    await DeleteAsync(editor, row, enemy); await consent; await Task.Delay(10);
                    int cost = career && enemy ? 2 * character.Options.KarmaQuality : 0;
                    check(character.Contacts.SequenceEqual(expected) && Controllers(editor, enemy).Select(r => r.ContactObject).SequenceEqual(expected) && row.IsDisposed && (bool)dirty.GetValue(editor)!
                        && character.Karma == karma - cost && character.ExpenseEntries.Count == expenses + (career && enemy ? 1 : 0),
                        "Native contact deletion removes only the selected row with unchanged career charge" + label + " index=" + index);
                    // Verify each intermediate save, not only the final empty list.
                    var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
                    if (!reopened.Contacts.Select(c => c.Name + c.Notes).SequenceEqual(expected.Select(c => c.Name + c.Notes)))
                        throw new InvalidOperationException("Contact survivors changed during save-format reload.");
                }
                check(character.Contacts.Count == 0 && Controllers(editor, enemy).Length == 0 && editor.Enabled
                    && (editor is not frmCreate closedCreation || closedCreation.ContactPresentation(enemy)!.Rows.Count == 0),
                    "Native contact deletion retains save-format survivors through middle first and final removals" + label);
            }
            finally
            {
                character.Options.ConfirmDelete = confirm; character.Options.ConfirmKarmaExpense = karmaConfirm;
                editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close();
            }
            check(editor.IsDisposed && owner.Enabled, "Native contact removal owner closes without retained input or modal" + label);
        }
    }
}
