#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Majorsilence.Forms;

namespace Chummer;

// Compare actual editor totals, not only XML equality: free knowledge points
// and focus bonding must retain their costs through editor initialization.
internal static class BuildPointPersistenceChecks
{
    internal static void Run(Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        character.BuildMethod = CharacterBuildMethod.BP;
        character.Skills.RemoveAll(skill => skill.KnowledgeSkill);
        character.Skills.Add(new Skill(character) { Name = "Test knowledge", KnowledgeSkill = true,
            SkillCategory = "Academic", Attribute = "LOG", Rating = 1 });
        int Total(Character model)
        {
            using var editor = new frmCreate(model); editor.Show();
            int remaining = (int)typeof(frmCreate).GetMethod("CalculateBP", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(editor, null)!;
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
            editor.Close(); return remaining;
        }
        int original = Total(character);
        character.Skills.Single(skill => skill.KnowledgeSkill).Rating = 2;
        check(Total(character) == original, "knowledge skills within the free allowance do not spend BP");
        var reloaded = new Character(); reloaded.LoadSnapshot(character.CaptureSnapshot());
        check(Total(reloaded) == original, "free knowledge skill costs survive save-format round trip and editor reopening");
        character.Foci.Add(new Focus { Name = "Sustaining Focus", Rating = 3, GearId = Guid.NewGuid().ToString() });
        int bonded = Total(character);
        check(bonded == original - 3 * character.Options.BPFocus, "bonded focus force retains its configured BP cost");
        reloaded.LoadSnapshot(character.CaptureSnapshot());
        check(Total(reloaded) == bonded, "focus BP cost survives save-format round trip and editor reopening");
    }
}
