#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public sealed class frmReload : IDisposable
{
    private ReloadWindow? window;

    public List<Gear> Ammo { private get; set; } = [];
    public List<string> Count { private get; set; } = [];
    public string SelectedAmmo { get; private set; } = "";
    public int SelectedCount { get; private set; }
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;

    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner);
        if (!pending.IsCompleted)
            Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }

    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null)
            throw new InvalidOperationException("The reload picker is already open.");

        SelectedAmmo = "";
        SelectedCount = 0;
        DialogResult = Forms.DialogResult.Cancel;
        if (Ammo.Count == 0 || Count.Count == 0)
            return DialogResult;
        if (Ammo.Count == 1 && Count.Count == 1)
            return Accept(Ammo[0].InternalId, Count[0]);

        window = new ReloadWindow(Ammo.Select(CreateChoice).ToArray(), Count.ToArray());
        try
        {
            var selection = await NativeDialogHost.ShowAsync<ReloadSelection>(window, owner);
            return selection == null ? DialogResult : Accept(selection.AmmoId, selection.Count);
        }
        finally { window = null; }
    }

    private Forms.DialogResult Accept(string ammo, string count)
    {
        if (!int.TryParse(count, NumberStyles.Integer, CultureInfo.CurrentCulture, out int rounds))
            return Forms.DialogResult.Cancel;
        SelectedAmmo = ammo;
        SelectedCount = rounds;
        return DialogResult = Forms.DialogResult.OK;
    }

    private static ListItem CreateChoice(Gear gear)
    {
        string label = gear.DisplayNameShort + " x" + gear.Quantity;
        if (gear.Parent is { } parent && parent.DisplayNameShort.Length > 0)
        {
            string location = parent.Location.Length == 0 ? "" : " @ " + parent.Location;
            label += " (" + parent.DisplayNameShort + location + ")";
        }
        if (gear.Location.Length > 0)
            label += " (" + gear.Location + ")";
        if (gear.Children.Count > 0)
            label += " [" + string.Join(", ", gear.Children.Select(child => child.DisplayNameShort)) + "]";
        return new ListItem { Value = gear.InternalId, Name = label };
    }

    public void Dispose() => window?.Close();
}

internal sealed record ReloadSelection(string AmmoId, string Count);

internal sealed class ReloadWindow : Window
{
    internal ComboBox AmmoInput { get; }
    internal ComboBox CountInput { get; }
    internal Button AcceptButton { get; }
    internal Button CancelButton { get; }

    internal ReloadWindow(ListItem[] ammo, string[] counts)
    {
        var language = LanguageManager.Instance;
        Title = language.GetString("Title_Reload");
        Width = 480;
        MinWidth = 260;
        MinHeight = 175;
        MaxHeight = 500;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        NativeControlPresentation.AddTheme(Styles);

        AmmoInput = new ComboBox
        {
            ItemsSource = ammo,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemTemplate = new FuncDataTemplate<ListItem>((choice, _) =>
                new TextBlock { Text = choice?.Name ?? "", TextWrapping = TextWrapping.Wrap })
        };
        Avalonia.Controls.Primitives.Popup? ammoPopup = null;
        AmmoInput.TemplateApplied += (_, args) =>
            ammoPopup = args.NameScope.Find<Avalonia.Controls.Primitives.Popup>("PART_Popup");
        AmmoInput.DropDownOpened += (_, _) =>
        {
            if (ammoPopup?.Child is { } content && AmmoInput.Bounds.Width > 0)
            {
                content.MinWidth = AmmoInput.Bounds.Width;
                content.MaxWidth = AmmoInput.Bounds.Width;
            }
        };
        CountInput = new ComboBox
        {
            ItemsSource = counts,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        AcceptButton = new Button { Content = language.GetString("String_OK"), IsDefault = true };
        AcceptButton.Click += (_, _) =>
        {
            if (AmmoInput.SelectedItem is ListItem selected && CountInput.SelectedItem is string count)
                Close(new ReloadSelection(selected.Value, count));
        };
        CancelButton = new Button { Content = language.GetString("String_Cancel"), IsCancel = true };
        CancelButton.Click += (_, _) => Close();

        var fields = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                Field(language.GetString("Label_Ammo"), AmmoInput),
                Field(language.GetString("Label_Qty"), CountInput)
            }
        };
        Content = NativeControlPresentation.CreatePromptLayout(fields, AcceptButton, CancelButton);
    }

    private static StackPanel Field(string caption, ComboBox input)
    {
        AutomationProperties.SetName(input, caption);
        return new StackPanel
        {
            Spacing = 4,
            Children = { new TextBlock { Text = caption, TextWrapping = TextWrapping.Wrap }, input }
        };
    }
}
