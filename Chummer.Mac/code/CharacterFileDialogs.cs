using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Forms;
using Majorsilence.Forms.Backends;

namespace Chummer
{
    internal static class CharacterFileDialogs
    {
        private static string lastDirectory;
        private static readonly FileDialogFilter[] filters =
        {
            new FileDialogFilter("Chummer characters (*.chum)", new[] { "*.chum" }),
            new FileDialogFilter("All files", new[] { "*" })
        };

        private static string StartDirectory(string currentFile)
        {
            string directory = lastDirectory;
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) return Path.GetFullPath(directory);
            var settings = SettingsStore.CurrentUser.CreateSubKey("Software\\Chummer");
            directory = settings.GetValue("characterfiledirectory") as string;
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) return Path.GetFullPath(directory);
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chummer");
            Directory.CreateDirectory(directory);
            return directory;
        }

        internal static async Task<string[]> OpenAsync(Form owner)
        {
            var paths = await NativeFileDialogs.OpenAsync(owner, new OpenFileRequest
            {
                Title = "Open Character", AllowMultiple = true, Filters = filters,
                InitialDirectory = StartDirectory(null)
            });
            if (paths.Length > 0) RememberDirectory(paths[0]);
            return paths;
        }

        internal static string ChooseSavePath(Form owner, Character character)
        {
            try { return ChooseSavePathCore(owner, character); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is ArgumentException || error is NotSupportedException ||
                                          error is InvalidOperationException)
            {
                ShowError(owner, "Save Character", error);
                return null;
            }
        }

        internal static string SuggestedFileName(Character character, DateTime timestamp)
        {
            string name = string.IsNullOrWhiteSpace(character.Alias)
                ? "chummer-" + timestamp.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture)
                : character.Alias.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars().Concat(new[] { '\\', ':', '*', '?', '"', '<', '>', '|' }))
                name = name.Replace(invalid, '_');
            return name.EndsWith(".chum", StringComparison.OrdinalIgnoreCase) ? name : name + ".chum";
        }

        private static string ChooseSavePathCore(Form owner, Character character)
        {
            string name = SuggestedFileName(character, DateTime.Now);
            string path = NativeFileDialogs.Save(owner, new SaveFileRequest
            {
                Title = "Save Character", SuggestedFileName = Path.GetFileNameWithoutExtension(name), DefaultExtension = "chum",
                InitialDirectory = StartDirectory(character.FileName), Filters = filters
            });
            if (string.IsNullOrEmpty(path)) return null;
            if (!Path.HasExtension(path)) path += ".chum";
            return path;
        }

        internal static bool TrySave(Form owner, Character character, string path)
        {
            string previousPath = character.FileName;
            try
            {
                character.FileName = path;
                character.Save();
                RememberDirectory(path);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is ArgumentException || error is NotSupportedException)
            {
                character.FileName = previousPath;
                ShowError(owner, "Save Character", error);
                return false;
            }
        }

        internal static void ShowError(Form owner, string title, Exception error)
            => MessageBox.Show(owner, error.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

        private static void RememberDirectory(string path)
        {
            lastDirectory = Path.GetDirectoryName(Path.GetFullPath(path));
            try
            {
                var settings = SettingsStore.CurrentUser.CreateSubKey("Software\\Chummer");
                settings.SetValue("characterfiledirectory", lastDirectory);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                // A successfully written character remains saved even if preferences
                // cannot be persisted. The folder is still remembered for this session.
                System.Diagnostics.Trace.WriteLine(error);
            }
        }
    }
}
