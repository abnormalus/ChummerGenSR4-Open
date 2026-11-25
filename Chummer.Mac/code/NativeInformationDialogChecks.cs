#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeInformationDialogChecks
{
    internal static async Task RunAsync(frmMain main, Action<bool, string> check)
    {
        using (var progress = new BulkProgressDialog())
        {
            var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = progress.RunAsync<object>(main, 4, async work =>
            {
                await work.StepAsync();
                waiting.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, work.Cancellation);
                return new object();
            });
            try
            {
                await waiting.Task.WaitAsync(TimeSpan.FromSeconds(10));
                progress.ClientSize = new System.Drawing.Size(260, 145);
                var status = (Forms.Label)typeof(BulkProgressDialog).GetField("status",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(progress)!;
                status.Text = string.Join(" ", Enumerable.Repeat("Long translated PACKS progress status", 20));
                await Idle();
                var surface = (Border)progress.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
                var layout = (Grid)surface.Child!;
                var details = layout.Children.OfType<ScrollViewer>().Single();
                var bar = layout.Children.OfType<ProgressBar>().Single();
                var cancel = layout.Children.OfType<Button>().Single();
                check(details.Extent.Height > details.Viewport.Height && details.Bounds.Bottom <= bar.Bounds.Top &&
                    bar.Bounds.Bottom <= cancel.Bounds.Top && cancel.Bounds.Bottom <= layout.Bounds.Height &&
                    layout.Bounds.Right <= surface.Bounds.Width && layout.Bounds.Bottom <= surface.Bounds.Height &&
                    bar.Value == 1 && bar.Maximum == 4,
                    $"Narrow PACKS progress scrolls long status text above the progress bar and Cancel [status={details.Bounds}/{details.Extent}, bar={bar.Bounds}/{bar.Value}/{bar.Maximum}, cancel={cancel.Bounds}]");
                cancel.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                check(await pending.WaitAsync(TimeSpan.FromSeconds(10)) == null && main.Enabled,
                    "Native PACKS Cancel unwinds the operation and restores its owner");
            }
            finally
            {
                if (!pending.IsCompleted)
                    await progress.CancelForQuitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            }
        }

        using (var foundry = new FoundryExportDialog(new[]
        {
            new Character { Alias = "First character" }, new Character { Alias = "Second character" }
        }))
        {
            var pending = foundry.ShowDialogAsync(main);
            try
            {
                foundry.ClientSize = new System.Drawing.Size(280, 200);
                await Idle();
                var surface = (Border)foundry.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
                var layout = (DockPanel)surface.Child!;
                var scroll = layout.Children.OfType<ScrollViewer>().Single();
                var commands = layout.Children.OfType<WrapPanel>().Single();
                var pickers = ((StackPanel)scroll.Content!).Children.OfType<ComboBox>().ToArray();
                check(foundry.Target == FoundryActorExporter.Target.Version126 && foundry.CharacterIndex == 0 &&
                    scroll.Extent.Height > scroll.Viewport.Height && scroll.Bounds.Bottom <= commands.Bounds.Top &&
                    commands.Bounds.Bottom <= layout.Bounds.Height,
                    "Narrow Foundry export scrolls guidance above both actions and defaults to the current version");
                pickers[0].SelectedIndex = 1;
                pickers[1].SelectedIndex = 0;
                check(foundry.CharacterIndex == 1 && foundry.Target == FoundryActorExporter.Target.Version16,
                    "Native Foundry export selectors update the requested character and target");
                commands.Children.OfType<Button>().Single(button => button.IsCancel).RaiseEvent(
                    new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                check(await pending.WaitAsync(TimeSpan.FromSeconds(10)) == Forms.DialogResult.Cancel && main.Enabled,
                    "Foundry export cancellation closes the dialog and restores its owner");
            }
            finally
            {
                if (!pending.IsCompleted)
                    foundry.Close();
            }
        }

        var aboutTask = AboutWindow.ShowAsync(main);
        var about = (AboutWindow)NativeDialogHost.ActiveWindow!;
        about.Width = 280;
        about.Height = 240;
        await Idle();
        var aboutLayout = (Grid)about.Content!;
        var aboutScroll = (ScrollViewer)aboutLayout.Children[0];
        var aboutDetails = (StackPanel)aboutScroll.Content!;
        check(aboutScroll.Extent.Height > aboutScroll.Viewport.Height &&
            aboutScroll.Extent.Width <= aboutScroll.Viewport.Width &&
            aboutScroll.Bounds.Bottom <= aboutLayout.Children[1].Bounds.Top,
            "Narrow About dialog scrolls its details without covering its action buttons");
        check(aboutDetails.Children.OfType<SelectableTextBlock>().Any(text => text.Text == AboutWindow.BuildDetails) &&
            aboutDetails.Children.OfType<Image>().Any(image => image.Source != null),
            "About renders selectable build metadata and the packaged Chummer logo");
        var closeAbout = ((WrapPanel)aboutLayout.Children[1]).Children.OfType<Button>().Single(button => button.Name == "AboutClose");
        var aboutActions = (WrapPanel)aboutLayout.Children[1];
        var product = aboutDetails.Children.OfType<TextBlock>().First(text => text is not SelectableTextBlock);
        double baseTitleFont = product.FontSize;
        double baseButtonFont = closeAbout.FontSize;
        Avalonia.Controls.Documents.TextElement.SetFontSize(aboutLayout, baseButtonFont * 1.5);
        aboutActions.Children.OfType<Button>().First().Content = "Translated command to support continued development";
        await Idle();
        check(Math.Abs(product.FontSize - baseTitleFont * 1.5) < 0.1 &&
            closeAbout.FontSize == baseButtonFont * 1.5 &&
            aboutActions.Children.All(button => button.Bounds.Right <= aboutActions.Bounds.Width + 1) &&
            aboutScroll.Bounds.Bottom <= aboutActions.Bounds.Top,
            "About title and long actions grow with inherited text without covering build details");
        closeAbout.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await aboutTask;
        check(NativeDialogHost.ActiveWindow == null && main.Enabled && Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled,
            "Closing the native About window restores its owner");

        var historyTask = RevisionHistoryWindow.ShowAsync(main);
        while (NativeDialogHost.ActiveWindow is not RevisionHistoryWindow && !historyTask.IsCompleted)
            await Task.Delay(10);
        var revision = (RevisionHistoryWindow)NativeDialogHost.ActiveWindow!;
        revision.Width = 280;
        revision.Height = 220;
        await Idle();
        var history = revision.History;
        check(history.Text == File.ReadAllText(RevisionHistoryWindow.HistoryPath) && history.IsReadOnly &&
            history.AcceptsReturn && history.TextWrapping == Avalonia.Media.TextWrapping.Wrap && history.CaretIndex == 0,
            "Revision History loads the packaged changelog as read-only wrapping text from the beginning");
        check(history.Bounds.Width <= revision.ClientSize.Width && history.Bounds.Height <= revision.ClientSize.Height,
            "Narrow Revision History fits the text field to the visible window");
        history.RaiseEvent(new KeyEventArgs { Key = Key.Escape, RoutedEvent = InputElement.KeyDownEvent });
        await historyTask;
        check(NativeDialogHost.ActiveWindow == null && main.Enabled && Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled,
            "Escape closes Revision History and restores its owner");

        var release = new TaskCompletionSource<ReleaseInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var updaterTask = UpdateWindow.ShowAsync(main, _ => release.Task);
        var updater = (UpdateWindow)NativeDialogHost.ActiveWindow!;
        updater.Width = 280;
        updater.Height = 240;
        await Idle();
        check(!updater.Download.IsEnabled && updater.Status.Text == "Checking Nightly releases…",
            "The native updater disables Download while the release check is pending");
        string notes = string.Join("\n", Enumerable.Repeat("Synthetic release notes for the narrow window.", 40));
        release.SetResult(new ReleaseInfo(notes, new string('a', 40), true));
        await Idle();
        var updateLayout = (Grid)updater.Content!;
        check(updater.Status.Text == "A newer Nightly build is available." && updater.ReleaseNotes.Text == notes &&
            updater.ReleaseNotes.IsReadOnly && updater.Download.IsEnabled &&
            updater.ReleaseNotes.Bounds.Bottom <= updateLayout.Children[3].Bounds.Top,
            "The native updater renders wrapping release notes above the available actions");
        updater.CloseButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await updaterTask;

        CancellationToken pendingToken = default;
        updaterTask = UpdateWindow.ShowAsync(main, async token =>
        {
            pendingToken = token;
            await Task.Delay(Timeout.Infinite, token);
            return new ReleaseInfo("", "", false);
        });
        updater = (UpdateWindow)NativeDialogHost.ActiveWindow!;
        await Idle();
        updater.Close();
        await updaterTask;
        check(pendingToken.IsCancellationRequested && NativeDialogHost.ActiveWindow == null &&
            Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled,
            "Closing the native updater cancels an in-flight check and restores the owner");

        var character = new System.Xml.XmlDocument();
        character.LoadXml("<characters><character><name>Synthetic Export Name</name></character></characters>");
        string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xml");
        try
        {
            int attempts = 0;
            string? extension = null;
            var exportTask = CharacterExportWindow.ShowAsync(main, character, (_, selectedExtension) =>
            {
                extension = selectedExtension;
                return Task.FromResult<string?>(++attempts == 1 ? null : destination);
            });
            var export = (CharacterExportWindow)NativeDialogHost.ActiveWindow!;
            export.Width = 260;
            await Idle();
            export.FormatInput.SelectedItem = "Squad Manager";
            var exportLayout = (Grid)export.Content!;
            var exportViewport = (ScrollViewer)exportLayout.Children[0];
            var exportFooter = (WrapPanel)exportLayout.Children[1];
            var exportFields = (StackPanel)exportViewport.Content!;
            var exportCaption = (TextBlock)exportFields.Children[0];
            exportCaption.Text = string.Join(" ", Enumerable.Repeat("Translated export format instructions", 18));
            export.SizeToContent = SizeToContent.Manual;
            export.Height = 230;
            Avalonia.Controls.Documents.TextElement.SetFontSize(exportLayout, export.FormatInput.FontSize * 1.5);
            await Task.Delay(80);
            export.FormatInput.BringIntoView();
            await Idle();
            check(export.FormatInput.SelectedIndex >= 0 && exportCaption.Bounds.Bottom <= export.FormatInput.Bounds.Top &&
                exportViewport.Offset.Y > 0 && exportViewport.Bounds.Bottom <= exportFooter.Bounds.Top &&
                exportFooter.Bounds.Bottom <= exportLayout.Bounds.Height,
                "Narrow character export scrolls long text above its action buttons at 150% text");
            export.ExportButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(export.IsVisible && export.ExportButton.IsEnabled && !File.Exists(destination),
                "Canceling the save picker leaves character export available for retry");
            export.ExportButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            await exportTask.WaitAsync(TimeSpan.FromSeconds(5));
            var saved = System.Xml.Linq.XDocument.Load(destination);
            check(extension == "xml" && saved.Descendants().Any(element =>
                element.Name.LocalName == "Name" && element.Value == "Synthetic Export Name") &&
                NativeDialogHost.ActiveWindow == null,
                "Character export uses the template extension and writes the selected transform output");
        }
        finally { File.Delete(destination); }

        var catalog = new System.Xml.XmlDocument();
        catalog.LoadXml("<chummer><armors/><mods/></chummer>");
        var armor = XmlManager.Instance.Load("armor.xml").SelectSingleNode("/chummer/armors/armor[name='Feedback Clothing']")!;
        catalog.SelectSingleNode("/chummer/armors")!.AppendChild(catalog.ImportNode(armor, true));
        var validation = new CatalogValidationWindow(_ => catalog) { Width = 280, Height = 230 };
        validation.Show(Forms.AvaloniaHostInterop.ToAvaloniaWindow(main));
        try
        {
            await Idle();
            var layout = (Grid)validation.Content!;
            check(validation.CatalogInput.ItemCount == 9 && !validation.TestButton.IsEnabled &&
                validation.CatalogInput.Bounds.Right <= validation.TestButton.Bounds.Left &&
                layout.Children[0].Bounds.Bottom <= validation.Progress.Bounds.Top &&
                validation.Progress.Bounds.Bottom <= validation.Output.Bounds.Top,
                "Narrow catalog validation keeps the selector, action, progress, and output clear");
            validation.CatalogInput.SelectedItem = "armor.xml";
            validation.TestButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            var validationDeadline = DateTime.UtcNow.AddSeconds(10);
            while (!validation.TestButton.IsEnabled && DateTime.UtcNow < validationDeadline)
                await Task.Delay(10);
            check(validation.Output.Text!.EndsWith("Done validation", StringComparison.Ordinal) &&
                validation.Progress.Value > 0 && validation.Progress.Value == validation.Progress.Maximum && validation.TestButton.IsEnabled,
                "The native catalog validator runs the armor checks and reports completion");
        }
        finally { validation.Close(); }

        foreach (var source in new[] { Improvement.ImprovementSource.Cyberware, Improvement.ImprovementSource.Bioware })
        {
            using var suite = new frmCreateCyberwareSuite(new Character(), source);
            var pending = suite.ShowDialogAsync(main);
            await Idle();
            suite.ClientSize = new System.Drawing.Size(260, 180);
            await Idle();
            var host = suite.Controls.OfType<Forms.NativeControlHost>().Single();
            var layout = (Grid)((Border)host.NativeControl!).Child!;
            var name = layout.GetVisualDescendants().OfType<TextBox>().Single(input => input.Name == "NativeSuiteName");
            var file = layout.GetVisualDescendants().OfType<TextBox>().Single(input => input.Name == "NativeSuiteFile");
            check(file.Text == (source == Improvement.ImprovementSource.Cyberware ? "custom_cyberware.xml" : "custom_bioware.xml") &&
                name.IsEffectivelyVisible && layout.Children[0].Bounds.Bottom <= layout.Children[1].Bounds.Top,
                "The narrow native suite form preserves its catalog filename and keeps the actions clear");
            name.Text = "Synthetic suite";
            var sourceName = (Forms.TextBox)typeof(frmCreateCyberwareSuite)
                .GetField("txtName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(suite)!;
            check(sourceName.Text == name.Text, "The native suite name updates the existing writer controller");
            layout.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdCancel")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.Cancel, "Canceling native suite creation ends the dialog without invoking the writer");

            var directory = Directory.CreateTempSubdirectory("chummer-suite-check-");
            try
            {
                string catalogType = source == Improvement.ImprovementSource.Cyberware ? "cyberware" : "bioware";
                string path = Path.Combine(directory.FullName, "custom_" + catalogType + ".xml");
                File.WriteAllText(path, "<chummer><suites><suite><name>Existing suite</name></suite></suites></chummer>");
                var ownerCharacter = new Character();
                ownerCharacter.Cyberware.Add(new Cyberware(ownerCharacter)
                {
                    Name = "Synthetic implant", SourceType = source, Rating = 2,
                    Grade = GlobalOptions.CyberwareGrades.GetGrade("Standard")
                });
                using var writer = new frmCreateCyberwareSuite(ownerCharacter, source, directory.FullName);
                var save = writer.ShowDialogAsync(main);
                await Idle();
                var writerHost = writer.Controls.OfType<Forms.NativeControlHost>().Single();
                var controls = (Border)writerHost.NativeControl!;
                controls.GetVisualDescendants().OfType<TextBox>()
                    .Single(input => input.Name == "NativeSuiteName").Text = "Synthetic suite";
                bool acknowledged = false;
                Dispatcher.UIThread.Post(() =>
                {
                    var message = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().LastOrDefault();
                    if (message == null)
                        return;
                    acknowledged = true;
                    message.DialogResult = Forms.DialogResult.OK;
                    message.Close();
                });
                controls.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOK")
                    .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                var suites = System.Xml.Linq.XDocument.Load(path).Root!.Element("suites")!.Elements("suite").ToArray();
                check(await save.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK && acknowledged &&
                    suites.Length == 2 && suites[0].Element("name")?.Value == "Existing suite" &&
                    suites[1].Element("name")?.Value == "Synthetic suite" &&
                    suites[1].Element(catalogType + "s")?.Element(catalogType)?.Element("rating")?.Value == "2",
                    "Native suite confirmation appends the selected ware and preserves existing suites");
            }
            finally { directory.Delete(recursive: true); }
        }

        using var packs = new frmCreatePACKSKit(new Character());
        var packsPending = packs.ShowDialogAsync(main);
        await Idle();
        packs.ClientSize = new System.Drawing.Size(270, 250);
        await Idle();
        var packsHost = packs.Controls.OfType<Forms.NativeControlHost>().Single();
        var packsLayout = (Grid)((Border)packsHost.NativeControl!).Child!;
        var packsScroll = (ScrollViewer)packsLayout.Children[0];
        var options = packsScroll.GetVisualDescendants().OfType<CheckBox>().ToArray();
        check(options.Length == 14 && packsScroll.Extent.Height > packsScroll.Viewport.Height &&
            packsScroll.Bounds.Bottom <= packsLayout.Children[1].Bounds.Top,
            "Narrow PACKS creation scrolls all fourteen export options above its action buttons");
        string id = Guid.NewGuid().ToString("N");
        string kitName = "Synthetic PACKS " + id;
        string kitFile = "custom_" + id + "_packs.xml";
        string kitPath = Path.Combine(MacPaths.CustomDataDirectory, kitFile);
        try
        {
            packsLayout.GetVisualDescendants().OfType<TextBox>().Single(input => input.Name == "NativePACKSName").Text = kitName;
            packsLayout.GetVisualDescendants().OfType<TextBox>().Single(input => input.Name == "NativePACKSFile").Text = kitFile;
            options.Single(option => option.Name == "NativechkAttributes").IsChecked = true;
            Dispatcher.UIThread.Post(() =>
            {
                var message = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().LastOrDefault();
                if (message == null)
                    return;
                message.DialogResult = Forms.DialogResult.OK;
                message.Close();
            });
            packsLayout.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOK")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            var kit = System.Xml.Linq.XDocument.Load(kitPath).Root!.Element("packs")!.Element("pack")!;
            check(await packsPending.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
                kit.Element("name")?.Value == kitName && kit.Element("attributes")?.Element("bod") != null &&
                kit.Element("qualities") == null, "Native PACKS confirmation writes the selected sections to the custom kit");
        }
        finally { File.Delete(kitPath); }

        using var preview = new frmViewer { CharacterXML = character };
        preview.Show(main);
        await preview.OutputTask.WaitAsync(TimeSpan.FromSeconds(10));
        preview.ClientSize = new System.Drawing.Size(370, 340);
        await Idle();
        var headerHost = preview.Controls.OfType<Forms.NativeControlHost>().Single();
        var previewLayout = (Grid)((Border)headerHost.NativeControl!).Child!;
        var header = (StackPanel)previewLayout.Children[0];
        var documentSlot = previewLayout.Children[1];
        var previewActions = (WrapPanel)header.Children[0];
        var browser = preview.Controls.OfType<Forms.WebBrowser>().Single();
        check(previewActions.Children.OfType<Button>().Where(button => button.IsVisible)
                .Select(button => button.Bounds.Top).Distinct().Count() > 1 &&
            header.Bounds.Bottom <= documentSlot.Bounds.Top && browser.Top == (int)Math.Ceiling(previewLayout.Bounds.Top + documentSlot.Bounds.Top) &&
            browser.Bottom <= preview.ClientSize.Height &&
            browser.Height > 0, $"Narrow character preview wraps the actions without covering the sheet (header {header.Bounds}, grid {previewLayout.Bounds}, slot {documentSlot.Bounds}, browser {browser.Bounds}, client {preview.ClientSize}, rows {string.Join(", ", previewActions.Children.OfType<Button>().Where(button => button.IsVisible).Select(button => button.Bounds.Top))})");
        check(header.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdSaveHTML").IsEnabled &&
            header.GetVisualDescendants().OfType<ComboBox>().Single().SelectedIndex >= 0,
            "Character preview keeps the native sheet selector and HTML action available after rendering");
        var previewStatus = (Forms.Label)typeof(frmViewer)
            .GetField("_status", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(preview)!;
        string longStatus = string.Join(" ", Enumerable.Repeat("Character sheet preparation status.", 18));
        previewStatus.Text = longStatus;
        await Idle();
        var nativeStatus = header.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Name == "NativePreviewStatus");
        check(nativeStatus.MaxLines == 3 && Equals(ToolTip.GetTip(nativeStatus), longStatus) &&
            browser.Top == (int)Math.Ceiling(previewLayout.Bounds.Top + documentSlot.Bounds.Top) && browser.Height > 0,
            "A long preview status stays readable while the measured document row retains space");
        preview.Close();

        var suiteCharacter = new Character();
        byte[] suiteSnapshot = suiteCharacter.CaptureSnapshot();
        using var suitePicker = new frmSelectCyberwareSuite(Improvement.ImprovementSource.Cyberware, suiteCharacter);
        var suiteChoice = suitePicker.ShowDialogAsync(main);
        await Idle();
        suitePicker.ClientSize = new System.Drawing.Size(320, 320);
        await Idle();
        var suiteHost = suitePicker.Controls.OfType<Forms.NativeControlHost>().Single();
        var suiteLayout = (Grid)((Border)suiteHost.NativeControl!).Child!;
        var suitePanes = (Grid)suiteLayout.Children[0];
        var suiteList = (ListBox)suitePanes.Children[0];
        check(Grid.GetRow(suitePanes.Children[1]) == 1 && suiteList.Bounds.Bottom <= suitePanes.Children[1].Bounds.Top &&
            suitePanes.Bounds.Bottom <= suiteLayout.Children[1].Bounds.Top,
            "Narrow cyberware suite picker stacks its choices and details above the actions");
        const string catalogSuite = "Aztechnology Topo";
        suiteList.SelectedItem = catalogSuite;
        await Idle();
        check(suiteLayout.GetVisualDescendants().OfType<TextBlock>().Single(label => label.Name == "NativelblGrade").Text == "Standard" &&
            suiteLayout.GetVisualDescendants().OfType<TextBlock>().Single(label => label.Name == "NativelblCyberware")
                .Text!.Contains("Muscle Replacement", StringComparison.Ordinal),
            "Selecting a native suite updates its grade and parts from the existing catalog calculation");
        var suiteAccept = suiteLayout.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOK");
        var suiteCaption = (TextBlock)suiteAccept.Content!;
        suiteCaption.Text = string.Join(" ", Enumerable.Repeat("Translated suite confirmation", 5));
        Avalonia.Controls.Documents.TextElement.SetFontSize(suiteLayout, suiteAccept.FontSize * 1.5);
        await Idle();
        var suiteActions = (WrapPanel)suiteLayout.Children[1];
        check(suiteCaption.Bounds.Height > suiteCaption.FontSize * 2 &&
            suiteAccept.Bounds.Right <= suiteActions.Bounds.Width + 1 &&
            suitePanes.Bounds.Bottom <= suiteActions.Bounds.Top,
            "Narrow suite picker wraps a translated confirmation without covering the choices");
        suiteAccept.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        check(await suiteChoice.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
            suitePicker.SelectedSuite == catalogSuite && suiteSnapshot.SequenceEqual(suiteCharacter.CaptureSnapshot()),
            "Native suite acceptance returns the canonical choice without changing the character");

        var programCharacter = new Character();
        bool addedBook = !programCharacter.Options.Books.Contains("UN");
        if (addedBook)
            programCharacter.Options.Books.Add("UN");
        try
        {
            using var picker = new frmSelectProgramOption(programCharacter) { ProgramCategory = "Hacking" };
            var pending = picker.ShowDialogAsync(main);
            await Idle();
            picker.ClientSize = new System.Drawing.Size(320, 320);
            await Idle();
            var host = picker.Controls.OfType<Forms.NativeControlHost>().Single();
            var layout = (Grid)((Border)host.NativeControl!).Child!;
            var panes = (Grid)layout.Children[0];
            var list = (ListBox)panes.Children[0];
            list.SelectedItem = "Copy Protection";
            await Idle();
            var source = panes.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativeOptionSource");
            check(Grid.GetRow(panes.Children[1]) == 1 && panes.Bounds.Bottom <= layout.Children[1].Bounds.Top &&
                source.IsEnabled && source.Content is string reference && reference.EndsWith(" 114", StringComparison.Ordinal),
                "The narrow program option picker stacks its source details below the list and keeps the actions clear");
            layout.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOKAdd")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
                picker.SelectedOption == "Copy Protection" && picker.AddAgain,
                "Native program option Add More returns the selected canonical option");
        }
        finally
        {
            if (addedBook)
                programCharacter.Options.Books.Remove("UN");
        }
        foreach (var (mode, choice, page) in new[]
        {
            (frmSelectMetamagic.Mode.Metamagic, "Centering", "198"),
            (frmSelectMetamagic.Mode.Echo, "Firewall Upgrade", "243")
        })
        {
            using var picker = new frmSelectMetamagic(new Character()) { WindowMode = mode };
            var pending = picker.ShowDialogAsync(main);
            await Idle();
            picker.ClientSize = new System.Drawing.Size(320, 320);
            await Idle();
            var host = picker.Controls.OfType<Forms.NativeControlHost>().Single();
            var layout = (Grid)((Border)host.NativeControl!).Child!;
            var panes = (Grid)layout.Children[0];
            var limit = panes.GetVisualDescendants().OfType<CheckBox>().Single();
            var list = (ListBox)panes.Children[0];
            int limitedCount = list.ItemCount;
            bool initiallyLimited = limit.IsChecked == true;
            limit.IsChecked = false;
            await Idle();
            list.SelectedItem = choice;
            await Idle();
            var source = panes.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativeMetamagicSource");
            check(initiallyLimited && list.ItemCount >= limitedCount && Grid.GetRow(panes.Children[1]) == 1 &&
                panes.Bounds.Bottom <= layout.Children[1].Bounds.Top && source.IsEnabled &&
                source.Content is string reference && reference.EndsWith(" " + page, StringComparison.Ordinal),
                $"The narrow {mode} picker shows its filter and source details above the actions");
            layout.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOKAdd")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
                picker.SelectedMetamagic == choice && picker.AddAgain,
                $"Native {mode} Add More returns the selected canonical entry");
        }

        using var powerPicker = new frmSelectPower(new Character());
        var powerChoice = powerPicker.ShowDialogAsync(main);
        await Idle();
        powerPicker.ClientSize = new System.Drawing.Size(320, 320);
        await Idle();
        var powerHost = powerPicker.Controls.OfType<Forms.NativeControlHost>().Single();
        var powerLayout = (Grid)((Border)powerHost.NativeControl!).Child!;
        var powerSearch = powerLayout.GetVisualDescendants().OfType<TextBox>().Single();
        var powerPanes = (Grid)powerLayout.Children[1];
        var powerList = (ListBox)powerPanes.Children[0];
        powerSearch.Text = "\"";
        await Idle();
        check(powerList.ItemCount == 0, "A quoted adept power search is treated as literal text");
        powerSearch.Text = "combat sense";
        await Idle();
        check(powerList.Items.Cast<string>().SequenceEqual(new[] { "Combat Sense" }) &&
            Grid.GetRow(powerPanes.Children[1]) == 1 &&
            powerLayout.Children[0].Bounds.Bottom <= powerPanes.Bounds.Top &&
            powerPanes.Bounds.Bottom <= powerLayout.Children[2].Bounds.Top,
            "Adept power search filters the native list and stays clear of details and actions at narrow widths");
        powerList.SelectedIndex = -1;
        powerSearch.RaiseEvent(new KeyEventArgs { Key = Key.Down, RoutedEvent = InputElement.KeyDownEvent });
        await Idle();
        var powerPoints = powerPanes.GetVisualDescendants().OfType<TextBlock>().Single(label => label.Name == "NativePowerPoints");
        var powerSource = powerPanes.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativePowerSource");
        check(powerPoints.Text!.StartsWith(".5 / ", StringComparison.Ordinal) && powerSource.IsEnabled &&
            powerSource.Content is string powerReference && powerReference.EndsWith(" 195", StringComparison.Ordinal),
            "Selecting a rated adept power shows its cost per level and source reference");
        var originalPowerSource = powerSource.Content;
        powerSource.Content = string.Join(" ", Enumerable.Repeat("Long translated power citation", 10));
        Avalonia.Controls.Documents.TextElement.SetFontSize(powerLayout, powerSource.FontSize * 1.5);
        await Idle();
        var powerSourceCaption = powerSource.GetVisualDescendants().OfType<TextBlock>().Single(text => Equals(text.Text, powerSource.Content));
        var powerDetailView = (ScrollViewer)powerPanes.Children[1];
        check(powerSourceCaption.Bounds.Height > powerSourceCaption.FontSize * 2 &&
            powerSource.Bounds.Right <= powerDetailView.Viewport.Width + 1 &&
            powerPanes.Bounds.Bottom <= powerLayout.Children[2].Bounds.Top,
            "The narrow adept power picker wraps a translated citation at 150% text without covering its actions");
        powerSource.Content = originalPowerSource;
        powerLayout.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
        powerLayout.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOKAdd")
            .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        check(await powerChoice.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
            powerPicker.SelectedPower == "Combat Sense" && powerPicker.LevelEnabled && powerPicker.PointsPerLevel == 0.5m &&
            powerPicker.AddAgain, "Native adept power Add More returns the canonical choice and level cost");

        using var programPicker = new frmSelectProgram(new Character());
        var selectedProgram = programPicker.ShowDialogAsync(main);
        await Idle();
        programPicker.ClientSize = new System.Drawing.Size(330, 340);
        await Idle();
        var programSurface = (Border)programPicker.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
        var programLayout = (Grid)programSurface.Child!;
        var programPanes = (Grid)programLayout.Children[1];
        for (int attempt = 0; attempt < 20 && Grid.GetRow(programPanes.Children[1]) != 1; attempt++)
            await Task.Delay(20);
        var programTree = (TreeView)programPanes.Children[0];
        var programSearch = programSurface.GetVisualDescendants().OfType<TextBox>().Single();
        programSearch.Text = "\"";
        await Idle();
        check(programTree.Items.Count == 0, "Program search treats quotation marks as literal text");
        programSearch.Text = "analyze";
        await Idle();
        var categoryRow = (TreeViewItem)programTree.Items[0]!;
        var programRow = (TreeViewItem)categoryRow.Items[0]!;
        programTree.SelectedItem = programRow;
        await Idle();
        var skill = programSurface.GetVisualDescendants().OfType<TextBlock>().Single(label => label.Name == "NativeProgramSkill");
        var programSource = programSurface.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativeProgramSource");
        check(programTree.Items.Count == 1 && categoryRow.Items.Count == 1 && skill.Text == "Computer" &&
            programSource.IsEnabled && programSource.Content is string sourcePage && sourcePage.EndsWith(" 232", StringComparison.Ordinal) &&
            Grid.GetRow(programPanes.Children[1]) == 1 && programLayout.Children[0].Bounds.Bottom <= programPanes.Bounds.Top &&
            programPanes.Bounds.Bottom <= programLayout.Children[2].Bounds.Top,
            $"At narrow widths the Program picker keeps search and actions clear and shows the selected program details [width={programPanes.Bounds.Width}, detailsRow={Grid.GetRow(programPanes.Children[1])}, skill={skill.Text}, source={programSource.Content}]");
        programTree.SelectedItem = categoryRow;
        check(!programSource.IsEnabled && skill.Text == "", "Selecting a program category clears the selected program details");
        programTree.SelectedItem = programRow;
        programSurface.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOKAdd")
            .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        check(await selectedProgram.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
            programPicker.SelectedProgram == "Analyze" && programPicker.AddAgain,
            "The native Program picker returns the canonical program and Add More choice");

        var spellCharacter = new Character();
        bool previousExtendedOption = spellCharacter.Options.ExtendAnyDetectionSpell;
        spellCharacter.Options.ExtendAnyDetectionSpell = true;
        try
        {
            using var spellPicker = new frmSelectSpell(spellCharacter);
            var spellChoice = spellPicker.ShowDialogAsync(main);
            await Idle();
            spellPicker.ClientSize = new System.Drawing.Size(330, 340);
            await Idle();
            var spellSurface = (Border)spellPicker.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            var spellLayout = (Grid)spellSurface.Child!;
            var spellPanes = (Grid)spellLayout.Children[1];
            var spellTree = (TreeView)spellPanes.Children[0];
            var spellSearch = spellSurface.GetVisualDescendants().OfType<TextBox>().Single();
            spellSearch.Text = "detect life";
            await Idle();
            var detection = (TreeViewItem)spellTree.Items[0]!;
            spellTree.SelectedItem = detection.Items.OfType<TreeViewItem>()
                .Single(row => ((Forms.TreeNode)row.Tag!).Tag?.ToString() == "Detect Life");
            await Idle();
            var spellDetails = (ScrollViewer)spellPanes.Children[1];
            var extended = spellSurface.GetVisualDescendants().OfType<CheckBox>().Single(box => box.Name == "NativechkExtended");
            var limited = spellSurface.GetVisualDescendants().OfType<CheckBox>().Single(box => box.Name == "NativechkLimited");
            var drain = spellSurface.GetVisualDescendants().OfType<TextBlock>().Single(label => label.Name == "NativelblDV");
            string? baseDrain = drain.Text;
            check(Grid.GetRow(spellDetails) == 1 && spellDetails.Extent.Height > spellDetails.Viewport.Height &&
                spellLayout.Children[0].Bounds.Bottom <= spellPanes.Bounds.Top &&
                spellPanes.Bounds.Bottom <= spellLayout.Children[2].Bounds.Top && extended.IsVisible && extended.IsEnabled,
                "The narrow Spell picker scrolls its details and options between search and the action buttons");
            extended.IsChecked = true;
            limited.IsChecked = true;
            check(drain.Text == baseDrain + "+2" && spellPicker.Extended && spellPicker.Limited,
                "The native Spell options update the drain value and controller flags");
            spellSurface.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOKAdd")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await spellChoice.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
                spellPicker.SelectedSpell == "Detect Life" && spellPicker.Limited && spellPicker.Extended && spellPicker.AddAgain,
                "The native Spell picker returns the selected spell with both options");
        }
        finally
        {
            spellCharacter.Options.ExtendAnyDetectionSpell = previousExtendedOption;
        }

        var mentorCharacter = new Character();
        bool addedStreetMagic = !mentorCharacter.Options.Books.Contains("SM");
        if (addedStreetMagic)
            mentorCharacter.Options.Books.Add("SM");
        try
        {
            using var mentorPicker = new frmSelectMentorSpirit(mentorCharacter);
            var mentorResult = mentorPicker.ShowDialogAsync(main);
            await Idle();
            mentorPicker.ClientSize = new System.Drawing.Size(340, 360);
            await Idle();
            var mentorSurface = (Border)mentorPicker.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            var mentorLayout = (Grid)mentorSurface.Child!;
            var mentorPanes = (Grid)mentorLayout.Children[0];
            var mentorCategory = mentorSurface.GetVisualDescendants().OfType<ComboBox>().Single(combo => combo.Name == "NativecboCategory");
            var mentors = mentorSurface.GetVisualDescendants().OfType<ListBox>().Single();
            mentorCategory.SelectedItem = "Other";
            await Idle();
            mentors.SelectedItem = "Sun";
            await Idle();
            var choices = mentorSurface.GetVisualDescendants().OfType<ComboBox>().Where(combo => combo != mentorCategory).ToArray();
            var mentorDetails = (ScrollViewer)mentorPanes.Children[1];
            check(Grid.GetRow(mentorDetails) == 1 && mentorDetails.Extent.Height > mentorDetails.Viewport.Height &&
                mentorPanes.Bounds.Bottom <= mentorLayout.Children[1].Bounds.Top &&
                choices.Length == 2 && choices.All(choice => choice.IsEffectivelyVisible && choice.ItemCount == 2),
                "The narrow Mentor picker scrolls both bonus choices and details above its confirmation button");
            mentors.SelectedItem = "Adversary";
            await Idle();
            check(choices[0].IsEffectivelyVisible && !choices[1].IsEffectivelyVisible && choices[1].ItemCount == 0,
                "Switching to a mentor with one bonus choice clears and hides the second choice");
            mentors.SelectedItem = "Sun";
            await Idle();
            choices[0].SelectedItem = "+2 dice for guardian spirits";
            choices[1].SelectedItem = "+2 dice for Combat Spells";
            mentorSurface.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOK")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await mentorResult.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
                mentorPicker.SelectedMentor == "Sun" && mentorPicker.Choice1 == "+2 dice for guardian spirits" &&
                mentorPicker.Choice2 == "+2 dice for Combat Spells" &&
                mentorPicker.Choice2BonusNode?.SelectSingleNode("spellcategory/name")?.InnerText == "Combat",
                "Native Mentor acceptance returns both canonical bonus choices and the associated bonus data");
        }
        finally
        {
            if (addedStreetMagic)
                mentorCharacter.Options.Books.Remove("SM");
        }

        using (var itemPicker = new frmSelectItem
        {
            Description = string.Join(" ", Enumerable.Repeat("Select the equipment to include in this group.", 14)),
            GeneralItems = new List<ListItem> { new() { Name = "First item", Value = "first" }, new() { Name = "Second item", Value = "second" } }
        })
        {
            var itemResult = itemPicker.ShowDialogAsync(main);
            await Idle();
            itemPicker.ClientSize = new System.Drawing.Size(280, 180);
            await Idle();
            var itemSurface = (Border)itemPicker.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            var itemLayout = (Grid)itemSurface.Child!;
            var itemFields = (ScrollViewer)itemLayout.Children[0];
            var items = itemSurface.GetVisualDescendants().OfType<ComboBox>().Single();
            check(itemFields.Extent.Height > itemFields.Viewport.Height &&
                itemFields.Bounds.Bottom <= itemLayout.Children[1].Bounds.Top &&
                items.Items.Cast<string>().SequenceEqual(new[] { "First item", "Second item" }),
                "The narrow item picker wraps a long prompt and keeps confirmation clear of scrolling fields");
            items.SelectedItem = "Second item";
            itemSurface.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOK")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await itemResult.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK && itemPicker.SelectedItem == "second",
                "The native general item picker returns the canonical value");
        }
        using (var restrictedPicker = new frmSelectItem { Character = new Character(), AllowAutoSelect = false })
        {
            var restrictedResult = restrictedPicker.ShowDialogAsync(main);
            await Idle();
            var surface = (Border)restrictedPicker.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            var restrictedChoice = surface.GetVisualDescendants().OfType<ComboBox>().Single();
            check(restrictedChoice.IsEditable, "The restricted item picker accepts an unlisted name");
            restrictedChoice.Text = "Custom license item";
            await Idle();
            surface.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOK")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await restrictedResult.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
                restrictedPicker.SelectedItem == "Custom license item", "The native restricted item picker returns entered text");
        }
        using (var forcedPicker = new frmSelectItem { ForceItem = "Forced item" })
        {
            check(await forcedPicker.ShowDialogAsync(main).WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
                forcedPicker.SelectedItem == "Forced item", "The forced item still resolves directly");
        }

    }

    private static Task Idle() => Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).GetTask();
}
