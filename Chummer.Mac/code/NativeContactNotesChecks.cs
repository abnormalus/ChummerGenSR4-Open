#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Synthetic notes only. Exercise the real ContactControl handler and legacy modal;
// no private character, contact identity or text is written into reports/images.
internal static class NativeContactNotesChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static async Task RunAsync(Forms.Form owner, Character source, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career;
            character.Contacts.Clear();
            var first = new Contact(character) { Name = "Synthetic contact one", Connection = 2, Loyalty = 1 };
            var second = new Contact(character) { Name = "Synthetic contact two", Connection = 2, Loyalty = 1 };
            character.Contacts.Add(first); character.Contacts.Add(second);
            using Forms.Form editor = career ? new frmCareer(character) { MdiParent = owner, Loading = true }
                : new frmCreate(character) { MdiParent = owner, Loading = true };
            editor.Show(); await Task.Delay(20);
            try
            {
                var panel = Field<Forms.Panel>(editor, "panContacts");
                var inputs = panel.Controls.OfType<ContactControl>().ToArray();
                var texts = new[] { "", "Synthetic writer’s notes; ordinary prose. ".PadRight(349, 'a'), "Synthetic ordinary notes - punctuation. ".PadRight(335, 'b') };
                for (int sample = 0; sample < texts.Length; sample++)
                {
                    var contact = sample == 2 ? second : first;
                    var control = inputs.Single(item => ReferenceEquals(item.ContactObject, contact));
                    contact.Notes = texts[sample];
                    for (int repeat = 0; repeat < 2; repeat++)
                    {
                        string expected = contact.Notes; bool loaded = false; frmNotes? closedDialog = null;
                        var reply = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                        {
                            try
                            {
                                var deadline = DateTime.UtcNow.AddSeconds(20);
                                while (!Forms.Application.OpenForms.OfType<frmNotes>().Any())
                                { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Contact notes did not open."); await Task.Delay(10); }
                                closedDialog = Forms.Application.OpenForms.OfType<frmNotes>().Last();
                                var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(closedDialog);
                                while (!window.IsVisible || window.Bounds.Width <= 0 || window.Bounds.Height <= 0)
                                { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Contact notes did not become visible."); await Task.Delay(10); }
                                // Opening/closing within one dispatcher turn can avoid the
                                // paint/focus path implicated by an opening-time crash.
                                await Task.Delay(100);
                                loaded = closedDialog.Notes == expected && window.IsVisible;
                                closedDialog.Notes = expected + "\nSynthetic edit";
                                closedDialog.Close(); reply.TrySetResult();
                            }
                            catch (Exception error)
                            { Forms.Application.OpenForms.OfType<frmNotes>().LastOrDefault()?.Close(); reply.TrySetException(error); }
                        });
                        control.GetType().GetMethod("imgNotes_Click", Flags)!.Invoke(control, new object[] { control, EventArgs.Empty });
                        await reply.Task;
                        string label = " career=" + career + " sample=" + sample + " repeat=" + repeat;
                        check(loaded && contact.Notes == expected + "\nSynthetic edit", "Contact notes modal loads and accepts exact synthetic plain text" + label);
                        check(closedDialog != null && closedDialog.IsDisposed && editor.Enabled &&
                            !Forms.Application.OpenForms.OfType<frmNotes>().Any(), "Repeated contact notes closes and disposes its modal without blocking the editor" + label);
                    }
                }
                var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
                check(reopened.Contacts.Select(item => item.Notes).SequenceEqual(character.Contacts.Select(item => item.Notes)),
                    "Contact notes edits persist through the unchanged save format career=" + career);
            }
            finally
            {
                editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
                editor.Close();
            }
            check(editor.IsDisposed && owner.Enabled, "Contact notes editor cleanup restores owner availability career=" + career);
        }
    }
}
