#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public sealed class frmSelectSide : IDisposable
{
    private SidePickerWindow? window;

    public string Description { private get; set; } = "";
    public string SelectedSide { get; private set; } = "";
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;

    public void ForceValue(string side)
    {
        SelectedSide = side;
        DialogResult = Forms.DialogResult.OK;
    }

    public Forms.DialogResult ShowDialog()
    {
        var owner = Forms.Form.ActiveForm ?? Forms.Application.OpenForms.Cast<Forms.Form>().LastOrDefault(form => form.Visible)
            ?? throw new InvalidOperationException("A side picker requires an open owner window.");
        var pending = ShowDialogAsync(owner);
        Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }

    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null)
            throw new InvalidOperationException("The side picker is already open.");

        SelectedSide = "";
        DialogResult = Forms.DialogResult.Cancel;
        window = new SidePickerWindow(Description);
        try
        {
            var selected = await NativeDialogHost.ShowAsync<string>(window, owner);
            if (selected != null)
                ForceValue(selected);
            return DialogResult;
        }
        finally
        {
            window = null;
        }
    }

    public void Dispose() => window?.Close();
}

internal sealed class SidePickerWindow : Window
{
    internal ComboBox Sides { get; }
    internal Button AcceptButton { get; }
    internal Button CancelButton { get; }

    internal SidePickerWindow(string description)
    {
        var language = LanguageManager.Instance;
        Title = language.GetString("Title_SelectSide");
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Width = 360;
        MinWidth = 250;
        MinHeight = 160;
        MaxHeight = 500;
        SizeToContent = SizeToContent.Height;
        NativeControlPresentation.AddTheme(Styles);

        Sides = new ComboBox
        {
            ItemsSource = new[]
            {
                language.GetString("String_Improvement_SideLeft"),
                language.GetString("String_Improvement_SideRight")
            },
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(Sides, Title);

        AcceptButton = new Button { Content = language.GetString("String_OK"), IsDefault = true };
        AcceptButton.Click += (_, _) =>
        {
            if (Sides.SelectedItem is string selected)
                Close(selected);
        };
        CancelButton = new Button { Content = language.GetString("String_Cancel"), IsCancel = true };
        CancelButton.Click += (_, _) => Close();

        var prompt = new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap };
        Content = NativeControlPresentation.CreatePromptLayout(
            new StackPanel { Spacing = 10, Children = { prompt, Sides } }, AcceptButton, CancelButton);
    }
}
