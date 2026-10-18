#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Forms = Majorsilence.Forms;
namespace Chummer;
internal static class NativeExpenseChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static async Task WaitForAsync(Func<bool> ready)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!ready()) { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Expense confirmation did not open."); await Task.Delay(10); }
    }
    private static Task Reply(Forms.DialogResult answer)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
            try
            {
                await WaitForAsync(() => Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any());
                var message = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last(); message.DialogResult = answer; message.Close(); completion.TrySetResult();
            }
            catch (Exception error) { NativeDialogHost.CancelAll(); completion.TrySetException(error); }
        });
        return completion.Task;
    }
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool, string> check)
    {
        var date = new DateTime(2072, 3, 15, 17, 26, 43, DateTimeKind.Local).AddTicks(123);
        using (var prompt = new frmExpense { Mode = ExpenseType.Nuyen, Amount = 103, SelectedDate = date, Refund = true, strReason = "Synthetic expense" })
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (ExpenseWindow)NativeDialogHost.ActiveWindow!;
            check(window.AmountInput.Minimum == 1 && window.AmountInput.Maximum == 9999999 && window.PercentInput.Maximum == 1000 && window.TimeInput.IsVisible == GlobalOptions.Instance.DatesIncludeTime,
                "Native expense preserves integer percentage and time-visibility limits");
            window.PercentInput.Value = 50;
            check(window.PreviewText.Text == string.Format(GlobalOptions.Instance.CultureInfo, "{0:N0}¥", 52), "Native expense preview preserves midpoint-to-even percentage calculation");
            window.DateInput.SelectedDate = null; window.Accept();
            check(!pending.IsCompleted && !window.AcceptButton.IsEnabled, "Native expense incomplete date cannot accept or manufacture an entry");
            window.DateInput.SelectedDate = new DateTimeOffset(2072, 3, 16, 0, 0, 0, TimeSpan.Zero); window.TimeInput.SelectedTime = new TimeSpan(9, 30, 0);
            window.ReasonInput.Text = "Synthetic accepted expense";
            var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var previousTheme = nativeOwner.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    nativeOwner.RequestedThemeVariant = theme; await Task.Delay(100);
                    check(window.ActualThemeVariant == theme && window.AmountInput.Bounds.Width >= 110,
                        "Native expense editor follows " + (theme == ThemeVariant.Dark ? "dark" : "light") + " owner theme");
                    using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192));
                    bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-expense-editor-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { nativeOwner.RequestedThemeVariant = previousTheme; }
            window.Accept();
            var expectedTime = GlobalOptions.Instance.DatesIncludeTime ? new TimeSpan(9, 30, 43).Add(TimeSpan.FromTicks(123)) : date.TimeOfDay;
            check(await pending == Forms.DialogResult.OK && prompt.Amount == 52 && prompt.Refund && prompt.strReason == "Synthetic accepted expense" && prompt.SelectedDate == date.Date.AddDays(1).Add(expectedTime) && prompt.SelectedDate.Kind == date.Kind,
                "Native expense accepts calculated amount and preserves date kind hidden seconds and text");
        }
        using (var prompt = new frmExpense { Mode = ExpenseType.Nuyen, Amount = 103, SelectedDate = date, Refund = true, strReason = "Synthetic expense" })
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (ExpenseWindow)NativeDialogHost.ActiveWindow!;
            window.AmountInput.Value = 10; window.PercentInput.Value = 50; window.ReasonInput.Text = "Discarded draft"; window.RefundInput.IsChecked = false; window.Close();
            check(await pending == Forms.DialogResult.Cancel && prompt.Amount == 103 && prompt.Refund && prompt.strReason == "Synthetic expense" && prompt.SelectedDate == date,
                "Native expense cancellation discards all edited financial and text drafts");
        }
        using (var prompt = new frmExpense { Amount = -3, Refund = true, SelectedDate = date })
        {
            prompt.LockFields(); var pending = prompt.ShowDialogAsync(owner); var window = (ExpenseWindow)NativeDialogHost.ActiveWindow!;
            check(!window.AmountInput.IsEnabled && !window.PercentInput.IsEnabled && !window.RefundInput.IsEnabled && window.ReasonInput.IsEnabled && window.DateInput.IsEnabled,
                "Native automatic expense locks financial fields while retaining date and reason editing");
            window.AmountInput.Value = 2; window.PercentInput.Value = 500; window.RefundInput.IsChecked = false; window.ReasonInput.Text = "Synthetic renamed automatic expense"; window.Accept();
            check(await pending == Forms.DialogResult.OK && prompt.Amount == -3 && prompt.Refund && prompt.strReason == "Synthetic renamed automatic expense",
                "Native automatic expense ignores queued changes to disabled financial fields");
        }
        using (var prompt = new frmExpense { Amount = -3 })
        {
            prompt.LockFields(true); var pending = prompt.ShowDialogAsync(owner); var window = (ExpenseWindow)NativeDialogHost.ActiveWindow!;
            window.AmountInput.Value = -5; window.Accept();
            check(await pending == Forms.DialogResult.OK && window.AmountInput.Minimum == -9999999 && prompt.Amount == -5,
                "Native manual negative expense retains editable symmetric amount range");
        }
        var edge = new ExpenseWindow(new ExpenseSelection(1, 100, ExpenseType.Karma, "Synthetic date boundary", false, ExpenseDateTime.Maximum), new ExpenseEditSettings(1, false, false, true));
        var edgePending = NativeDialogHost.ShowAsync<ExpenseSelection>(edge, owner); edge.TimeInput.SelectedTime = TimeSpan.FromMinutes(1); edge.Accept();
        check(!edgePending.IsCompleted && !edge.AcceptButton.IsEnabled, "Native expense refuses a time beyond the legacy maximum-date midnight");
        edge.TimeInput.SelectedTime = TimeSpan.Zero; edge.Accept();
        check((await edgePending)?.Date == ExpenseDateTime.Maximum, "Native expense accepts the exact legacy maximum-date boundary");
        // Exercise the visible time editor independently of the user's date-only
        // preference; the domain preserves seconds, ticks and DateTime.Kind.
        var timed = new ExpenseWindow(new ExpenseSelection(1, 100, ExpenseType.Karma, "Synthetic timed expense", false, date), new ExpenseEditSettings(1, false, false, true));
        var timedPending = NativeDialogHost.ShowAsync<ExpenseSelection>(timed, owner);
        timed.TimeInput.SelectedTime = null; timed.Accept();
        check(timed.TimeInput.IsVisible && !timedPending.IsCompleted && !timed.AcceptButton.IsEnabled,
            "Native visible expense time requires an explicit complete time");
        timed.TimeInput.SelectedTime = new TimeSpan(9, 30, 0); timed.Accept();
        var timedSelection = await timedPending;
        check(timedSelection != null && timedSelection.Date == date.Date.AddHours(9).AddMinutes(30).AddSeconds(43).AddTicks(123) && timedSelection.Date.Kind == date.Kind,
            "Native visible expense time editing retains original hidden seconds ticks and kind");
        using (var incomplete = new frmExpense { Mode = ExpenseType.Nuyen, Amount = 103 })
        {
            var pending = incomplete.ShowDialogAsync(owner); var window = (ExpenseWindow)NativeDialogHost.ActiveWindow!;
            window.AmountInput.Value = null; window.Accept(); await Task.Delay(30);
            check(!pending.IsCompleted && !window.AcceptButton.IsEnabled, "Native expense rejects a cleared amount instead of accepting a fallback charge");
            window.AmountInput.Value = 103; window.PercentInput.Value = null; window.Accept(); await Task.Delay(30);
            bool rejected = !pending.IsCompleted && !window.AcceptButton.IsEnabled;
            window.Close();
            check(rejected && await pending == Forms.DialogResult.Cancel && incomplete.Amount == 103,
                "Native expense rejects a cleared percentage and preserves the unaccepted draft");
        }
        await CheckCareerAsync(owner, original, check);
    }
    private static async Task CheckCareerAsync(Forms.Form owner, Character original, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = true; character.Karma = 100; character.Nuyen = 1000;
        using var editor = new frmCareer(character) { Loading = true, MdiParent = owner }; editor.Show();
        void Invoke(string command) => editor.GetType().GetMethod(command, Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
        void Expense(string command, bool accept, int amount, int percent = 100, bool refund = false)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                var window = (ExpenseWindow)NativeDialogHost.ActiveWindow!; window.AmountInput.Value = amount; window.PercentInput.Value = percent; window.RefundInput.IsChecked = refund;
                window.ReasonInput.Text = "Synthetic career expense"; window.DateInput.SelectedDate = new DateTimeOffset(2072, 3, 15, 0, 0, 0, TimeSpan.Zero);
                if (accept) window.Accept(); else window.Close();
            });
            Invoke(command);
        }
        void SelectExpense(string list, ExpenseLogEntry expense)
        {
            editor.PopulateExpenseList(); var rows = Field<Forms.ListView>(editor, list).Items.Cast<Forms.ListViewItem>().ToArray();
            foreach (var row in rows) row.Selected = false;
            rows.Single(row => row.SubItems[3].Text == expense.InternalId).Selected = true;
        }
        try
        {
            int count = character.ExpenseEntries.Count;
            foreach (string command in new[] { "cmdKarmaGained_Click", "cmdKarmaSpent_Click", "cmdNuyenGained_Click", "cmdNuyenSpent_Click" }) Expense(command, false, 5);
            check(character.Karma == 100 && character.Nuyen == 1000 && character.ExpenseEntries.Count == count,
                "Native career gain and spend cancellation preserves both balances and ledger");
            Expense("cmdKarmaGained_Click", true, 10, refund: true);
            check(character.Karma == 110 && character.ExpenseEntries.Last().Amount == 10 && character.ExpenseEntries.Last().Refund,
                "Native Karma gain preserves accepted amount and refund ledger semantics");
            Expense("cmdKarmaSpent_Click", true, 7); var karmaExpense = character.ExpenseEntries.Last();
            check(character.Karma == 103 && karmaExpense.Amount == -7 && karmaExpense.Undo.KarmaType == KarmaExpenseType.ManualSubtract,
                "Native Karma spend records the existing signed expense and undo kind");
            Expense("cmdNuyenGained_Click", true, 103, 50, true); var nuyenGain = character.ExpenseEntries.Last();
            check(character.Nuyen == 1052 && nuyenGain.Amount == 52 && nuyenGain.Refund && nuyenGain.Date.Date == new DateTime(2072, 3, 15),
                "Native Nuyen gain applies rounded percentage once with accepted date and refund");
            Expense("cmdNuyenSpent_Click", true, 101, 50);
            check(character.Nuyen == 1002 && character.ExpenseEntries.Last().Amount == -50 && character.ExpenseEntries.Count == count + 4,
                "Native Nuyen spend applies midpoint rounding without duplicate ledger entries");
            var notice = Reply(Forms.DialogResult.OK); Expense("cmdKarmaSpent_Click", true, 9999999); await notice;
            check(character.Karma == 103 && character.ExpenseEntries.Count == count + 4, "Native unaffordable Karma expense preserves balance and ledger");
            notice = Reply(Forms.DialogResult.OK); Expense("cmdNuyenSpent_Click", true, 9999999); await notice;
            check(character.Nuyen == 1002 && character.ExpenseEntries.Count == count + 4, "Native unaffordable Nuyen expense preserves balance and ledger");
            SelectExpense("lstKarma", karmaExpense); Expense("cmdKarmaEdit_Click", false, -9);
            check(character.Karma == 103 && karmaExpense.Amount == -7, "Native canceled manual Karma edit preserves balance and original entry");
            SelectExpense("lstKarma", karmaExpense); Expense("cmdKarmaEdit_Click", true, -9);
            check(character.Karma == 101 && karmaExpense.Amount == -9 && character.ExpenseEntries.Count == count + 4,
                "Native accepted manual Karma edit adjusts only the amount difference");
            SelectExpense("lstNuyen", nuyenGain); Expense("cmdNuyenEdit_Click", true, 60);
            check(character.Nuyen == 1010 && nuyenGain.Amount == 60 && nuyenGain.Refund && character.ExpenseEntries.Count == count + 4,
                "Native accepted manual Nuyen edit retains refund and adjusts balance once");
            SelectExpense("lstNuyen", nuyenGain); var answer = Reply(Forms.DialogResult.Cancel); Invoke("tsUndoNuyenExpense_Click"); await answer;
            check(character.Nuyen == 1010 && character.ExpenseEntries.Contains(nuyenGain), "Native manual expense undo cancellation preserves the edited amount and balance");
            SelectExpense("lstNuyen", nuyenGain); answer = Reply(Forms.DialogResult.Yes); Invoke("tsUndoNuyenExpense_Click"); await answer;
            check(character.Nuyen == 950 && !character.ExpenseEntries.Contains(nuyenGain) && character.ExpenseEntries.Count == count + 3,
                "Native manual Nuyen expense undo reverses the current edited amount exactly once");
            SelectExpense("lstKarma", karmaExpense); answer = Reply(Forms.DialogResult.Cancel); Invoke("tsUndoKarmaExpense_Click"); await answer;
            check(character.Karma == 101 && character.ExpenseEntries.Contains(karmaExpense), "Native manual Karma undo cancellation preserves edited amount and balance");
            SelectExpense("lstKarma", karmaExpense); answer = Reply(Forms.DialogResult.Yes); Invoke("tsUndoKarmaExpense_Click"); await answer;
            check(character.Karma == 110 && !character.ExpenseEntries.Contains(karmaExpense) && character.ExpenseEntries.Count == count + 2,
                "Native manual Karma expense undo reverses the edited signed amount once");
        }
        finally { editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close(); }
    }
}
