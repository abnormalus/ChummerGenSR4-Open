#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal sealed class UpdateWindow : Window
{
    internal TextBlock Status { get; }
    internal TextBox ReleaseNotes { get; }
    internal Button Download { get; }
    internal Button CloseButton { get; }

    internal static async Task CheckSilentlyAsync(Forms.Form owner)
    {
        using var request = new CancellationTokenSource();
        Forms.FormClosingEventHandler closing = (_, args) => { if (!args.Cancel) request.Cancel(); };
        owner.FormClosing += closing;
        try
        {
            var release = await ReleaseUpdates.CheckAsync(request.Token);
            if (release.Newer && !request.IsCancellationRequested && !owner.IsDisposed)
                await ShowAsync(owner, _ => Task.FromResult(release));
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception error) { AppDiagnostics.Record("automatic-update-check-failed", error); }
        finally { owner.FormClosing -= closing; }
    }

    internal static async Task ShowAsync(Forms.Form owner, Func<CancellationToken, Task<ReleaseInfo>>? check = null)
    {
        var window = new UpdateWindow(owner, check ?? (token => ReleaseUpdates.CheckAsync(token)));
        await NativeDialogHost.ShowAsync<object>(window, owner);
    }

    private UpdateWindow(Forms.Form owner, Func<CancellationToken, Task<ReleaseInfo>> check)
    {
        var language = LanguageManager.Instance;
        Title = language.GetString("Title_Update");
        Width = 600;
        Height = 440;
        MinWidth = 280;
        MinHeight = 230;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);

        var current = new TextBlock
        {
            Text = language.GetString("Label_Update_CurrentVersion") + " " + Forms.Application.ProductVersion,
            TextWrapping = TextWrapping.Wrap
        };
        Status = new TextBlock { Text = "Checking Nightly releases…", TextWrapping = TextWrapping.Wrap };
        ReleaseNotes = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalContentAlignment = VerticalAlignment.Top
        };
        Avalonia.Automation.AutomationProperties.SetName(ReleaseNotes, "Release notes");
        ScrollViewer.SetVerticalScrollBarVisibility(ReleaseNotes, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(ReleaseNotes, ScrollBarVisibility.Disabled);
        Download = new Button { Content = language.GetString("Button_Update_Download"), IsEnabled = false };
        Download.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(ReleaseUpdates.ReleasePage) { UseShellExecute = true }); }
            catch (Exception error)
            {
                AppDiagnostics.Record("update-page-open-failed", error);
                AppErrors.Notify(owner, "Unable to open the release page.", "Chummer updates");
            }
        };
        CloseButton = new Button { Content = language.GetString("Button_Update_Close"), IsCancel = true, IsDefault = true };
        CloseButton.Click += (_, _) => Close();

        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        buttons.Children.Add(Download);
        buttons.Children.Add(CloseButton);
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"), RowSpacing = 8 };
        layout.Children.Add(current);
        Grid.SetRow(Status, 1);
        layout.Children.Add(Status);
        Grid.SetRow(ReleaseNotes, 2);
        layout.Children.Add(ReleaseNotes);
        Grid.SetRow(buttons, 3);
        layout.Children.Add(buttons);
        Content = layout;
        Opened += async (_, _) => await LoadReleaseAsync(check);
    }

    private async Task LoadReleaseAsync(Func<CancellationToken, Task<ReleaseInfo>> check)
    {
        using var request = new CancellationTokenSource();
        void Cancel(object? sender, EventArgs args) => request.Cancel();
        Closed += Cancel;
        try
        {
            var release = await check(request.Token);
            if (request.IsCancellationRequested)
                return;

            Status.Text = release.Newer ? "A newer Nightly build is available." : "Latest Nightly release";
            ReleaseNotes.Text = release.Notes;
            Download.IsEnabled = true;
            AppDiagnostics.Record("update-check-completed");
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception error)
        {
            AppDiagnostics.Record("update-check-failed", error);
            if (!request.IsCancellationRequested)
            {
                Status.Text = "Unable to check for updates. You can still open the release page.";
                Download.IsEnabled = true;
            }
        }
        finally { Closed -= Cancel; }
    }
}
