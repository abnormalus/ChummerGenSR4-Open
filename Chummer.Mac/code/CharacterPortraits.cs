using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Forms;
using Majorsilence.Forms.Backends;

namespace Chummer
{
    internal static class CharacterPortraits
    {
        internal static void Attach(Form owner, Character character, PictureBox preview, Button add, Button remove, Action changed)
        {
            preview.SizeMode = PictureBoxSizeMode.Zoom;
            add.Width = 65; add.Text = LanguageManager.Instance.GetString("Portrait_Add");
            var edit = new Button { Name = "editPortrait", Left = add.Left + 70, Top = add.Top, Width = 65,
                Height = add.Height, Text = LanguageManager.Instance.GetString("Portrait_Edit") };
            remove.Left = edit.Left + 70; remove.Width = 65;
            remove.Text = LanguageManager.Instance.GetString("Portrait_Remove");
            edit.Click += async (_, _) =>
            {
                if (string.IsNullOrEmpty(character.Mugshot)) { await AddAsync(owner, character, preview, changed); return; }
                try { await ApplyAsync(owner, character, preview, Convert.FromBase64String(character.Mugshot), changed); }
                catch (Exception error) { Report(owner, error); }
            };
            preview.DoubleClick += async (_, _) =>
            {
                if (!string.IsNullOrEmpty(character.Mugshot))
                {
                    try { await ApplyAsync(owner, character, preview, Convert.FromBase64String(character.Mugshot), changed); }
                    catch (Exception error) { Report(owner, error); }
                }
            };
            add.Parent.Controls.Add(edit);
        }
        internal static async Task AddAsync(Form owner, Character character, PictureBox preview, Action changed)
        {
            try
            {
                var paths = await NativeFileDialogs.OpenAsync(owner, new OpenFileRequest {
                    Title = LanguageManager.Instance.GetString("Portrait_Choose"), AllowMultiple = false,
                    Filters = new[] { new FileDialogFilter("Images", new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp", "*.gif" }) }
                });
                if (paths.Length == 0) return;
                await ApplyAsync(owner, character, preview, await File.ReadAllBytesAsync(paths[0]), changed);
            }
            catch (Exception error) { Report(owner, error); }
        }
        private static async Task ApplyAsync(Form owner, Character character, PictureBox preview, byte[] original, Action changed)
        {
            try
            {
                AppDiagnostics.Action("portrait.edit");
                var result = await PortraitEditor.EditAsync(owner, original);
                if (result == null || !Application.OpenForms.Contains(owner)) return;
                using var stream = new MemoryStream(result);
                var image = Majorsilence.Forms.Drawing.Image.FromStream(stream, true);
                var previous = preview.Image;
                character.Mugshot = Convert.ToBase64String(result);
                preview.Image = image;
                previous?.Dispose();
                changed();
                AppDiagnostics.Record("portrait.applied");
            }
            catch (Exception error) { Report(owner, error); }
        }
        private static void Report(Form owner, Exception error)
        {
            AppDiagnostics.Record("portrait.failed", error);
            AppErrors.Inform(owner, "Portrait_Error");
        }
    }
}
