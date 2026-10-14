using System;
using System.Collections.Generic;
using System.Linq;
using Majorsilence.Forms;

namespace Chummer
{
    // Providers supply choices and searchable names; this behavior retains custom text.
    internal sealed class SearchableChoices
    {
        internal bool IsFiltering { get; private set; }
        internal static SearchableChoices Attach(ComboBox combo, IEnumerable<object> available, Func<object, string> name)
        {
            var behavior = new SearchableChoices();
            var choices = available.ToArray();
            bool editing = false;
            combo.KeepInputFocusOnDropDown = true;
            combo.TextUpdate += (_, _) => editing = true;
            combo.TextChanged += (_, _) =>
            {
                if (behavior.IsFiltering || (combo.SelectedItem != null && combo.Text == combo.SelectedItem.ToString())) return;
                behavior.IsFiltering = true;
                try
                {
                    string query = combo.Text;
                    int caret = combo.SelectionStart;
                    bool show = editing || combo.DroppedDown;
                    editing = false;
                    combo.DroppedDown = false;
                    combo.Items.Clear();
                    combo.Items.AddRange(choices.Where(item => name(item).IndexOf(query,
                        StringComparison.OrdinalIgnoreCase) >= 0).ToArray());
                    combo.Text = query;
                    combo.SelectionStart = caret;
                    combo.SelectionLength = 0;
                    if (show) combo.DroppedDown = combo.Items.Count > 0;
                }
                finally { behavior.IsFiltering = false; }
            };
            return behavior;
        }
    }
}
