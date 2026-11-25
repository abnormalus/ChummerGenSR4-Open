#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Controls.Documents;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Exercise the actual contact commands and confirmation modal with synthetic data.
internal static class NativeContactRemovalChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static Task ReplyToConfirmation(Forms.DialogResult? answer)
    {
        var reply = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                { if (DateTime.UtcNow >= deadline) throw new TimeoutException("The confirmation dialog did not open."); await Task.Delay(10); }
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
    private static Task DeleteAsync(Forms.Form editor, ContactControl controller, bool enemy)
    {
        var section = NativeContactInteractions.Section(editor, enemy);
        return NativeContactInteractions.ClickAsync(section, section.Rows[controller].Delete);
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
                var rows = NativeContactInteractions.Controllers(editor, enemy).ToArray();
                var section = NativeContactInteractions.Section(editor, enemy);
                var middle = rows.Single(r => ReferenceEquals(r.ContactObject, contacts[1]));
                var liveRow = section.Rows[middle];
                var normalFontSize = liveRow.Name.FontSize;
                section.Viewport.MaxHeight = 120;
                TextElement.SetFontSize(section.Viewport, normalFontSize * 1.5);
                await Task.Delay(60);
                check(liveRow.Name.FontSize == normalFontSize * 1.5
                    && liveRow.Connection.FontSize == liveRow.Name.FontSize
                    && liveRow.Name.Bounds.Height >= liveRow.Name.FontSize + liveRow.Name.Padding.Top + liveRow.Name.Padding.Bottom
                    && liveRow.Layout.Bounds.Width <= section.Viewport.Bounds.Width
                    && section.Viewport.Extent.Height > section.Viewport.Viewport.Height,
                    "Native contact fields accommodate 150% inherited text and remain scrollable" + label +
                    $" [font={normalFontSize}/{liveRow.Name.FontSize}/{liveRow.Connection.FontSize}, " +
                    $"row={liveRow.Layout.Bounds}, input={liveRow.Name.Bounds}, viewport={section.Viewport.Bounds}/{section.Viewport.Viewport}, extent={section.Viewport.Extent}]");
                liveRow.Layout.MaxWidth = 220;
                await Task.Delay(60);
                var connectionCell = (Avalonia.Controls.Control)liveRow.Connection.Parent!;
                var loyaltyCell = (Avalonia.Controls.Control)liveRow.Loyalty.Parent!;
                check(Avalonia.Controls.Grid.GetRow(loyaltyCell) == 1 &&
                    connectionCell.Bounds.Bottom <= loyaltyCell.Bounds.Top,
                    "Contact and enemy rating fields stack without overlap in a narrow viewport" + label);
                liveRow.Layout.ClearValue(Avalonia.Controls.Control.MaxWidthProperty);
                section.Viewport.ClearValue(Avalonia.Controls.Control.MaxHeightProperty);
                section.Viewport.ClearValue(TextElement.FontSizeProperty);
                await Task.Delay(30);
                var dirty = editor.GetType().GetField("_blnIsDirty", Flags)!; dirty.SetValue(editor, false);
                editor.GetType().GetProperty("Loading")!.SetValue(editor, false);
                liveRow.Connection.Value = 2;
                liveRow.Loyalty.Value = 2;
                check(middle.ContactObject.Connection == 2 && middle.ContactObject.Loyalty == 2,
                    "Native connection and loyalty inputs update the selected record" + label);
                liveRow.Connection.Value = 1;
                liveRow.Loyalty.Value = 1;
                dirty.SetValue(editor, false);
                var nameInput = section.Rows[middle].Name;
                const string editedName = "Synthetic literal [O'Brien]";
                nameInput.Text = editedName;
                check(middle.ContactObject.Name == editedName && (bool)dirty.GetValue(editor)!,
                    "Native contact name edit updates model and marks the actual owner dirty" + label);
                dirty.SetValue(editor, false);
                nameInput.Text = editedName;
                check(!(bool)dirty.GetValue(editor)!, "Native unchanged contact name retains clean owner state" + label);
                character.Options.ConfirmDelete = true;
                character.Options.ConfirmKarmaExpense = false;
                foreach (bool closeWindow in new[] { false, true })
                {
                    byte[] before = character.CaptureSnapshot();
                    var reply = ReplyToConfirmation(closeWindow ? null : Forms.DialogResult.No);
                    await DeleteAsync(editor, middle, enemy); await reply;
                    check(before.SequenceEqual(character.CaptureSnapshot()) && NativeContactInteractions.Controllers(editor, enemy).Contains(middle)
                        && section.Rows.ContainsKey(middle) && !middle.IsDisposed
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
                    check(character.Contacts.SequenceEqual(expected) && NativeContactInteractions.Controllers(editor, enemy).Select(r => r.ContactObject).SequenceEqual(expected) && row.IsDisposed && (bool)dirty.GetValue(editor)!
                        && character.Karma == karma - cost && character.ExpenseEntries.Count == expenses + (career && enemy ? 1 : 0),
                        "Native contact deletion removes only the selected row with unchanged career charge" + label + " index=" + index);
                    // Verify each intermediate save, not only the final empty list.
                    var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
                    if (!reopened.Contacts.Select(c => c.Name + c.Notes).SequenceEqual(expected.Select(c => c.Name + c.Notes)))
                        throw new InvalidOperationException("Contact survivors changed during save-format reload.");
                }
                check(character.Contacts.Count == 0 && NativeContactInteractions.Controllers(editor, enemy).Count == 0 && editor.Enabled
                    && section.Rows.Count == 0,
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
