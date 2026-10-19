#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeCreationInputChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    internal static async Task RunAsync(Forms.Form owner, Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = false; character.IgnoreRules = true;
        using var editor = new frmCreate(character) { MdiParent = owner, Loading = true };
        editor.Show(); await Task.Delay(20);
        try
        {
            var section = Field<CreationInputSection>(editor, "creationInputSection");
            check(section.Inputs.Count == 12 && section.Inputs.Values.All(input => input.GetType() == typeof(Avalonia.Controls.NumericUpDown)),
                "Native creation batch exposes eleven attribute inputs and one money allocation input");
            check(section.Inputs.Keys.All(name => Field<Forms.NumericUpDown>(editor, name).Parent == null),
                "Native creation inputs retain detached controller state without drawing legacy spinners");
            var body = Field<Forms.NumericUpDown>(editor, "nudBOD"); var input = section.Inputs["nudBOD"];
            editor.Loading = true; body.Minimum = 1; body.Maximum = 10; body.Value = 2;
            check(input.Minimum == 1 && input.Maximum == 10 && input.Value == 2,
                "Native creation attributes follow controller loading bounds and values");
            editor.Loading = false;
            int before = character.BOD.Value; input.Value = null;
            check(character.BOD.Value == before && body.Value == 2,
                "Cleared inline creation input does not commit a fallback value");
            input.Value = 3;
            check(body.Value == 3 && character.BOD.Value == 3 && Field<bool>(editor, "_blnIsDirty"),
                "Native creation attribute edit uses the existing controller and dirty-state path");
            var money = section.Inputs["nudNuyen"]; money.Value = 2.5m;
            check(character.NuyenBP == 2.5m && Field<Forms.NumericUpDown>(editor, "nudNuyen").Value == 2.5m,
                "Native money allocation preserves decimal model values through existing accounting");
            editor.Loading = true; body.Maximum = 2;
            check(input.Maximum == 2 && input.Value == body.Value && body.Value == 2,
                "Native attribute refresh follows controller range clipping without a stale value");
            editor.Loading = false; input.Value = 1;
            var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
            check(reopened.BOD.Value == character.BOD.Value && reopened.NuyenBP == character.NuyenBP,
                "Native creation attribute and money edits retain the unchanged character serialization contract");
        }
        finally { typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false); editor.Close(); }
    }
}
