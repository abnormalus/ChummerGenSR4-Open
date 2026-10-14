using System;
using System.Threading;
using Majorsilence.Forms;

namespace Chummer
{
    internal static class AppErrors
    {
        private static bool reporting;
        internal static Action<Form, string, string> Notify = (owner, text, title) =>
            MessageBox.Show(owner, text, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        internal static void Inform(Form owner, string key)
        {
            AppDiagnostics.Record(key);
            Notify(owner, LanguageManager.Instance.GetString(key), "Chummer");
        }

        internal static void OnThreadException(object sender, ThreadExceptionEventArgs args)
        {
            AppDiagnostics.Record("recoverable-command-error", args.Exception);
            if (reporting) return;
            reporting = true;
            try { Notify(GlobalOptions.Instance.MainForm,
                LanguageManager.Instance.GetString("Message_CommandFailed"), "Chummer"); }
            catch (Exception error) { AppDiagnostics.Record("recovery-notice-failed", error); }
            finally { reporting = false; }
        }
    }
}
