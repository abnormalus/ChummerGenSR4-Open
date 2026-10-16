using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer
{
    // Platform-specific presentation belongs here; commands and character behavior stay shared.
    internal static class AppPlatform
    {
        internal static bool UsesNativeMenus => System.OperatingSystem.IsMacOS();
        internal static void ConfigureMenus(Form owner, MenuStrip menus, MenuItem preferences, MenuItem about, MenuItem quit, Action openPreferences)
        {
            owner.FormClosing += (_, _) => { AppDiagnostics.CaptureUiState(); AppDiagnostics.Record("main-closing"); };
            owner.FormClosed += (_, _) => { AppDiagnostics.CaptureUiState(); AppDiagnostics.Record("main-closed"); };
            if (UsesNativeMenus)
            {
                var macPreferences = new ToolStripMenuItem(LanguageManager.Instance.GetString("Menu_MacPreferences")) { Name = "preferences" };
                macPreferences.Click += (_, _) => openPreferences();
                var macQuit = new ToolStripMenuItem(LanguageManager.Instance.GetString("Menu_MacQuit")) { Name = "quitApplication" };
                macQuit.Click += (_, _) => QuitApplication();
                try { DesktopMenus.Install(owner, menus, macPreferences, about, macQuit); AppDiagnostics.Record("native-menu-installed");
                    var nativeWindow = owner.ToAvaloniaWindow();
                    void RefreshFallbackCommands()
                    {
                        // The Mac application menu may also be hidden in full screen.
                        // Retain its essential commands in the visible shared strip.
                        bool fullScreen = nativeWindow.WindowState == Avalonia.Controls.WindowState.FullScreen;
                        preferences.Visible = fullScreen; quit.Visible = fullScreen;
                        DesktopMenus.Refresh(owner);
                    }
                    nativeWindow.PropertyChanged += (_, change) => {
                        if (change.Property == Avalonia.Controls.Window.WindowStateProperty) RefreshFallbackCommands();
                    };
                    RefreshFallbackCommands();
                }
                catch (Exception error) { AppDiagnostics.Record("native-menu-fallback", error); }
            }
        }
        internal static bool QuitApproved { get; private set; }
        internal static bool QuitInProgress { get; private set; }
        internal static async void QuitApplication()
        {
            try { await QuitApplicationAsync(); }
            catch (Exception error) { AppDiagnostics.Record("quit-failed", error); }
        }
        internal static async Task QuitApplicationAsync()
        {
            if (QuitInProgress) return;
            QuitInProgress = true;
            QuitApproved = false;
            AppDiagnostics.CaptureUiState();
            AppDiagnostics.Record("quit-start");
            try
            {
                NativeDialogHost.CancelAll();
                // Cancel preparation before the one shared close sequence, so a running
                // batch does not cancel Quit or repeat unsaved-character prompts.
                foreach (var editor in Application.OpenForms.OfType<frmCreate>().ToArray())
                    await editor.CancelPacksForQuitAsync();
                foreach (var operation in Application.OpenForms.OfType<BulkProgressDialog>().ToArray())
                    await operation.CancelForQuitAsync();
                foreach (var batch in Application.OpenForms.OfType<frmPrintMultiple>().ToArray())
                    await batch.CancelPreparationForQuitAsync();
                Application.Exit();
                AppDiagnostics.CaptureUiState();
                QuitApproved = Application.OpenForms.Count == 0;
                AppDiagnostics.Record(QuitApproved ? "quit-completed" : "quit-cancelled-or-incomplete");
            }
            catch (Exception error) { QuitApproved = false; AppDiagnostics.Record("quit-failed", error); throw; }
            finally { QuitInProgress = false; }
        }
        internal static bool CanTerminateAfterQuit => QuitApproved && Application.OpenForms.Count == 0;
        internal static void OpenLogs(Form owner)
        {
            try
            {
                Directory.CreateDirectory(AppDiagnostics.DirectoryPath);
                Process.Start(new ProcessStartInfo(AppDiagnostics.DirectoryPath) { UseShellExecute = true });
            }
            catch (Exception error)
            {
                AppDiagnostics.Record("open-logs-failed", error);
                MessageBox.Show(owner, "Unable to open diagnostic logs.\n" + AppDiagnostics.DirectoryPath,
                    "Diagnostic Logs", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
