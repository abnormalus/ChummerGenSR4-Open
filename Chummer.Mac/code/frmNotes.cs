#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public sealed class frmNotes : IDisposable
{
    private NotesWindow? window;

    public string Notes { get; set; } = "";
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.None;
    public event EventHandler? Shown;

    public Forms.DialogResult ShowDialog(Forms.IWin32Window owner)
    {
        var form = (owner as Forms.Form) ?? (owner as Forms.Control)?.FindForm()
            ?? Forms.Form.ActiveForm ?? Forms.Application.OpenForms.Cast<Forms.Form>().LastOrDefault(candidate => candidate.Visible)
            ?? throw new InvalidOperationException("The notes editor requires an open owner window.");
        var pending = ShowDialogAsync(form);
        Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }

    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null)
            throw new InvalidOperationException("The notes editor is already open.");

        DialogResult = Forms.DialogResult.None;
        var editor = new NotesWindow(Notes);
        window = editor;
        editor.Opened += NotifyShown;
        try
        {
            await NativeDialogHost.ShowAsync<object>(editor, owner);
            Notes = editor.Input.Text ?? "";
            return DialogResult = Forms.DialogResult.OK;
        }
        finally
        {
            editor.Opened -= NotifyShown;
            window = null;
        }
    }

    private void NotifyShown(object? sender, EventArgs args) => Shown?.Invoke(this, EventArgs.Empty);

    public void Dispose() => window?.Close();
}

internal sealed class NotesWindow : Window
{
    private static Size lastSize = new(534, 278);

    internal TextBox Input { get; }

    internal NotesWindow(string notes)
    {
        Title = LanguageManager.Instance.GetString("Title_Notes");
        Width = lastSize.Width;
        Height = lastSize.Height;
        MinWidth = 250;
        MinHeight = 150;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);

        Input = new TextBox
        {
            Name = "NotesInput",
            Text = notes,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        AutomationProperties.SetName(Input, Title);
        ScrollViewer.SetHorizontalScrollBarVisibility(Input, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(Input, ScrollBarVisibility.Auto);
        Content = new Border { Padding = new Thickness(8), Child = Input };
        Opened += (_, _) =>
        {
            Input.Focus();
            Input.CaretIndex = notes.Length;
        };
        Closing += (_, _) =>
        {
            if (WindowState == WindowState.Normal && ClientSize.Width >= MinWidth && ClientSize.Height >= MinHeight)
                lastSize = ClientSize;
        };
        AddHandler(KeyDownEvent, (_, args) =>
        {
            if (args.Key != Key.Escape)
                return;
            args.Handled = true;
            Close();
        }, RoutingStrategies.Tunnel);
    }
}
