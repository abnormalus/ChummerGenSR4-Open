#nullable enable
using System;
using System.IO;
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

internal sealed class RevisionHistoryWindow : Window
{
    internal static string HistoryPath => Path.Combine(AppContext.BaseDirectory, "changelog.txt");
    internal TextBox History { get; }

    internal static async Task ShowAsync(Forms.Form owner)
    {
        string history;
        try { history = await File.ReadAllTextAsync(HistoryPath); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            var language = LanguageManager.Instance;
            Forms.MessageBox.Show(owner, language.GetString("Message_History_FileNotFound"),
                language.GetString("MessageTitle_FileNotFound"), Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Exclamation);
            return;
        }

        if (!owner.IsDisposed)
            await NativeDialogHost.ShowAsync<object>(new RevisionHistoryWindow(history), owner);
    }

    private RevisionHistoryWindow(string history)
    {
        Title = LanguageManager.Instance.GetString("Title_History");
        Width = 784;
        Height = 462;
        MinWidth = 280;
        MinHeight = 190;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);
        History = new TextBox
        {
            Text = history,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Top
        };
        AutomationProperties.SetName(History, Title);
        ScrollViewer.SetHorizontalScrollBarVisibility(History, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(History, ScrollBarVisibility.Auto);
        Content = new Border { Padding = new Thickness(12), Child = History };
        AddHandler(KeyDownEvent, (_, args) =>
        {
            if (args.Key != Key.Escape)
                return;
            args.Handled = true;
            Close();
        }, RoutingStrategies.Tunnel);
    }
}
