#nullable enable
using System;
using System.Linq;
using Avalonia.Controls;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeSelectionInput
{
    internal static ComboBox Bind(Forms.Form owner, Forms.TabPage page, Forms.ComboBox source)
    {
        var input = new ComboBox { Name = "Native" + source.Name, MinWidth = 95 };
        page.Controls.Remove(source);
        bool syncing = false;
        string[] choices = [];
        void Refresh()
        {
            if (syncing)
                return;
            syncing = true;
            try
            {
                var current = source.Items.Cast<object>().Select(source.GetItemText).ToArray();
                if (!current.SequenceEqual(choices))
                    input.ItemsSource = choices = current;
                input.SelectedIndex = source.SelectedIndex;
                input.IsEnabled = source.Enabled;
                input.IsVisible = source.Visible;
            }
            finally { syncing = false; }
        }
        void Changed(object? sender, EventArgs args) => Refresh();
        var observer = new NativeControlObservation(source, Refresh);
        source.SelectedIndexChanged += Changed;
        source.DataSourceChanged += Changed;
        input.SelectionChanged += (_, _) =>
        {
            if (!syncing && input.SelectedIndex >= 0 && source.Enabled && source.Visible)
            {
                source.SelectedIndex = input.SelectedIndex;
                Refresh();
            }
        };
        Refresh();

        owner.Disposed += (_, _) =>
        {
            observer.Dispose();
            source.SelectedIndexChanged -= Changed;
            source.DataSourceChanged -= Changed;
            source.Dispose();
        };
        return input;
    }
}
