using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chummer
{
    internal static class AppDiagnostics
    {
        private static readonly object Gate = new object();
        private static readonly string Session = Guid.NewGuid().ToString("N");
        private static long lastUiResponse = Environment.TickCount64;
        private static int openFormCount;
        private static string modalType;
        private static object interactionState;
        internal static void CaptureUiState()
        {
            var main = Majorsilence.Forms.Application.OpenForms.OfType<frmMain>().FirstOrDefault();
            var editor = main?.ActiveMdiChild;
            bool? nativeMainEnabled = null;
            // Headless checks have no native window. Keep the two enabled states
            // separate: native modal ownership can block input while Form.Enabled is true.
            if (main != null)
                try { nativeMainEnabled = Majorsilence.Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled; }
                catch (InvalidOperationException) { }
            // Capture only structural state on the UI thread. Never record window titles,
            // field contents, character identities, or the text of focused controls.
            interactionState = new {
                mainEnabled = main?.Enabled, nativeMainEnabled, mainContainsFocus = main?.ContainsFocus,
                editorType = editor?.GetType().Name, editorEnabled = editor?.Enabled,
                editorContainsFocus = editor?.ContainsFocus, nativeDialogCount = NativeDialogHost.Count
            };
            openFormCount = Majorsilence.Forms.Application.OpenForms.Count;
            modalType = Majorsilence.Forms.Application.OpenForms.Cast<Majorsilence.Forms.Form>().LastOrDefault(form => form.Modal)?.GetType().Name ?? NativeDialogHost.ActiveDialogType;
        }
        internal static IDisposable StartWatchdog()
        {
            long lastTick = Environment.TickCount64;
            int queued = 0;
            bool stalled = false;
            CaptureUiState();
            System.Threading.Interlocked.Exchange(ref lastUiResponse, lastTick);
            return new System.Threading.Timer(_ =>
            {
                long now = Environment.TickCount64;
                // A long gap in the worker timer indicates sleep/suspension, not an observed UI hang.
                if (now - lastTick > 15000)
                    System.Threading.Interlocked.Exchange(ref lastUiResponse, now);
                lastTick = now;
                long delay = now - System.Threading.Interlocked.Read(ref lastUiResponse);
                if (delay >= 15000 && !stalled) { stalled = true; Record("ui-unresponsive"); }
                else if (delay < 15000 && stalled) { stalled = false; Record("ui-responsive-again"); }
                if (System.Threading.Interlocked.CompareExchange(ref queued, 1, 0) != 0) return;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    CaptureUiState();
                    System.Threading.Interlocked.Exchange(ref lastUiResponse, Environment.TickCount64);
                    System.Threading.Interlocked.Exchange(ref queued, 0);
                });
            }, null, 2000, 2000);
        }
        internal static string CurrentAction { get; private set; } = "startup";
        internal static string DirectoryPath => Path.Combine(MacPaths.UserDirectory, "logs");
        internal static string LogPath => Path.Combine(DirectoryPath, "diagnostics.jsonl");
        internal static void Initialize()
        {
            Majorsilence.Forms.Application.ThreadException += AppErrors.OnThreadException;
            Majorsilence.Forms.MenuItem.CommandStarting += item => Action(
                item is Majorsilence.Forms.ToolStripItem command && !string.IsNullOrEmpty(command.Name)
                    ? "menu." + command.Name : "menu.command");
            AppDomain.CurrentDomain.UnhandledException += (_, args) => Record("fatal", args.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (_, args) => Record("unobserved-task", args.Exception);
            Record("session-start");
        }
        internal static void Action(string id)
        {
            // Callers supply fixed command IDs, never user-entered text or paths.
            CurrentAction = id;
            CaptureUiState();
            Record("action");
        }
        internal static void Record(string kind, Exception error = null)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(DirectoryPath);
                    if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 2 * 1024 * 1024)
                    {
                        for (int index = 3; index >= 1; index--)
                        {
                            string previous = index == 1 ? LogPath : LogPath + "." + (index - 1);
                            if (File.Exists(previous)) File.Move(previous, LogPath + "." + index, true);
                        }
                    }
                    var exceptions = error == null ? null : Flatten(error).Select(item => new {
                        type = item.GetType().FullName, code = item.HResult,
                        nativeRenderTimerCode = NativeRenderTimerCode(item),
                        stack = new StackTrace(item, false).ToString()
                    }).ToArray();
                    File.AppendAllText(LogPath, JsonSerializer.Serialize(new {
                        utc = DateTimeOffset.UtcNow, kind, action = CurrentAction,
                        session = Session, processId = Environment.ProcessId, openFormCount, modalType, interactionState,
                        uiResponseAgeMs = Math.Max(0, Environment.TickCount64 - System.Threading.Interlocked.Read(ref lastUiResponse)),
                        version = typeof(Program).Assembly.GetName().Version?.ToString(),
                        build = typeof(Program).Module.ModuleVersionId.ToString(),
                        runtime = Environment.Version.ToString(), platform = Environment.OSVersion.Platform.ToString(),
                        osVersion = Environment.OSVersion.VersionString, architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                        fullRedraw = Majorsilence.Forms.RenderingDiagnostics.ForceFullRedraw,
                        exceptions
                    }) + Environment.NewLine);
                }
            }
            catch (Exception) { /* Logging must never prevent launching, saving, or closing. */ }
        }
        // Capture only this known upstream numeric diagnostic, never arbitrary exception messages.
        internal static int? NativeRenderTimerCode(Exception error)
        {
            const string prefix = "Avalonia.Native was not able to start the RenderTimer. Native error code is: ";
            if (error is InvalidOperationException && error.Message.StartsWith(prefix, StringComparison.Ordinal) &&
                int.TryParse(error.Message.Substring(prefix.Length), System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture, out int code)) return code;
            return null;
        }

        private static System.Collections.Generic.IEnumerable<Exception> Flatten(Exception error)
        {
            yield return error;
            if (error is AggregateException aggregate)
                foreach (var inner in aggregate.InnerExceptions)
                    foreach (var item in Flatten(inner)) yield return item;
            else if (error.InnerException != null)
                foreach (var item in Flatten(error.InnerException)) yield return item;
        }
    }
}
