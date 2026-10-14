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
        internal static string CurrentAction { get; private set; } = "startup";
        internal static string DirectoryPath => Path.Combine(MacPaths.UserDirectory, "logs");
        internal static string LogPath => Path.Combine(DirectoryPath, "diagnostics.jsonl");
        internal static void Initialize()
        {
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
                        stack = new StackTrace(item, false).ToString()
                    }).ToArray();
                    File.AppendAllText(LogPath, JsonSerializer.Serialize(new {
                        utc = DateTimeOffset.UtcNow, kind, action = CurrentAction,
                        version = typeof(Program).Assembly.GetName().Version?.ToString(),
                        build = typeof(Program).Module.ModuleVersionId.ToString(),
                        runtime = Environment.Version.ToString(), platform = Environment.OSVersion.Platform.ToString(),
                        osVersion = Environment.OSVersion.VersionString, architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                        exceptions
                    }) + Environment.NewLine);
                }
            }
            catch (Exception) { /* Logging must never prevent launching, saving, or closing. */ }
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
