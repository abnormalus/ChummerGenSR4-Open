using System;
using System.Collections.Generic;
using System.Linq;
using Majorsilence.Forms;




namespace Chummer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            DesktopMenus.ApplicationName = "Chummer";
            ListBox.DefaultIncrementalSearch = true;
            Form.ApplicationIconOverride = new Majorsilence.Forms.Drawing.Icon(
                System.IO.Path.Combine(AppContext.BaseDirectory, "branding", "chummer.ico"));
            Form.CenterModalDialogsOnRootOwner = true;
            if (args.Length > 0 && args[0] == "--smoke-test")
            {
                Environment.Exit(MacSmokeTests.Run(args));
                return;
            }
            AppDiagnostics.Initialize();
            try
            {
                MacPaths.Initialize();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                LanguageManager.Instance.Load(GlobalOptions.Instance.Language, null);
                if (LanguageManager.Instance.Loaded)
                {
                    using var main = new frmMain();
                    using var watchdog = AppDiagnostics.StartWatchdog();
                    Application.Run(main);
                }
                else
                    Application.Exit();
            }
            catch (Exception error)
            {
                AppDiagnostics.Record("fatal-main-loop", error);
                Environment.ExitCode = 1;
                try { MessageBox.Show("Chummer encountered an error. Diagnostic logs are available at:\n" + AppDiagnostics.DirectoryPath,
                    "Chummer", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
            }
            finally { AppDiagnostics.Record("session-end"); }
            // The loop has returned and using scopes have disposed the main window and watchdog.
            // Explicit Quit must also end any surviving platform/native worker threads.
            if (AppPlatform.CanTerminateAfterQuit)
            {
                AppDiagnostics.Record("process-exit");
                Environment.Exit(Environment.ExitCode);
            }
        }
    }
}
