using System;
using System.Linq;
using Majorsilence.Forms;

namespace Chummer
{
    internal static class AliasRandomizer
    {
        internal static void Attach(Form owner, Character character, TextBox alias, Label metatype)
        {
            var menu = new ContextMenuStrip();
            var culture = menu.Items.Add(LanguageManager.Instance.GetString("Alias_WithCulture"));
            var five = menu.Items.Add(LanguageManager.Instance.GetString("Alias_GenerateFive"));
            var undo = menu.Items.Add(LanguageManager.Instance.GetString("Alias_Undo")); undo.Enabled = false;
            var button = new SplitButton { Name = "cmdRandomizeAlias", Text = LanguageManager.Instance.GetString("Alias_Randomize"),
                Width = 110, Height = Math.Max(24, alias.Height), AutoSize = false, SplitMenuStrip = menu };
            alias.Parent.Controls.Add(button);
            bool layingOut = false, editing = false;
            string previousAlias = null, generatedAlias = null;
            var preferences = new StreetNamePreferences();
            void Layout()
            {
                if (layingOut) return; layingOut = true;
                try { button.Left = metatype.Left - button.Width - 8; button.Top = alias.Top - 2;
                    alias.Width = Math.Max(40, button.Left - alias.Left - 6); }
                finally { layingOut = false; }
            }
            alias.LocationChanged += (_, _) => Layout(); alias.SizeChanged += (_, _) => Layout();
            metatype.LocationChanged += (_, _) => Layout(); owner.Load += (_, _) => Layout(); Layout();
            void Apply(string name)
            {
                previousAlias = alias.Text; generatedAlias = name; alias.Text = name; undo.Enabled = true;
            }
            undo.Click += (_, _) => { AppDiagnostics.Action("alias.undo"); if (alias.Text == generatedAlias) alias.Text = previousAlias; undo.Enabled = false; };
            alias.TextChanged += (_, _) => { if (alias.Text != generatedAlias) undo.Enabled = false; };
            button.Click += (_, _) =>
            {
                try
                {
                    AppDiagnostics.Action("alias.randomize");
                    var results = StreetNameEngine.Load().Suggest(character, new StreetNamePreferences { CharacterAware = true, Strength = "None" }, 1);
                    if (results.Count > 0) Apply(results[0].Name); else AppErrors.Inform(owner, "Alias_NoResults");
                }
                catch (Exception error) { AppDiagnostics.Record("alias-generation-failed", error); AppErrors.Inform(owner, "Alias_Error"); }
            };
            async void Options(object sender, EventArgs args)
            {
                if (editing) return; editing = true;
                try
                {
                    AppDiagnostics.Action("alias.culture-options");
                    var choice = await AliasGeneratorDialog.ShowAsync(owner, character, StreetNameEngine.Load(), preferences);
                    if (choice != null && Application.OpenForms.Contains(owner)) { preferences = choice.Preferences; Apply(choice.Name); }
                }
                catch (Exception error) { AppDiagnostics.Record("alias-generation-dialog-failed", error); AppErrors.Inform(owner, "Alias_Error"); }
                finally { editing = false; }
            }
            culture.Click += Options; five.Click += Options;
            owner.FormClosed += (_, _) => menu.Dispose();
        }
    }
}
