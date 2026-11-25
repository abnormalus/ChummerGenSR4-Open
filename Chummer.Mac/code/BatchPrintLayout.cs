#nullable enable
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmPrintMultiple
{
    private void InitializeNativeLayout()
    {
        var host = new Forms.NativeControlHost { Name = "NativeBatchPrint", Dock = Forms.DockStyle.Fill };
        var tree = NativeEditorTree.Attach(this, treCharacters, "NativetreCharacters", Text, layoutHost: host);
        if (tree == null)
            return;

        tree.Styles.Add(new Style(selector => selector.OfType<TreeViewItem>())
        {
            Setters =
            {
                new Setter(TreeViewItem.HeaderTemplateProperty, new FuncDataTemplate<string>((name, _) =>
                    new TextBlock { Text = name, TextTrimming = TextTrimming.CharacterEllipsis }))
            }
        });
        ScrollViewer.SetHorizontalScrollBarVisibility(tree, Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        var actions = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        NativeControlPresentation.ConstrainWrapItems(actions);
        var progress = new ProgressBar { Name = "NativeprgProgress" };
        Avalonia.Automation.AutomationProperties.SetName(progress, "Batch print progress");
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var footer = new StackPanel { Spacing = 8, Children = { actions, progress, status } };
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 12 };
        layout.Children.Add(tree);
        Grid.SetRow(footer, 1);
        layout.Children.Add(footer);
        var surface = new Border { Child = layout };
        NativeControlPresentation.AddTheme(surface.Styles);
        host.NativeControl = surface;
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(280, 250);

        foreach (var source in new[] { cmdSelectCharacter, cmdDelete, cmdPrint })
        {
            Controls.Remove(source);
            var caption = new TextBlock { TextWrapping = TextWrapping.Wrap };
            var button = new Button { Name = "Native" + source.Name, Content = caption };
            void Refresh()
            {
                caption.Text = source.Text;
                Avalonia.Automation.AutomationProperties.SetName(button, source.Text);
                button.IsEnabled = source.Enabled;
            }
            var observer = new NativeControlObservation(source, Refresh);
            void CaptionChanged(object? sender, EventArgs args) => Refresh();
            source.TextChanged += CaptionChanged;
            button.Click += (_, _) => source.PerformClick();
            actions.Children.Add(button);
            Refresh();
            Disposed += (_, _) =>
            {
                source.TextChanged -= CaptionChanged;
                observer.Dispose();
                source.Dispose();
            };
        }

        Controls.Remove(prgProgress);
        Controls.Remove(batchStatus);
        void RefreshProgress()
        {
            progress.Maximum = Math.Max(progress.Maximum, prgProgress.Maximum);
            progress.Minimum = prgProgress.Minimum;
            progress.Maximum = prgProgress.Maximum;
            progress.Value = prgProgress.Value;
        }
        var progressObserver = new NativeControlObservation(prgProgress, RefreshProgress);
        prgProgress.CreateControl();
        RefreshProgress();
        void StatusChanged(object? sender, EventArgs args)
        {
            status.Text = batchStatus.Text;
            status.IsVisible = status.Text.Length != 0;
        }
        batchStatus.TextChanged += StatusChanged;
        StatusChanged(null, EventArgs.Empty);
        Disposed += (_, _) =>
        {
            batchStatus.TextChanged -= StatusChanged;
            progressObserver.Dispose();
            host.NativeControl = null;
            prgProgress.Dispose();
            batchStatus.Dispose();
        };
    }
}
