#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.VisualTree;
using Majorsilence.Forms;

namespace Chummer;

// Exercise real editors: generic MDI forms cannot expose toolbar ownership or
// character command availability when activation and close events interleave.
internal static class NativeCharacterSwitchChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;

    internal static async Task RunAsync(frmMain main, Character character, Action<bool, string> check, bool checkFullscreen = true)
    {
        byte[] original = character.CaptureSnapshot();
        if (checkFullscreen && System.OperatingSystem.IsMacOS())
        {
            var nativeMain = main.ToAvaloniaWindow();
            nativeMain.WindowState = Avalonia.Controls.WindowState.FullScreen; await Task.Delay(100);
            check(main.Controls.OfType<NativeControlHost>().Single(host => host.Name == "NativeWorkspaceMenu").Visible && Field<ToolStripMenuItem>(main, "optionsToolStripMenuItem").Visible &&
                Field<ToolStripMenuItem>(main, "exitToolStripMenuItem").Visible,
                "Mac full-screen fallback includes Preferences and Quit");
            nativeMain.WindowState = Avalonia.Controls.WindowState.Normal; await Task.Delay(100);
            check(!main.Controls.OfType<NativeControlHost>().Single(host => host.Name == "NativeWorkspaceMenu").Visible && !Field<ToolStripMenuItem>(main, "optionsToolStripMenuItem").Visible &&
                !Field<ToolStripMenuItem>(main, "exitToolStripMenuItem").Visible,
                "Mac leaving full screen restores application-menu command placement");
        }

        var menuProjection = main.Controls.OfType<NativeControlHost>().Single(host => host.Name == "NativeWorkspaceMenu");
        var toolbarProjection = main.Controls.OfType<NativeControlHost>().Single(host => host.Name == "NativeWorkspaceToolbar");
        var menuViewport = ((Avalonia.Controls.DockPanel)menuProjection.NativeControl!).Children.OfType<Avalonia.Controls.ScrollViewer>().Single();
        var nativeMenuBar = (Avalonia.Controls.Menu)menuViewport.Content!;
        var nativeToolbar = (Avalonia.Controls.WrapPanel)((Avalonia.Controls.Border)toolbarProjection.NativeControl!).Child!;
        var legacyMenu = Field<MenuStrip>(main, "menuStrip");
        var legacyToolbar = Field<ToolStrip>(main, "toolStrip");
        check(!legacyMenu.Visible && !legacyToolbar.Visible && toolbarProjection.Visible &&
            menuProjection.Visible == !System.OperatingSystem.IsMacOS(),
            "Workspace presents its native toolbar and the platform-appropriate menu surface");

        var openLocal = Field<ToolStripButton>(main, "openToolStripButton");
        var gitHub = nativeToolbar.Children.OfType<Avalonia.Controls.Button>().Single(button =>
            button.Tag is ToolStripButton { Name: "openGitHubToolStripButton" });
        check(gitHub.IsEnabled && nativeToolbar.Children.IndexOf(gitHub) == nativeToolbar.Children.IndexOf(
                nativeToolbar.Children.OfType<Avalonia.Controls.Button>().Single(button => ReferenceEquals(button.Tag, openLocal))) + 1,
            "Workspace keeps Open from GitHub available beside Open");
        gitHub.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        var repositorySetup = NativeDialogHost.ActiveWindow ?? throw new InvalidOperationException("Repository setup did not open.");
        check(repositorySetup.GetVisualDescendants().OfType<Avalonia.Controls.TextBox>().Any(control =>
                Avalonia.Automation.AutomationProperties.GetName(control) == "GitHub repository") && !gitHub.IsEnabled,
            "Open from GitHub opens repository setup on first use and guards duplicate clicks");
        repositorySetup.Width = 320;
        repositorySetup.Height = 340;
        var setupScroll = ((Avalonia.Controls.Grid)repositorySetup.Content!).Children.OfType<Avalonia.Controls.ScrollViewer>().Single();
        var verify = setupScroll.GetVisualDescendants().OfType<Avalonia.Controls.Button>().Single(button => button.IsDefault);
        Avalonia.Controls.Documents.TextElement.SetFontSize(setupScroll, verify.FontSize * 1.5);
        for (int attempt = 0; attempt < 30 && (setupScroll.Viewport.Height <= 0 || repositorySetup.ClientSize.Width > 320); attempt++)
            await Task.Delay(20);
        setupScroll.Offset = new Vector(0, setupScroll.Extent.Height - setupScroll.Viewport.Height);
        await Task.Delay(50);
        var verifyPosition = verify.TranslatePoint(default, setupScroll);
        check(repositorySetup.CanResize && verifyPosition.HasValue && verifyPosition.Value.Y >= 0 &&
            verifyPosition.Value.Y + verify.Bounds.Height <= setupScroll.Viewport.Height + 1 && setupScroll.Offset.Y > 0,
            $"Narrow repository setup scrolls to the verification command at 150% text [position={verifyPosition}, command={verify.Bounds}, viewport={setupScroll.Viewport}, offset={setupScroll.Offset}, scroll={setupScroll.Bounds}, window={repositorySetup.ClientSize}, extent={setupScroll.Extent}, presenter={setupScroll.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ScrollContentPresenter>().FirstOrDefault()?.Bounds}]");
        repositorySetup.Close();
        await Task.Delay(60);
        check(gitHub.IsEnabled && NativeDialogHost.ActiveWindow == null,
            "Cancelling repository setup restores the workspace toolbar");

        int menuInvocations = 0, toolInvocations = 0;
        var testMenu = new ToolStripMenuItem("&Synthetic menu") { ShortcutKeys = Keys.Control | Keys.F9 };
        testMenu.Click += (_, _) => menuInvocations++;
        var testTool = new ToolStripButton { Text = "Synthetic tool" };
        testTool.Click += (_, _) => toolInvocations++;
        var file = Field<ToolStripMenuItem>(main, "fileMenu");
        file.DropDownItems.Add(testMenu);
        legacyToolbar.Items.Add(testTool);
        bool showMenus = menuProjection.Visible;
        try
        {
            await Task.Delay(40);
            menuProjection.Visible = true;
            menuProjection.SyncNativeControl();
            await Task.Delay(60);
            var fileHeader = nativeMenuBar.Items.OfType<Avalonia.Controls.MenuItem>().Single(item => ReferenceEquals(item.Tag, file));
            fileHeader.IsSubMenuOpen = true;
            await Task.Delay(40);
            var nativeEntry = fileHeader.Items.OfType<Avalonia.Controls.MenuItem>().Single(item => ReferenceEquals(item.Tag, testMenu));
            var nativeButton = nativeToolbar.Children.OfType<Avalonia.Controls.Button>().Single(button => ReferenceEquals(button.Tag, testTool));
            check(fileHeader.Bounds.Width > 0 && nativeButton.Bounds.Width > 0 &&
                nativeToolbar.Children.OfType<Avalonia.Controls.Button>().Any(button => button.Content is Avalonia.Controls.DockPanel stack &&
                    stack.Children.OfType<Avalonia.Controls.Image>().Any(image => image.Source != null && image.IsEffectivelyVisible)),
                $"Workspace renders the native menu row, toolbar buttons, and original button imagery [menu={fileHeader.Bounds}; button={nativeButton.Bounds}; menuHost={menuProjection.Visible}/{menuProjection.Bounds}; toolbarHost={toolbarProjection.Visible}/{toolbarProjection.Bounds}; toolbar={nativeToolbar.Bounds}/{nativeToolbar.IsEffectivelyVisible}; icons={nativeToolbar.Children.OfType<Avalonia.Controls.Button>().Count(button => button.Content is Avalonia.Controls.DockPanel stack && stack.Children.OfType<Avalonia.Controls.Image>().Any(image => image.Source != null))}]");
            int defaultMenuHeight = menuProjection.Height;
            double menuFont = nativeMenuBar.FontSize;
            try
            {
                Avalonia.Controls.Documents.TextElement.SetFontSize(nativeMenuBar, menuFont * 2.25);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
                check(fileHeader.FontSize >= menuFont * 2.25 && menuProjection.Height > defaultMenuHeight &&
                    fileHeader.Bounds.Bottom <= menuProjection.Height && menuProjection.Bottom <= toolbarProjection.Top,
                    $"Workspace menu expands for larger text without covering the toolbar [menu={defaultMenuHeight}/{menuProjection.Height}, font={menuFont}/{fileHeader.FontSize}, item={fileHeader.Bounds}, toolbar={toolbarProjection.Top}]");
            }
            finally
            {
                nativeMenuBar.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
            }
            var normalWorkspace = main.ClientSize;
            try
            {
                main.ClientSize = new System.Drawing.Size(260, normalWorkspace.Height);
                Avalonia.Controls.Documents.TextElement.SetFontSize(nativeMenuBar, menuFont * 2.25);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
                var lastMenu = nativeMenuBar.Items.OfType<Avalonia.Controls.MenuItem>().Last(item => item.IsVisible);
                menuViewport.Offset = new Vector(menuViewport.Extent.Width - menuViewport.Viewport.Width, 0);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
                var bar = menuViewport.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ScrollBar>()
                    .Single(control => control.Orientation == Avalonia.Layout.Orientation.Horizontal);
                var finalMenuOrigin = lastMenu.TranslatePoint(default, menuViewport);
                var barOrigin = bar.TranslatePoint(default, menuViewport);
                check(bar.IsEffectivelyVisible && menuViewport.Offset.X > 0 &&
                    finalMenuOrigin.HasValue && barOrigin.HasValue &&
                    finalMenuOrigin.Value.X + lastMenu.Bounds.Width <= menuViewport.Viewport.Width + 1 &&
                    finalMenuOrigin.Value.Y + lastMenu.Bounds.Height <= barOrigin.Value.Y &&
                    menuProjection.Bottom <= toolbarProjection.Top,
                    $"A narrow workspace scrolls to the final menu without covering its items [menu={finalMenuOrigin}/{lastMenu.Bounds}, bar={barOrigin}/{bar.Bounds}, viewport={menuViewport.Viewport}, offset={menuViewport.Offset}]");
            }
            finally
            {
                nativeMenuBar.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                main.ClientSize = normalWorkspace;
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
            }
            check((string?)nativeEntry.Header == "_Synthetic menu" && nativeEntry.InputGesture?.Key == Avalonia.Input.Key.F9,
                "Workspace menu presents access keys and the existing shortcut");
            nativeEntry.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.MenuItem.ClickEvent) { Source = nativeEntry });
            nativeButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            check(menuInvocations == 1 && toolInvocations == 1, $"Native workspace actions invoke their existing shared commands [menu={menuInvocations}; tool={toolInvocations}; owner={main.Enabled}; window={main.ToAvaloniaWindow().IsEnabled}; menuVisible={testMenu.Visible}; file={file.Visible}/{file.Enabled}; entry={nativeEntry.IsEffectivelyVisible}/{nativeEntry.IsEnabled}; toolVisible={testTool.Visible}]");
            testMenu.Enabled = testTool.Enabled = false;
            nativeEntry.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.MenuItem.ClickEvent) { Source = nativeEntry });
            nativeButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            check(!nativeEntry.IsEnabled && !nativeButton.IsEnabled && menuInvocations == 1 && toolInvocations == 1,
                "Disabled workspace commands update immediately and reject queued clicks");
            fileHeader.IsSubMenuOpen = false;
            int originalToolbarHeight = toolbarProjection.Height;
            var extraTools = Enumerable.Range(1, 14).Select(index => new ToolStripButton
            {
                Text = "Synthetic toolbar command " + index
            }).ToArray();
            try
            {
                foreach (var item in extraTools)
                    legacyToolbar.Items.Add(item);
                await Task.Delay(60);
                var wrapped = nativeToolbar.Children.OfType<Avalonia.Controls.Button>()
                    .Where(button => button.Tag is ToolStripButton item && extraTools.Contains(item)).ToArray();
                check(wrapped.Length == extraTools.Length && wrapped.Select(button => button.Bounds.Top).Distinct().Count() > 1 &&
                    toolbarProjection.Height > originalToolbarHeight &&
                    wrapped.All(button => button.Bounds.Right <= toolbarProjection.Width && button.Bounds.Bottom <= toolbarProjection.Height),
                    "Workspace toolbar wraps all commands into visible rows when they overflow");
                int wrappedHeight = toolbarProjection.Height;
                double normalFont = wrapped[0].FontSize;
                Avalonia.Controls.Documents.TextElement.SetFontSize(nativeToolbar, normalFont * 1.5);
                await Task.Delay(60);
                check(toolbarProjection.Height > wrappedHeight && wrapped[0].FontSize >= normalFont * 1.5 &&
                    wrapped.All(button => button.Bounds.Right <= toolbarProjection.Width &&
                        button.Bounds.Bottom <= toolbarProjection.Height),
                    "Workspace toolbar remeasures and keeps every wrapped command visible at 150% text");

            }
            finally
            {
                nativeToolbar.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                foreach (var item in extraTools)
                {
                    legacyToolbar.Items.Remove(item);
                    item.Dispose();
                }
                await Task.Delay(60);
            }
            check(toolbarProjection.Height == originalToolbarHeight, "Workspace releases the extra toolbar rows when commands are removed");
        }
        finally
        {
            file.DropDownItems.Remove(testMenu);
            legacyToolbar.Items.Remove(testTool);
            menuProjection.Visible = showMenus;
            await Task.Delay(30);
        }

        Character Copy() { var copy = new Character(); copy.LoadSnapshot(original); return copy; }
        using var first = new frmCreate(Copy()) { Loading = true, MdiParent = main };
        var careerCharacter = Copy(); careerCharacter.Created = true;
        using var second = new frmCareer(careerCharacter) { Loading = true, MdiParent = main };
        var tabs = main.DocumentTabs.NativeTabs!;
        var toolbar = Field<ToolStrip>(main, "toolStrip");
        var menus = Field<MenuStrip>(main, "menuStrip");
        var save = Field<ToolStripMenuItem>(main, "_mnuSaveCharacter");
        var close = Field<ToolStripMenuItem>(main, "_mnuCloseCharacter");
        var previousClipboardType = GlobalOptions.Instance.ClipboardContentType;
        first.Show(); second.Show();
        try
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { },
                Avalonia.Threading.DispatcherPriority.ApplicationIdle);
            var firstHeader = tabs.Items.OfType<Avalonia.Controls.TabItem>().Single(tab => ReferenceEquals(tab.Tag, first));
            var secondHeader = tabs.Items.OfType<Avalonia.Controls.TabItem>().Single(tab => ReferenceEquals(tab.Tag, second));
            check(tabs.ItemCount == 2 && tabs.IsEffectivelyVisible && tabs.SelectedItem == secondHeader &&
                firstHeader.Bounds.Width > 0 && secondHeader.Bounds.Height > 0,
                "Native document strip shows both measured headers and selects the active editor");
            var viewport = Field<NativeControlHost>(main, "tabForms").NativeControl as Avalonia.Controls.ScrollViewer
                ?? throw new InvalidOperationException("Missing native document viewport.");
            var documentHost = Field<NativeControlHost>(main, "tabForms");
            int normalHeight = documentHost.Height;
            double normalFont = secondHeader.FontSize;
            Avalonia.Controls.Documents.TextElement.SetFontSize(viewport, normalFont * 2.25);
            viewport.MaxWidth = 60;
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { },
                Avalonia.Threading.DispatcherPriority.ApplicationIdle);
            var scrollbar = viewport.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ScrollBar>()
                .Single(bar => bar.Orientation == Avalonia.Layout.Orientation.Horizontal);
            var scrollbarOrigin = scrollbar.TranslatePoint(default, viewport);
            var headerOrigin = secondHeader.TranslatePoint(default, viewport);
            check(documentHost.Height > normalHeight && secondHeader.FontSize == normalFont * 2.25 &&
                scrollbar.IsEffectivelyVisible && scrollbarOrigin.HasValue && headerOrigin.HasValue &&
                headerOrigin.Value.Y + secondHeader.Bounds.Height <= scrollbarOrigin.Value.Y,
                $"The workspace document scrollbar sits below the clickable headers [viewport={viewport.Viewport}, extent={viewport.Extent}, bar={scrollbar.IsEffectivelyVisible}/{scrollbarOrigin}/{scrollbar.Bounds}, header={headerOrigin}/{secondHeader.Bounds}]");
            viewport.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
            viewport.ClearValue(Avalonia.Controls.ScrollViewer.MaxWidthProperty);
            string originalAlias = first.DocumentCharacter.Alias;
            string oldTitle = (string)secondHeader.Header!;
            first.DocumentCharacter.Alias = "Inactive synthetic alias";
            check((string?)firstHeader.Header == "Inactive synthetic alias" && (string?)secondHeader.Header == oldTitle &&
                main.ActiveMdiChild == second, "Renaming an inactive document updates only its header");
            first.DocumentCharacter.Alias = originalAlias;

            var statusHosts = main.Controls.OfType<NativeControlHost>().Where(host => host.Name == "NativeCharacterStatus").ToArray();
            check(statusHosts.Length == 2 && statusHosts.Count(host => host.Visible) == 1,
                "The workspace shows only the active editor's native status bar");
            var careerStatus = (Avalonia.Controls.Border)statusHosts.Single(host => host.Visible).NativeControl!;
            var statusFields = ((Avalonia.Controls.WrapPanel)careerStatus.Child!).Children.OfType<Avalonia.Controls.TextBlock>().ToArray();
            var karma = Field<ToolStripStatusLabel>(second, "tssKarma");
            var nativeKarma = statusFields.Single(field => field.Name == "NativetssKarma");
            string oldKarma = karma.Text;
            karma.Text = "42";
            karma.Visible = false;
            check(nativeKarma.Text == "42" && !nativeKarma.IsVisible, "Native status fields mirror updates and hidden state");
            karma.Visible = true;
            karma.Text = oldKarma;
            check(nativeKarma.Text == oldKarma && nativeKarma.IsVisible && careerStatus.Bounds.Height > 0,
                "Native status restores the original value in a measured bar");
            int statusHeight = statusHosts.Single(host => host.Visible).Height;
            double statusFont = nativeKarma.FontSize;
            Avalonia.Controls.Documents.TextElement.SetFontSize(careerStatus, statusFont * 1.5);
            karma.Text = string.Join(" ", Enumerable.Repeat("Translated remaining karma", 18));
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
            var statusLayout = (Avalonia.Controls.WrapPanel)careerStatus.Child!;
            check(nativeKarma.FontSize == statusFont * 1.5 && nativeKarma.Bounds.Height > nativeKarma.FontSize &&
                statusHosts.Single(host => host.Visible).Height > statusHeight &&
                statusFields.Where(field => field.IsVisible).All(field =>
                    field.Bounds.Right <= statusLayout.Bounds.Width + 1 && field.Bounds.Bottom <= statusLayout.Bounds.Height + 1),
                $"The expanded status bar keeps larger wrapping balances visible [height={statusHeight}/{careerStatus.Bounds.Height}, field={nativeKarma.Bounds}, panel={statusLayout.Bounds}]");
            careerStatus.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
            karma.Text = oldKarma;
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);


            // Open the in-window Window menu even on Mac, where the normal menu lives in the system bar.
            menuProjection.Visible = true;
            menuProjection.SyncNativeControl();
            await Task.Delay(30);
            var windows = Field<ToolStripMenuItem>(main, "windowsMenu");
            var windowHeader = nativeMenuBar.Items.OfType<Avalonia.Controls.MenuItem>()
                .Single(item => ReferenceEquals(item.Tag, windows));
            windowHeader.IsSubMenuOpen = true;
            await Task.Delay(40);
            var listed = windowHeader.Items.OfType<Avalonia.Controls.MenuItem>()
                .Where(item => item.Tag is ToolStripMenuItem { Name: "" }).ToArray();
            check(windowHeader.IsSubMenuOpen && listed.Length == 2 && listed.SingleOrDefault(item => item.IsChecked)?.Tag is ToolStripMenuItem current &&
                current.Text == second.Text,
                "The native Window menu builds a current document list and marks its active entry");
            windowHeader.IsSubMenuOpen = false;
            menuProjection.Visible = !System.OperatingSystem.IsMacOS();
            menuProjection.SyncNativeControl();

            var packsTab = Field<TabControl>(first, "tabStreetGearTabs").TabPages.Cast<TabPage>().Single(page => page.Name == "tabPacks");
            check(packsTab.Text == "PACKS" && packsTab.Controls.OfType<Button>().Count() == 2,
                "Native Street Gear exposes PACKS browsing/import and creation commands");
            // Clipboard contents are shared across editors, unlike selection state.
            // Returning to an unchanged tab must re-evaluate its edit commands.
            GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Gear;
            Field<TabControl>(first, "tabCharacterTabs").SelectedTab = Field<TabPage>(first, "tabStreetGear");
            Field<TabControl>(first, "tabStreetGearTabs").SelectedTab = Field<TabPage>(first, "tabGear");
            first.Select(); second.Select();
            GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Lifestyle;
            first.Select();
            check(!Field<ToolStripMenuItem>(first, "mnuEditPaste").Enabled && !Field<ToolStripButton>(first, "tsbPaste").Enabled,
                "Returning to a character disables stale Paste after incompatible clipboard changes");
            second.Select();
            GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Gear;
            first.Select();
            check(Field<ToolStripMenuItem>(first, "mnuEditPaste").Enabled && Field<ToolStripButton>(first, "tsbPaste").Enabled,
                "Returning to a character enables compatible Paste in both menu and toolbar");
            for (int iteration = 0; iteration < 5; iteration++)
            {
                first.Select(); await Task.Delay(20);
                check(main.ActiveMdiChild == first && tabs.SelectedItem is Avalonia.Controls.TabItem { Tag: var active } && ReferenceEquals(active, first) &&
                    toolbar.Items.Contains(Field<ToolStripButton>(first, "tsbSave")) &&
                    !toolbar.Items.Contains(Field<ToolStripButton>(second, "tsbSave")) && save.Enabled && close.Enabled,
                    "Native repeated character switch keeps tab, toolbar and commands aligned " + iteration);
                check(nativeToolbar.Children.OfType<Avalonia.Controls.Button>().Any(button =>
                    ReferenceEquals(button.Tag, Field<ToolStripButton>(first, "tsbSave"))) &&
                    !nativeToolbar.Children.OfType<Avalonia.Controls.Button>().Any(button =>
                    ReferenceEquals(button.Tag, Field<ToolStripButton>(second, "tsbSave"))),
                    "Native toolbar projects only the active editor's Save command " + iteration);
                check(menus.Items.Contains(Field<ToolStripMenuItem>(first, "mnuCreateSpecial")) &&
                    menus.Items.Contains(Field<ToolStripMenuItem>(first, "mnuCreateEdit")) &&
                    !menus.Items.Contains(Field<ToolStripMenuItem>(second, "mnuCreateSpecial")) &&
                    menus.Items.IndexOf(Field<ToolStripMenuItem>(main, "fileMenu")) <
                        menus.Items.IndexOf(Field<ToolStripMenuItem>(first, "mnuCreateEdit")),
                    "Native creation menu ownership follows active character " + iteration);
                if (System.OperatingSystem.IsMacOS())
                {
                    var nativeMenu = Avalonia.Controls.NativeMenu.GetMenu(main.ToAvaloniaWindow())!;
                    check(nativeMenu.Items.OfType<Avalonia.Controls.NativeMenuItem>().Take(2).Select(item => item.Header)
                        .SequenceEqual(new[] { "File", "Edit" }) &&
                        nativeMenu.Items.OfType<Avalonia.Controls.NativeMenuItem>().Any(item => item.Header == "Special" &&
                        item.Menu!.Items.OfType<Avalonia.Controls.NativeMenuItem>().Any(command => command.Header == "Add PACKS Kit")),
                        "Mac native Special menu exposes PACKS after character switch " + iteration);
                }
                tabs.SelectedItem = secondHeader;
                await Task.Delay(20);
                check(main.ActiveMdiChild == second && toolbar.Items.Contains(Field<ToolStripButton>(second, "tsbSave")),
                    "Native tab selection activates the Career document and its toolbar " + iteration);
            }
            // Closing an inactive editor must not undo the active editor's merge.
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(first, false);
            first.Close();
            check(main.ActiveMdiChild == second && tabs.SelectedItem is Avalonia.Controls.TabItem { Tag: var survivor } && ReferenceEquals(survivor, second) &&
                tabs.ItemCount == 1 && !main.DocumentTabs.Visible &&
                toolbar.Items.Contains(Field<ToolStripButton>(second, "tsbSave")) && save.Enabled &&
                menus.Items.IndexOf(Field<ToolStripMenuItem>(main, "fileMenu")) <
                    menus.Items.IndexOf(Field<ToolStripMenuItem>(second, "mnuCreateEdit")),
                "Native inactive character close retains the active save toolbar and menu");
            // Invoke the shared Close Character command, rather than closing the form directly.
            typeof(frmCareer).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(second, false);
            close.PerformClick();
            check(!main.Controls.OfType<NativeControlHost>().Any(host => host.Name == "NativeCharacterStatus"),
                "Closed editors release both native status bars");
            check(second.IsDisposed && main.ActiveMdiChild == null && tabs.ItemCount == 0 &&
                !menus.Items.OfType<ToolStripMenuItem>().Any(item => item.Name == "mnuCreateSpecial" || item.Name == "mnuCreateEdit") &&
                !save.Enabled && !close.Enabled && !toolbar.Items.Contains(Field<ToolStripButton>(second, "tsbSave")) &&
                !toolbar.Items.OfType<ToolStripButton>().Any(item => item.Name == "tsbSave" && item.Visible),
                "Native Close Character clears final tab, toolbar and document command availability");
        }
        finally
        {
            GlobalOptions.Instance.ClipboardContentType = previousClipboardType;
            foreach (var editor in new Form[] { first, second })
                if (!editor.IsDisposed)
                {
                    editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
                    editor.Close();
                }
        }
        var overflow = Enumerable.Range(1, 7).Select(index => new Form
        {
            MdiParent = main,
            Text = $"Synthetic document {index} with a long title for tab overflow"
        }).ToArray();
        try
        {
            foreach (var document in overflow)
                document.Show();
            await Task.Delay(100);
            var viewport = (Avalonia.Controls.ScrollViewer)Field<NativeControlHost>(main, "tabForms").NativeControl!;
            check(tabs.ItemCount == 7 && viewport.Extent.Width > viewport.Viewport.Width && viewport.Offset.X > 0,
                "The workspace scrolls to reveal an active header when document tabs overflow");
            overflow[0].Select();
            await Task.Delay(50);
            check(viewport.Offset.X < 1 && ReferenceEquals(main.ActiveMdiChild, overflow[0]),
                "Activating the first document scrolls its header back into view");
        }
        finally
        {
            foreach (var document in overflow)
            {
                document.Close();
                document.Dispose();
            }
        }
        check(character.CaptureSnapshot().SequenceEqual(original), "Native character switching leaves the source character unchanged");
    }
}
