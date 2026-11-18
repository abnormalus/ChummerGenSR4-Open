#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCybereyeEnhancementChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static async Task RunAsync(frmMain owner, Character original, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character();
            character.LoadSnapshot(original.CaptureSnapshot());
            character.Created = career;
            character.Nuyen = 1000000;
            if (!character.Options.Books.Contains("SR4")) character.Options.Books.Add("SR4");
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true, MdiParent = owner } :
                new frmCreate(character) { Loading = true, MdiParent = owner };
            editor.Show();
            var tree = (Forms.TreeView)editor.GetType().GetField("treCyberware", Flags)!.GetValue(editor)!;
            T Field<T>(string name) => (T)editor.GetType().GetField(name, Flags)!.GetValue(editor)!;
            Field<Forms.TabControl>("tabCharacterTabs").SelectedTab = Field<Forms.TabPage>("tabCyberware");

            async Task Add(string command, string name, int rating = 0, bool addMore = false)
            {
                var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        var deadline = DateTime.UtcNow.AddSeconds(10);
                        while (NativeDialogHost.ActiveWindow is not CyberwarePickerWindow)
                        {
                            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Cyberware picker did not open.");
                            await Task.Delay(20);
                        }
                        var picker = (CyberwarePickerWindow)NativeDialogHost.ActiveWindow;
                        picker.SearchBox.Text = name;
                        picker.ItemList.SelectedItem = picker.Model.Items.Single(item => item.Name == name);
                        if (rating > 0) picker.Model.Rating = rating;
                        (addMore ? picker.AddMoreButton : picker.AcceptButton).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        if (picker.IsVisible) throw new InvalidOperationException("Cyberware picker did not accept the selection.");
                        if (addMore)
                        {
                            CyberwarePickerWindow? next;
                            while ((next = NativeDialogHost.ActiveWindow as CyberwarePickerWindow) == null || ReferenceEquals(next, picker))
                            {
                                if (DateTime.UtcNow >= deadline) throw new TimeoutException("Add More did not reopen the cyberware picker.");
                                await Task.Delay(20);
                            }
                            next.SearchBox.Text = "Datajack";
                            if (next.Model.Items.Any()) throw new InvalidOperationException("Add More lost the cybereye enhancement scope.");
                            next.Close();
                        }
                        completion.SetResult();
                    }
                    catch (Exception error)
                    {
                        NativeDialogHost.CancelAll();
                        completion.SetException(error);
                    }
                });
                editor.GetType().GetMethod(command, Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
            }

            try
            {
                await Add("cmdAddCyberware_Click", "Cybereyes Basic System", 2);
                var eyes = character.Cyberware.Last();
                var hostNode = tree.SelectedNode;
                await Add("tsCyberwareAddAsPlugin_Click", "Low-Light Vision");
                check(ReferenceEquals(tree.SelectedNode, hostNode), "Cybereye enhancement keeps the host selected career=" + career);
                await Add("tsCyberwareAddAsPlugin_Click", "Flare Compensation", addMore: true);
                check(eyes.Children.Any(item => item.Name == "Low-Light Vision") &&
                      eyes.Children.Any(item => item.Name == "Flare Compensation") &&
                      eyes.Children.All(item => item.Parent == eyes),
                    "Successive enhancements attach to the cybereyes career=" + career);
                await Add("cmdAddCyberware_Click", "Datajack");
                check(character.Cyberware.Last().Name == "Datajack" && NativeDialogHost.Count == 0,
                    "Add Cyberware still purchases a separate implant after adding enhancements career=" + career);
            }
            finally
            {
                editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
                editor.Close();
            }
        }
    }
}
