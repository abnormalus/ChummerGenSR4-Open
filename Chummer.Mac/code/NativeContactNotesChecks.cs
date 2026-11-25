#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Synthetic notes only. Exercise the real ContactControl handler and native modal;
// no private character, contact identity or text is written into reports/images.
internal static class NativeContactNotesChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
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
                var inputs = NativeContactInteractions.Controllers(editor, enemy: false);
                var section = NativeContactInteractions.Section(editor, enemy: false);
                var texts = new[] { "", "Synthetic writer’s notes; ordinary prose. ".PadRight(349, 'a'), "Synthetic ordinary notes - punctuation. ".PadRight(335, 'b') };
                for (int sample = 0; sample < texts.Length; sample++)
                {
                    var contact = sample == 2 ? second : first;
                    var control = inputs.Single(item => ReferenceEquals(item.ContactObject, contact));
                    contact.Notes = texts[sample];
                    for (int repeat = 0; repeat < 2; repeat++)
                    {
                        string expected = contact.Notes; bool loaded = false; NotesWindow? closedDialog = null;
                        int closing = 0;
                        var reply = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                        {
                            try
                            {
                                var deadline = DateTime.UtcNow.AddSeconds(20);
                                while (NativeDialogHost.ActiveWindow is not NotesWindow)
                                { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Contact notes did not open."); await Task.Delay(10); }
                                var window = closedDialog = (NotesWindow)NativeDialogHost.ActiveWindow!;
                                while (!window.IsVisible || window.Bounds.Width <= 0 || window.Bounds.Height <= 0)
                                { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Contact notes did not become visible."); await Task.Delay(10); }
                                // Opening/closing within one dispatcher turn can avoid the
                                // paint/focus path implicated by an opening-time crash.
                                await Task.Delay(100);
                                if (sample == 1)
                                {
                                    window.Width = 250;
                                    await Task.Delay(60);
                                    check(window.Input.Bounds.Width <= window.ClientSize.Width &&
                                        window.Input.TextWrapping == Avalonia.Media.TextWrapping.Wrap &&
                                        window.Input.AcceptsReturn, "Narrow contact notes keeps the multiline editor inside the window");
                                }
                                loaded = window.Input.Text == expected && window.IsVisible;
                                window.Input.Text = expected + "\nSynthetic edit";
                                window.Closing += (_, _) => closing++;
                                if (repeat == 0)
                                    window.Close();
                                else
                                    window.Input.RaiseEvent(new Avalonia.Input.KeyEventArgs
                                    {
                                        Key = Avalonia.Input.Key.Escape,
                                        RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent
                                    });
                                reply.TrySetResult();
                            }
                            catch (Exception error)
                            { closedDialog?.Close(); reply.TrySetException(error); }
                        });
                        await NativeContactInteractions.ClickAsync(section, section.Rows[control].Notes, check);
                        await reply.Task;
                        string label = " career=" + career + " sample=" + sample + " repeat=" + repeat;
                        check(loaded && contact.Notes == expected + "\nSynthetic edit", "Contact notes modal loads and accepts exact synthetic plain text" + label);
                        check(closedDialog is { IsVisible: false } && closing == 1 && editor.Enabled && owner.Enabled &&
                            NativeDialogHost.ActiveWindow is not NotesWindow,
                            "Contact notes closes once and restores the editor after window close or Escape" + label);
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
