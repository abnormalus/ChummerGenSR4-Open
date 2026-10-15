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
            if (args.Length >= 3 && args[0] == "--check-bulk")
                MacPaths.TestUserDirectory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(args[2])), "native-bulk-user");
            AppDiagnostics.Initialize();
            try
            {
                MacPaths.Initialize();
                if (args.Length >= 3 && args[0] == "--check-bulk") GlobalOptions.Instance.AutomaticUpdate = false;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                LanguageManager.Instance.Load(GlobalOptions.Instance.Language, null);
                if (LanguageManager.Instance.Loaded)
                {
                    using var main = new frmMain();
                    using var watchdog = AppDiagnostics.StartWatchdog();
                    if (args.Length >= 3 && args[0] == "--check-bulk")
                    {
                        main.Shown += async (_, _) => {
                            try { await NativeBulkChecks.RunAsync(main, args[1], args[2]); }
                            catch (Exception error) { AppDiagnostics.Record("bulk.native-checks-failed", error); Environment.ExitCode = 1; }
                            finally { NativeBulkChecks.DiscardEditors(); await AppPlatform.QuitApplicationAsync(); }
                        };
                    }
                    // Optional standalone PDF opening also provides a reproducible reader check
                    // without changing the user's sourcebook preferences.
                    if (args.Length >= 2 && args[0] == "--pdf")
                    {
                        int page = args.Length >= 3 && int.TryParse(args[2], out int requested) ? Math.Max(1, requested) : 1;
                        main.Shown += (_, _) => SourcebookReader.OpenDocument(
                            new SourcebookInfo { Code = "PDF", Path = args[1] }, page, main);
                    }
                    // Open an existing character directly in the output preview for support/reproduction.
                    if (args.Length >= 2 && args[0] == "--print-preview")
                    {
                        main.Shown += (_, _) => {
                            try {
                                var character = new Character { FileName = args[1] };
                                if (!character.Load()) throw new System.IO.InvalidDataException("Character could not be loaded.");
                                character.Print(false);
                            }
                            catch (Exception error) {
                                AppDiagnostics.Record("character-preview-open-failed", error);
                                AppErrors.Notify(main, "Unable to open the character print preview.", "Character output");
                            }
                        };
                    }
                    if (args.Length >= 3 && args[0] == "--export-foundry")
                    {
                        main.Shown += async (_, _) => {
                            try {
                                var character = new Character { FileName = args[1] };
                                if (!character.Load()) throw new System.IO.InvalidDataException("Character could not be loaded.");
                                var target = args.Length >= 4 && args[3] == "1.26" ? FoundryActorExporter.Target.Version126 : FoundryActorExporter.Target.Version16;
                                string json = FoundryActorExporter.Serialize(character, target);
                                await System.IO.File.WriteAllTextAsync(args[2], json, new System.Text.UTF8Encoding(false));
                                AppDiagnostics.Record("character-foundry-command-completed");
                            }
                            catch (Exception error) { AppDiagnostics.Record("character-foundry-command-failed", error); Environment.ExitCode = 1; }
                            finally { Application.Exit(); }
                        };
                    }
                    // Native PDF export can also be reproduced without a printer or preference changes.
                    if (args.Length >= 3 && args[0] == "--export-pdf")
                    {
                        main.Shown += async (_, _) => {
                            try {
                                var character = new Character { FileName = args[1] };
                                if (!character.Load()) throw new System.IO.InvalidDataException("Character could not be loaded.");
                                character.Print(false);
                                await character.PrintWindow.ExportPdfAsync(args[2]);
                                AppDiagnostics.Record("character-pdf-command-completed");
                            }
                            catch (Exception error) { AppDiagnostics.Record("character-pdf-command-failed", error); Environment.ExitCode = 1; }
                            finally { Application.Exit(); }
                        };
                    }
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
