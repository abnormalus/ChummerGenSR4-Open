using System;
using System.Diagnostics;
using System.IO;
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
                macQuit.Click += (_, _) =>
                {
                    AppDiagnostics.CaptureUiState();
                    AppDiagnostics.Record("quit-start");
                    try
                    {
                        Application.Exit();
                        AppDiagnostics.CaptureUiState();
                        AppDiagnostics.Record(Application.OpenForms.Count == 0 ? "quit-completed" : "quit-cancelled-or-incomplete");
                    }
                    catch (Exception error) { AppDiagnostics.Record("quit-failed", error); throw; }
                };
                try { DesktopMenus.Install(owner, menus, macPreferences, about, macQuit); AppDiagnostics.Record("native-menu-installed");
                    // Remove Windows-only placements from the source tree projected into native menus.
                    preferences.Visible = false; quit.Visible = false;
                }
                catch (Exception error) { AppDiagnostics.Record("native-menu-fallback", error); }
            }
        }
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
