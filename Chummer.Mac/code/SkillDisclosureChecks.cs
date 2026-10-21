#nullable enable
using System;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class SkillDisclosureChecks
{
    internal static void Run(Character source, Action<bool, string> check)
    {
        const string path = "Software\\Chummer", key = "perceptionsubcategoriesexpanded";
        var old = SettingsStore.GetValue(path, key);
        try
        {
            SkillDisplayPreferences.PerceptionExpanded = false;
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
            var model = new Skill(character) { Name = "Perception", Attribute = "INT", Specialization = "Visual" };
            var metaModel = new Skill(character) { Name = "Perception (Visual)", Attribute = "INT", IsMeta = true, MetaBase = "Perception" };
            using var controller = new SkillControl { SkillObject = model };
            using var meta = new SkillControl { SkillObject = metaModel };
            controller.RatingChanged += _ => { }; controller.SpecializationChanged += _ => { };
            meta.SkillIsMeta = true;
            using var panel = new Forms.Panel(); panel.Controls.Add(controller); panel.Controls.Add(meta);
            using var section = new CreationActiveSkillSection(null, panel, new[] { controller, meta });
            var row = section.Rows[controller]; var detail = section.Rows[meta];
            var before = character.CaptureSnapshot();
            check(!row.SpecializationDisclosure.Expanded && row.Specialization.Parent is Control parent && !parent.IsVisible && model.Specialization == "Visual",
                "Skill specialization starts collapsed without clearing a saved specialization");
            row.SpecializationDisclosure.Toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(row.SpecializationDisclosure.Expanded && row.Specialization.Parent is Control expanded && expanded.IsVisible,
                "Skill plus reveals only its own specialization line");
            row.SpecializationDisclosure.Toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(!row.SpecializationDisclosure.Expanded && model.Specialization == "Visual" && character.CaptureSnapshot().SequenceEqual(before),
                "Skill specialization collapse preserves exact character snapshot and specialty");
            check(!detail.Layout.IsVisible && row.PerceptionDisclosure?.Content?.ToString() == ">",
                "Perception derived subcategories default to collapsed with a disclosure arrow");
            row.PerceptionDisclosure!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(detail.Layout.IsVisible && SkillDisplayPreferences.PerceptionExpanded && character.CaptureSnapshot().SequenceEqual(before),
                "Perception disclosure reveals derived rows without changing character rules or data");
            var document = XDocument.Load(Path.Combine(MacPaths.UserDirectory, "preferences.xml"));
            check(document.Descendants("value").Any(e => (string?)e.Attribute("name") == key && e.Value == "True"),
                "Perception expansion writes the existing persistent app preference store");
            meta.Visible = false; meta.Visible = true;
            check(detail.Layout.IsVisible, "Perception expanded preference survives canonical filter refresh");
            row.PerceptionDisclosure.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            meta.Visible = false; meta.Visible = true;
            check(!detail.Layout.IsVisible && !SkillDisplayPreferences.PerceptionExpanded,
                "Perception collapsed preference survives canonical filter refresh");
            var held = row.PerceptionDisclosure; section.Dispose();
            held.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(!SkillDisplayPreferences.PerceptionExpanded, "Disposed Perception disclosure cannot change app preferences");
        }
        finally
        {
            if (old == null) SettingsStore.DeleteValue(path, key); else SettingsStore.SetValue(path, key, old);
        }
    }
}
