using System;
using Majorsilence.Forms;

namespace Chummer;

internal static class NativeControlObservationChecks
{
    internal static void Run(Action<bool, string> check)
    {
        using var owner = new Form();
        using var source = new TextBox();
        owner.Controls.Add(source);
        // Invalidate is deliberately a no-op until a controller is created.
        // Exercise the real event path on a shown headless owner.
        owner.Show();
        int refreshes = 0;
        using var observation = new NativeControlObservation(source, () => refreshes++);
        check(refreshes == 0, "Native observation leaves initial synchronization to its adapter");
        source.Enabled = false;
        check(refreshes > 0, "Native observation forwards controller availability changes");
        int before = refreshes;
        source.Visible = !source.Visible;
        check(refreshes > before, "Native observation forwards controller visibility changes");
        before = refreshes;
        source.Invalidate();
        check(refreshes > before, "Native observation forwards controller invalidation");

        // A second owner must survive the first owner's repeated cleanup. This
        // catches accidentally removing another observer or disposing the source.
        int otherRefreshes = 0;
        using var other = new NativeControlObservation(source, () => otherRefreshes++);
        observation.Dispose(); observation.Dispose();
        before = refreshes;
        source.Enabled = true;
        source.Visible = !source.Visible;
        source.Invalidate();
        check(refreshes == before && otherRefreshes > 0 && !source.IsDisposed,
            "Disposed native observation detaches only its own callbacks and preserves its source");
        other.Dispose();
        before = otherRefreshes;
        source.Invalidate();
        check(otherRefreshes == before, "Final native observation releases its invalidation callback");
        RunAdapters(check);
    }

    private static void RunAdapters(Action<bool, string> check)
    {
        using var text = new TextBox { Text = "Original" };
        using var choice = new ComboBox { Text = "Custom" };
        using var number = new NumericUpDown { Maximum = 10, Value = 2 };
        using var command = new Button { Text = "Run" };
        using var tips = new ToolTip();
        int executions = 0;
        using var textBinding = new NativeInlineTextBinding(text, () => true);
        using var choiceBinding = new NativeInlineChoiceBinding(choice);
        using var numberBinding = new NativeInlineNumericBinding(number, tips, true);
        using var commandBinding = new NativeInlineCommandBinding(command, tips, () => true, () => executions++);

        text.Enabled = choice.Enabled = number.Enabled = command.Enabled = false;
        check(!textBinding.Input.IsEnabled && !choiceBinding.Input.IsEnabled &&
            !numberBinding.Input.IsEnabled && !commandBinding.Input.IsEnabled,
            "All four native adapters share controller availability observation");
        text.Visible = choice.Visible = number.Visible = command.Visible = false;
        check(!textBinding.Input.IsVisible && !choiceBinding.Input.IsVisible &&
            !numberBinding.Input.IsVisible && !commandBinding.Input.IsVisible,
            "All four native adapters share controller visibility observation");

        textBinding.Dispose(); choiceBinding.Dispose(); numberBinding.Dispose(); commandBinding.Dispose();
        text.Enabled = choice.Enabled = number.Enabled = command.Enabled = true;
        text.Visible = choice.Visible = number.Visible = command.Visible = true;
        text.Text = "Controller text"; choice.Text = "Controller choice"; number.Value = 4; command.Text = "Changed";
        check(!textBinding.Input.IsEnabled && !choiceBinding.Input.IsEnabled &&
            !numberBinding.Input.IsEnabled && !commandBinding.Input.IsEnabled &&
            textBinding.Input.Text == "Original" && choiceBinding.Input.Text == "Custom" &&
            numberBinding.Input.Value == 2 && (string)commandBinding.Input.Content == "Run",
            "Disposed native adapters detach presentation and value subscriptions");

        textBinding.Input.Text = "Queued text"; choiceBinding.Input.Text = "Queued choice"; numberBinding.Input.Value = 6;
        commandBinding.Input.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        check(text.Text == "Controller text" && choice.Text == "Controller choice" && number.Value == 4 && executions == 0,
            "Disposed native adapters reject queued edits and commands");
    }
}
