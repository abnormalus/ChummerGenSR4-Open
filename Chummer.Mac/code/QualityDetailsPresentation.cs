#nullable enable
using System.Linq;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Projects existing quality data into the two editor detail labels. No model,
// accounting or persistence writes; callers explicitly choose BP versus Karma.
internal static class QualityDetailsPresentation
{
    internal static void Initialize(Forms.TreeView tree, Forms.Label cost, Forms.Label source, Forms.ToolTip tooltip)
    {
        Clear(cost, source, tooltip);
        // The compatibility tree deliberately emits no AfterSelect when its
        // selection is cleared. Its invalidation still exposes that transition.
        // These controls share one form lifetime; no global observer is retained.
        void ClearIfUnselected()
        {
            if (tree.SelectedNode == null && (cost.Text != "" || source.Text != "" ||
                !string.IsNullOrEmpty(tooltip.GetToolTip(source))))
                Clear(cost, source, tooltip);
        }
        tree.Invalidated += (_, _) => ClearIfUnselected();
        // Before creation, Invalidate deliberately does nothing. Reconcile any
        // pre-show selection clearing when this tree first becomes live.
        tree.HandleCreated += (_, _) => ClearIfUnselected();
    }

    internal static void Clear(Forms.Label cost, Forms.Label source, Forms.ToolTip tooltip)
    {
        // Initial, category and stale selections must not expose designer text
        // or retain a previous quality's cost/source link.
        cost.Text = ""; source.Text = ""; tooltip.SetToolTip(source, "");
    }

    internal static void Refresh(Character character, bool useKarma, Forms.TreeNode? selected,
        Forms.Label cost, Forms.Label source, Forms.ToolTip tooltip)
    {
        Clear(cost, source, tooltip);
        if (selected == null || selected.Level == 0 || selected.Tag == null) return;
        var quality = character.Qualities.FirstOrDefault(q => q.InternalId == selected.Tag.ToString());
        if (quality == null) return;
        var options = character.Options;
        source.Text = options.LanguageBookShort(quality.Source) + " " + quality.Page;
        tooltip.SetToolTip(source, options.LanguageBookLong(quality.Source) + " " +
            LanguageManager.Instance.GetString("String_Page") + " " + quality.Page);
        cost.Text = (useKarma ? quality.BP * options.KarmaQuality : quality.BP).ToString() + " " +
            LanguageManager.Instance.GetString(useKarma ? "String_Karma" : "String_BP");
    }
}
