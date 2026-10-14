using System;
using System.Collections.Generic;
using System.Linq;
using Majorsilence.Forms;

namespace Chummer
{
    // Providers own categories/sources; editable combos own text and popup interaction.
    internal sealed class SearchableChoices
    {
        internal bool IsFiltering { get; private set; }
        private ComboBox combo;
        private Func<IEnumerable<object>> available;
        private Func<object, string> name;
        private bool openingFiltered;
        internal void Refresh(string query = "")
        {
            if (IsFiltering) return;
            IsFiltering = true;
            try
            {
                string text = combo.Text;
                int caret = combo.SelectionStart;
                combo.Items.Clear();
                combo.Items.AddRange(available().Where(item => name(item).IndexOf(query,
                    StringComparison.OrdinalIgnoreCase) >= 0).ToArray());
                combo.Text = text;
                combo.SelectionStart = caret;
                combo.SelectionLength = 0;
            }
            finally { IsFiltering = false; }
        }
        internal static SearchableChoices Attach(ComboBox combo, IEnumerable<object> choices, Func<object, string> name)
        {
            var snapshot = choices.ToArray();
            return Attach(combo, () => snapshot, name);
        }
        internal static SearchableChoices Attach(ComboBox combo, Func<IEnumerable<object>> available, Func<object, string> name)
        {
            var behavior = new SearchableChoices { combo = combo, available = available, name = name };
            bool editing = false;
            combo.KeepInputFocusOnDropDown = true;
            combo.DropDownOpening += (_, _) =>
            {
                // Browsing takes popup focus for reliable mouse selection; typed suggestions
                // keep the caret in the input so the next character continues the search.
                combo.KeepInputFocusOnDropDown = behavior.openingFiltered;
                if (!behavior.openingFiltered) behavior.Refresh();
            };
            combo.TextUpdate += (_, _) => editing = true;
            combo.TextChanged += (_, _) =>
            {
                if (behavior.IsFiltering || (combo.SelectedItem != null && combo.Text == combo.SelectedItem.ToString())) return;
                bool show = editing || combo.DroppedDown;
                editing = false;
                combo.DroppedDown = false;
                behavior.Refresh(combo.Text);
                if (show && combo.Items.Count > 0)
                {
                    behavior.openingFiltered = true;
                    try { combo.DroppedDown = true; }
                    finally { behavior.openingFiltered = false; }
                }
            };
            return behavior;
        }
    }
}
