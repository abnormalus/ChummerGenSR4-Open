using System.Threading.Tasks;
using Majorsilence.Forms.Backends;

namespace Majorsilence.Forms
{
    /// <summary>Native toolkit file pickers attached to the visible presentation window.</summary>
    public static class NativeFileDialogs
    {
        public static Task<string[]> OpenAsync(Form owner, OpenFileRequest request)
            => owner.PresentationWindow.Backend.ShowOpenFileDialog(request);

        public static Task<string?> SaveAsync(Form owner, SaveFileRequest request)
            => owner.PresentationWindow.Backend.ShowSaveFileDialog(request);

        // Existing close/creation workflows require a result before continuing. Pump the toolkit's
        // modal loop instead of blocking its UI thread with Task.Wait or Task.Result.
        public static string? Save(Form owner, SaveFileRequest request)
            => Form.RunModal(SaveAsync(owner, request));
    }
}
