using System;
using System.Linq;
using System.Xml;
using System.Drawing;
using Majorsilence.Forms;

namespace Chummer
{
    internal static class PromptDetails
    {
        internal const string Placeholder = "<placeholder>";
        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? Placeholder : value;

        private static bool HasDetail(XmlNode bonus) => bonus?.SelectSingleNode("selecttext | selectrestricted") != null;
        internal static bool CanEdit(Quality quality)
        {
            if (quality == null) return false;
            if (HasDetail(quality.Bonus)) return true;
            // Legacy quality creation does not retain its bonus XML. Older saved
            // qualities can therefore have an empty bonus; consult their rules definition.
            var definition = XmlManager.Instance.Load("qualities.xml").SelectNodes("/chummer/qualities/quality")
                .Cast<XmlNode>().FirstOrDefault(node => node["name"]?.InnerText == quality.Name);
            return HasDetail(definition?["bonus"]);
        }
        internal static bool CanEdit(Gear gear) => gear != null &&
            (HasDetail(gear.Bonus) || gear.Category == "ID/Credsticks");

        internal static void Apply(Character character, Quality quality, string value)
        {
            quality.Extra = Normalize(value);
            UpdateImprovement(character, Improvement.ImprovementSource.Quality, quality.InternalId, quality.Extra);
        }

        internal static void Apply(Character character, Gear gear, string value)
        {
            if (HasDetail(gear.Bonus))
            {
                gear.Extra = Normalize(value);
                UpdateImprovement(character, Improvement.ImprovementSource.Gear, gear.InternalId, gear.Extra);
            }
            else gear.GearName = Normalize(value);
        }

        private static void UpdateImprovement(Character character, Improvement.ImprovementSource source, string id, string value)
        {
            foreach (var improvement in character.Improvements.Where(item => item.ImproveSource == source &&
                item.SourceName == id && (item.ImproveType == Improvement.ImprovementType.Text ||
                item.ImproveType == Improvement.ImprovementType.Restricted)))
                improvement.ImprovedName = value;
        }

        internal static void Attach(Form owner, Character character, TreeView tree, Func<string, Quality> findQuality,
            Func<string, Gear> findGear, Action changed, params ContextMenuStrip[] menus)
        {
            if (findQuality != null)
                tree.NodeFormatting += (_, args) =>
                {
                    var node = args.Node;
                    var quality = node.Tag == null ? null : findQuality(node.Tag.ToString());
                    if (quality == null) return;
                    if (DetailChoices.RequiresReview(quality.Name, quality.Extra)) node.ForeColor = Color.Red;
                    else if (node.ForeColor == Color.Red) node.ForeColor = Color.Empty;
                };

            bool Eligible()
            {
                string id = tree.SelectedNode?.Tag?.ToString();
                return id != null && (CanEdit(findQuality?.Invoke(id)) || CanEdit(findGear?.Invoke(id)));
            }
            void Edit()
            {
                AppDiagnostics.Action("detail.edit");
                string id = tree.SelectedNode?.Tag?.ToString();
                if (id == null) return;
                var quality = findQuality?.Invoke(id);
                var gear = findGear?.Invoke(id);
                if (!CanEdit(quality) && !CanEdit(gear)) return;
                string name = quality?.DisplayNameShort ?? gear.DisplayNameShort;
                string previous = quality?.Extra ?? (HasDetail(gear.Bonus) ? gear.Extra : gear.GearName);
                using var dialog = new frmSelectText { AutoAcceptPrefilled = false, Text = "Edit Detail",
                    Description = "Detail for " + name + ":", SelectedValue = previous };
                dialog.ConfigureChoices(character, quality?.Name);
                if (dialog.ShowDialog(owner) != DialogResult.OK) return;
                if (dialog.SelectedValue == previous) return;
                if (quality != null) Apply(character, quality, dialog.SelectedValue);
                else Apply(character, gear, dialog.SelectedValue);
                tree.SelectedNode.Text = quality?.DisplayName ?? gear.DisplayName;
                changed();
            }
            foreach (var menu in menus)
            {
                var command = new ToolStripMenuItem("Edit Detail…") { Name = "editPromptDetail" };
                command.Click += (_, _) => Edit();
                menu.Items.Add(command);
                menu.Opening += (_, _) => command.Visible = Eligible();
            }
            tree.NodeMouseDoubleClick += (_, args) => { tree.SelectedNode = args.Node; Edit(); };
            tree.NodeMouseClick += (_, args) => { if (args.Button == MouseButtons.Right) tree.SelectedNode = args.Node; };
        }
    }
}
