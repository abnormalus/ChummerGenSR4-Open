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
                    Application.Run(new frmMain());
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
        }
    }
}