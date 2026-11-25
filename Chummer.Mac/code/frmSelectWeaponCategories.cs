#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public sealed class frmSelectWeaponCategories : IDisposable
{
    private readonly (string Name, string Value, bool Selected)[] categories;
    private WeaponCategoriesWindow? window;

    public frmSelectWeaponCategories(IEnumerable<ListItem> categories, ICollection<string> selectedCategories)
    {
        this.categories = categories.Select(item => (item.Name, item.Value, selectedCategories.Contains(item.Value))).ToArray();
    }

    public IEnumerable<string> SelectedCategories { get; private set; } = [];

    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner);
        Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }

    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null)
            throw new InvalidOperationException("Weapon category selection is already open.");

        SelectedCategories = [];
        window = new WeaponCategoriesWindow(categories);
        try
        {
            var result = await NativeDialogHost.ShowAsync<string[]>(window, owner);
            SelectedCategories = result ?? [];
            return result == null ? Forms.DialogResult.Cancel : Forms.DialogResult.OK;
        }
        finally
        {
            window = null;
        }
    }

    public void Dispose() => window?.Close();
}

internal sealed class WeaponCategoriesWindow : Window
{
    internal StackPanel Options { get; } = new() { Spacing = 4 };
    internal Button AcceptButton { get; }
    internal Button CancelButton { get; }

    internal WeaponCategoriesWindow(IEnumerable<(string Name, string Value, bool Selected)> categories)
    {
        var language = LanguageManager.Instance;
        Title = language.GetString("Title_SelectWeaponCategories");
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Width = 380;
        Height = 440;
        MinWidth = 250;
        MinHeight = 200;
        NativeControlPresentation.AddTheme(Styles);

        foreach (var (name, value, selected) in categories)
        {
            Options.Children.Add(new CheckBox
            {
                Tag = value,
                IsChecked = selected,
                Content = new TextBlock { Text = name, TextWrapping = TextWrapping.Wrap }
            });
        }

        AcceptButton = new Button { Content = language.GetString("String_OK"), IsDefault = true };
        AcceptButton.Click += (_, _) => Close(Options.Children.OfType<CheckBox>()
            .Where(option => option.IsChecked == true).Select(option => (string)option.Tag!).ToArray());
        CancelButton = new Button { Content = language.GetString("String_Cancel"), IsCancel = true };
        CancelButton.Click += (_, _) => Close();

        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        actions.Children.Add(AcceptButton);
        actions.Children.Add(CancelButton);
        var list = new ScrollViewer
        {
            Content = Options,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            AllowAutoHide = false
        };
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 10 };
        layout.Children.Add(list);
        Grid.SetRow(actions, 1);
        layout.Children.Add(actions);
        Content = layout;
    }
}
