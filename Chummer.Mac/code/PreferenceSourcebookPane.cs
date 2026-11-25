#nullable enable
using System;
using System.Drawing;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmOptions
{
    private TextBox? nativeSettingName;

    private void InitializeSourcebookPane()
    {
        var sourcebookTree = NativeEditorTree.Attach(this, treSourcebook, "NativetreSourcebook", label1.Text);
        if (sourcebookTree == null)
            return;

        ScrollViewer.SetHorizontalScrollBarVisibility(sourcebookTree, ScrollBarVisibility.Disabled);
        sourcebookTree.Styles.Add(new Style(selector => selector.OfType<CheckBox>())
        {
            Setters =
            {
                new Setter(CheckBox.ContentTemplateProperty, new FuncDataTemplate<string>((text, _) =>
                    new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }))
            }
        });
        var tree = tabGeneral.Controls.OfType<Forms.NativeControlHost>().Single(host => host.Name == "NativetreSourcebook");
        var disclosureText = MakeCaption();
        var paneText = MakeCaption();
        var toggle = new CheckBox
        {
            Content = disclosureText,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        NativeControlPresentation.AddTheme(toggle.Styles);
        var disclosure = new Forms.NativeControlHost { Name = "NativeSourcebookDisclosure", NativeControl = toggle };
        var caption = new Forms.NativeControlHost
        {
            Name = "NativeSourcebookCaption",
            NativeControl = paneText
        };
        tabGeneral.Controls.Remove(label1);
        tabGeneral.Controls.Add(disclosure);
        tabGeneral.Controls.Add(caption);
        var (header, footer) = CreatePreferenceShell();
        tabControl1.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new Size(440, 460);
        bool expanded = false;
        bool layoutQueued = false;
        Control[] typographyRoots = [(Control)header.NativeControl!, (Control)footer.NativeControl!, toggle, paneText];
        foreach (var root in typographyRoots)
            root.PropertyChanged += FontChanged;

        toggle.IsCheckedChanged += (_, _) =>
        {
            expanded = toggle.IsChecked == true;
            Arrange(null, EventArgs.Empty);
        };
        tabGeneral.SizeChanged += Arrange;
        tabGeneral.VisibleChanged += Arrange;
        SizeChanged += ArrangeWindow;
        Shown += ScheduleProfileLayout;
        cmdOK.TextChanged += ArrangeWindow;
        lblSetting.TextChanged += ScheduleProfileLayout;
        lblSettingName.TextChanged += ScheduleProfileLayout;
        label1.TextChanged += RefreshCaption;
        Disposed += (_, _) =>
        {
            foreach (var root in typographyRoots)
                root.PropertyChanged -= FontChanged;
            tabGeneral.SizeChanged -= Arrange;
            tabGeneral.VisibleChanged -= Arrange;
            SizeChanged -= ArrangeWindow;
            Shown -= ScheduleProfileLayout;
            cmdOK.TextChanged -= ArrangeWindow;
            lblSetting.TextChanged -= ScheduleProfileLayout;
            lblSettingName.TextChanged -= ScheduleProfileLayout;
            label1.TextChanged -= RefreshCaption;
            disclosure.NativeControl = null;
            caption.NativeControl = null;
            label1.Dispose();
        };
        RefreshCaption(null, EventArgs.Empty);
        ArrangeWindow(null, EventArgs.Empty);

        static TextBlock MakeCaption() => new()
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            MaxLines = 3
        };

        void RefreshCaption(object? sender, EventArgs args)
        {
            string text = label1.Text;
            disclosureText.Text = paneText.Text = text;
            ToolTip.SetTip(toggle, text);
            ToolTip.SetTip(paneText, text);
            Avalonia.Automation.AutomationProperties.SetName(toggle, text);
            Avalonia.Automation.AutomationProperties.SetName(sourcebookTree, text);
            Arrange(sender, args);
        }

        void FontChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == TextElement.FontSizeProperty || args.Property == TextElement.FontFamilyProperty)
                ScheduleProfileLayout(sender, EventArgs.Empty);
        }

        void ScheduleProfileLayout(object? sender, EventArgs args)
        {
            if (layoutQueued)
                return;

            layoutQueued = true;
            Dispatcher.UIThread.Post(() =>
            {
                layoutQueued = false;
                if (!IsDisposed)
                    ArrangeWindow(sender, args);
            }, DispatcherPriority.Loaded);
        }

        void ArrangeWindow(object? sender, EventArgs args)
        {
            const int gap = 8;
            int width = Math.Max(0, ClientSize.Width - gap * 2);
            var profile = (Grid)header.NativeControl!;
            bool compact = width < 500;
            var nameField = profile.Children[1];
            if (compact != (Grid.GetRow(nameField) == 1))
            {
                profile.ColumnDefinitions = new ColumnDefinitions(compact ? "*" : "*,*");
                Grid.SetColumn(nameField, compact ? 0 : 1);
                Grid.SetRow(nameField, compact ? 1 : 0);
            }
            int headerHeight = Measure(header, width);
            int footerHeight = Measure(footer, width);
            header.Bounds = new Rectangle(gap, gap, width, headerHeight);
            footer.Bounds = new Rectangle(gap, Math.Max(0, ClientSize.Height - footerHeight - gap), width, footerHeight);
            int tabTop = header.Bottom + gap;
            tabControl1.Bounds = new Rectangle(0, tabTop, ClientSize.Width, Math.Max(0, footer.Top - tabTop - gap));
            header.SyncNativeControl();
            footer.SyncNativeControl();
            Arrange(sender, args);
        }

        void Arrange(object? sender, EventArgs args)
        {
            const int gap = 8;
            int width = Math.Max(0, tabGeneral.ClientSize.Width - gap * 2);
            int height = Math.Max(0, tabGeneral.ClientSize.Height - gap * 2);
            bool narrow = width < 700;
            disclosure.Visible = narrow;
            caption.Visible = !narrow;
            tree.Visible = !narrow || expanded;
            if (narrow)
            {
                const int minimumGeneralArea = 96;
                const int preferredMinimumTreeHeight = 80;
                const int minimumVisibleTreeHeight = 60;
                disclosureText.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
                disclosureText.MaxLines = 3;
                int disclosureHeight = Math.Max(32, Measure(disclosure, width));
                if (expanded && height - disclosureHeight - gap - minimumGeneralArea < minimumVisibleTreeHeight)
                {
                    disclosureText.TextWrapping = Avalonia.Media.TextWrapping.NoWrap;
                    disclosureText.MaxLines = 1;
                    var line = new TextBlock { Text = "Mg", FontSize = toggle.FontSize, FontFamily = toggle.FontFamily };
                    line.Measure(Avalonia.Size.Infinity);
                    disclosureHeight = Math.Max(32, (int)Math.Ceiling(line.DesiredSize.Height) + 8);
                }
                int treeHeight = expanded
                    ? Math.Min(Math.Max(preferredMinimumTreeHeight, height / 3),
                        Math.Max(0, height - disclosureHeight - gap - minimumGeneralArea))
                    : 0;
                disclosure.Bounds = new Rectangle(gap, gap, width, disclosureHeight);
                tree.Bounds = new Rectangle(gap, gap + disclosureHeight, width, treeHeight);
                int tabOffset = disclosureHeight + treeHeight + gap;
                tabControl2.Bounds = new Rectangle(gap, gap + tabOffset, width, Math.Max(0, height - tabOffset));

            }
            else
            {
                int paneWidth = Math.Clamp(width / 3, 210, 280);
                int captionHeight = Math.Max(20, Measure(caption, paneWidth));
                caption.Bounds = new Rectangle(gap, gap, paneWidth, captionHeight);
                tree.Bounds = new Rectangle(gap, gap + captionHeight + gap, paneWidth,
                    Math.Max(0, height - captionHeight - gap));
                tabControl2.Bounds = new Rectangle(gap * 2 + paneWidth, gap, Math.Max(0, width - paneWidth - gap), height);
            }
            Dispatcher.UIThread.Post(() =>
            {
                if (IsDisposed)
                    return;
                disclosure.SyncNativeControl();
                caption.SyncNativeControl();
                tree.SyncNativeControl();
            }, DispatcherPriority.Loaded);
        }

        static int Measure(Forms.NativeControlHost host, int width)
        {
            var element = (Control)host.NativeControl!;
            // The presentation host sets explicit bounds after measuring.
            // Clear the previous arranged size so it cannot constrain this measure.
            element.ClearValue(Layoutable.WidthProperty);
            element.ClearValue(Layoutable.HeightProperty);
            element.InvalidateMeasure();
            element.Measure(new Avalonia.Size(width, double.PositiveInfinity));
            return (int)Math.Ceiling(element.DesiredSize.Height);
        }
    }

    private (Forms.NativeControlHost Header, Forms.NativeControlHost Footer) CreatePreferenceShell()
    {
        Forms.Control[] sources = [cboSetting, lblSetting, txtSettingName, lblSettingName, cmdOK];
        foreach (var source in sources)
            Controls.Remove(source);

        var profiles = NativeListSelection.Attach(this, cboSetting, lblSetting.Text, layoutManaged: true)!;
        var name = new NativeInlineTextBinding(txtSettingName, () => true);
        nativeSettingName = name.Input;
        nativeSettingName.Name = "NativeSettingName";
        nativeSettingName.ClearValue(TextBox.FontSizeProperty);
        nativeSettingName.ClearValue(TextBox.MinHeightProperty);
        Avalonia.Automation.AutomationProperties.SetName(nativeSettingName, lblSettingName.Text);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 6
        };
        AddProfileField(lblSetting, profiles, 0);
        AddProfileField(lblSettingName, nativeSettingName, 1);
        var header = Host("NativePreferenceProfile", grid);

        var buttonText = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, TextAlignment = Avalonia.Media.TextAlignment.Center };
        var button = new Button { Name = "NativePreferenceOK", HorizontalAlignment = HorizontalAlignment.Center, Content = buttonText };
        void RefreshOK()
        {
            buttonText.Text = cmdOK.Text;
            button.IsEnabled = cmdOK.Enabled;
            Avalonia.Automation.AutomationProperties.SetName(button, cmdOK.Text);
        }
        void RefreshCaption(object? sender, EventArgs args) => RefreshOK();
        cmdOK.TextChanged += RefreshCaption;
        var observer = new NativeControlObservation(cmdOK, RefreshOK);
        button.Click += (_, _) => cmdOK.PerformClick();
        RefreshOK();
        var footer = Host("NativePreferenceFooter", new Border { Child = button });
        Disposed += (_, _) =>
        {
            header.NativeControl = null;
            footer.NativeControl = null;
            cmdOK.TextChanged -= RefreshCaption;
            observer.Dispose();
            name.Dispose();
            nativeSettingName = null;
            foreach (var source in sources)
                source.Dispose();
        };
        return (header, footer);

        void AddProfileField(Forms.Label source, Control input, int column)
        {
            var caption = new TextBlock
            {
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                MaxLines = 2
            };
            void RefreshProfileCaption(object? sender, EventArgs args)
            {
                caption.Text = source.Text;
                ToolTip.SetTip(caption, source.Text);
                Avalonia.Automation.AutomationProperties.SetName(input, source.Text);
            }
            source.TextChanged += RefreshProfileCaption;
            Disposed += (_, _) => source.TextChanged -= RefreshProfileCaption;
            RefreshProfileCaption(null, EventArgs.Empty);
            var stack = new StackPanel { Spacing = 4, Children = { caption, input } };
            Grid.SetColumn(stack, column);
            grid.Children.Add(stack);
        }

        Forms.NativeControlHost Host(string hostName, Control content)
        {
            NativeControlPresentation.AddTheme(content.Styles);
            var host = new Forms.NativeControlHost { Name = hostName, NativeControl = content };
            Controls.Add(host);
            return host;
        }
    }
}
