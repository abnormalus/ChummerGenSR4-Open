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

internal static class NativeCareerListChecks
{
    internal static async Task RunAsync(frmMain main, Character fixture, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = true;
        using var editor = new frmCareer(character) { MdiParent = main };
        editor.Show();
        T Field<T>(string name) => (T)typeof(frmCareer).GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(editor)!;
        var outer = Field<Forms.TabControl>("tabCharacterTabs");

        foreach (string name in new[] { "Karma", "Nuyen", "Calendar" })
        {
            bool calendar = name == "Calendar";
            outer.SelectedTab = Field<Forms.TabPage>(calendar ? "tabCalendar" : "tabKarma");
            var source = Field<Forms.ListView>("lst" + name);
            var host = main.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "NativeCareer" + name);
            var content = (DockPanel)host.NativeControl!;
            var list = content.Children.OfType<ListBox>().Single();
            var first = new Forms.ListViewItem("1/1/2070 12:00 AM");
            var second = new Forms.ListViewItem("1/2/2070 12:00 AM");
            source.Items.Add(first);
            source.Items.Add(second);
            if (!calendar) source.Sort();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            check(source.Parent == null && host.Visible && list.IsEffectivelyVisible && list.Bounds.Height > 0 &&
                list.Items.Cast<Forms.ListViewItem>().SequenceEqual(source.Items.Cast<Forms.ListViewItem>()),
                name + " displays the native list with the editor's order");
            list.SelectedItem = second;
            check(source.SelectedItems.Count == 1 && ReferenceEquals(source.SelectedItems[0], second),
                name + " selection identifies the original editor row");
            if (!calendar)
            {
                var header = content.Children.OfType<Grid>().Single().Children.OfType<Button>().First();
                var before = source.Items.Cast<Forms.ListViewItem>().ToArray();
                header.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(!source.Items.Cast<Forms.ListViewItem>().SequenceEqual(before) &&
                    list.Items.Cast<Forms.ListViewItem>().SequenceEqual(source.Items.Cast<Forms.ListViewItem>()) &&
                    ReferenceEquals(list.SelectedItem, second), name + " header sorting retains the selected row");
            }
            source.Items.Remove(second);
            source.Items.Remove(first);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(list.SelectedItem == null && !list.Items.Contains(second),
                name + " refresh clears a removed row");
        }
        editor.Close();
    }
}
