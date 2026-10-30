#nullable enable
using System;
using System.Reflection;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Actual editor selections, with fixed expected costs; no expected value is
// produced by the presentation component under test.
internal static class QualityDetailsChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static void Run(Character original, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        foreach (bool karma in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot());
            character.Created = career; character.BuildMethod = karma ? CharacterBuildMethod.Karma : CharacterBuildMethod.BP;
            character.Options.KarmaQuality = 2;
            var quality = new Quality(character) { Name = "Synthetic detail", BP = 3, Source = "SR4", Page = "77" };
            character.Qualities.Add(quality);
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            var cost = Field<Forms.Label>(editor, "lblQualityBP"); var source = Field<Forms.Label>(editor, "lblQualitySource");
            var tooltip = Field<Forms.ToolTip>(editor, "tipTooltip"); var tree = Field<Forms.TreeView>(editor, "treQualities");
            string context = " career=" + career + " karmaBuild=" + karma;
            bool Empty() => cost.Text == "" && source.Text == "" && string.IsNullOrEmpty(tooltip.GetToolTip(source));
            check(Empty(), "Quality details start empty without designer placeholders" + context);
            tree.Nodes.Clear(); var root = new Forms.TreeNode("Synthetic category") { Tag = "category" };
            var node = new Forms.TreeNode("Synthetic detail") { Tag = quality.InternalId }; root.Nodes.Add(node); tree.Nodes.Add(root);
            byte[] before = character.CaptureSnapshot(); bool dirty = Field<bool>(editor, "_blnIsDirty");
            tree.SelectedNode = node;
            check(cost.Text == ((career || karma) ? "6 " + LanguageManager.Instance.GetString("String_Karma") : "3 " + LanguageManager.Instance.GetString("String_BP")) &&
                source.Text == character.Options.LanguageBookShort("SR4") + " 77" && tooltip.GetToolTip(source).EndsWith(" 77"),
                "Quality details preserve fixed BP Karma and source projection" + context);
            tree.SelectedNode = root;
            check(Empty(), "Quality category selection clears prior cost source and tooltip" + context);
            tree.SelectedNode = node;
            // Retag without AfterSelect, then invoke the real handler as a
            // queued refresh would after its model disappeared.
            node.Tag = Guid.NewGuid().ToString();
            editor.GetType().GetMethod("treQualities_AfterSelect", Flags)!.Invoke(editor, new object?[] { tree, null });
            check(Empty(), "Quality stale identity clears prior details safely" + context);
            node.Tag = null;
            editor.GetType().GetMethod("treQualities_AfterSelect", Flags)!.Invoke(editor, new object?[] { tree, null });
            check(Empty(), "Quality missing identity leaves details empty" + context);
            tree.SelectedNode = null;
            check(Empty() && before.AsSpan().SequenceEqual(character.CaptureSnapshot()) && dirty == Field<bool>(editor, "_blnIsDirty"),
                "Quality detail transitions preserve character and editor dirty state" + context);
        }
    }
}
