#nullable enable
using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CommonQualitySection
{
    internal static void Attach(Forms.Form editor, Forms.TreeView controller, string name,
        Forms.Label costCaption, Forms.Label cost, Forms.Label sourceCaption, Forms.Label source, Forms.ToolTip tips,
        Action openSource, Action refreshDetails, Action delete, Action editDetail,
        Func<Forms.TreeNode, System.Drawing.Color> foreground, params Forms.Button[] commands)
    {
        if (Application.Current == null || controller.Parent is not { } page)
            return;

        var workspace = editor.MdiParent ?? editor;
        var layout = new DockPanel { LastChildFill = true };
        var presenter = new CreationRegionPresenter(editor, page, layout, name,
            () =>
            {
                var available = InventoryTreeLayout.VisiblePageSize(workspace, page);
                return new System.Drawing.Rectangle(controller.Left, 6,
                    Math.Clamp(available.Width - controller.Left, 0, Math.Max(0, controller.Width)),
                    Math.Max(0, available.Height - 12));
            },
            clipToSource: true);
        var tree = NativeEditorTree.Attach(editor, controller, name, LanguageManager.Instance.GetString("String_Qualities"),
            refreshDetails, delete, editDetail, foreground, layoutHost: presenter.Host)!;
        ScrollViewer.SetHorizontalScrollBarVisibility(tree, ScrollBarVisibility.Disabled);
        tree.Styles.Add(new Style(selector => selector.OfType<TreeViewItem>())
        {
            Setters =
            {
                new Setter(TreeViewItem.HeaderTemplateProperty, new FuncDataTemplate<string>((text, _) =>
                    new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }))
            }
        });
        var actions = new WrapPanel { ItemSpacing = 5, LineSpacing = 4 };
        foreach (var command in commands)
        {
            page.Controls.Remove(command);
            var caption = new TextBlock { TextWrapping = TextWrapping.Wrap };
            var button = new Button { Name = "Native" + command.Name, Content = caption };
            button.Bind(Button.BackgroundProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("ControlFillColorSecondaryBrush"));
            button.Bind(Button.BorderBrushProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("ControlStrokeColorDefaultBrush"));
            button.BorderThickness = new Thickness(1);
            void Update()
            {
                caption.Text = command.Text.Replace("&", "");
                AutomationProperties.SetName(button, caption.Text);
                button.IsEnabled = command.Enabled;
                button.IsVisible = command.Visible;
            }
            var observation = new NativeControlObservation(command, Update);
            button.Click += (_, _) => command.PerformClick();
            EventHandler changed = (_, _) => Update();
            command.TextChanged += changed;
            actions.Children.Add(button);
            Update();
            editor.Disposed += (_, _) => { command.TextChanged -= changed; observation.Dispose(); command.Dispose(); };
        }

        var labels = new[] { costCaption, cost, sourceCaption, source };
        foreach (var label in labels)
            page.Controls.Remove(label);
        var total = new TextBlock { Name = "NativeQualityCost", TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        var citation = new Button { Name = "NativeQualityCitation", HorizontalAlignment = HorizontalAlignment.Left };
        var citationText = new TextBlock { TextWrapping = TextWrapping.Wrap };
        citation.Content = citationText;
        citation.Click += (_, _) => openSource();
        var details = new WrapPanel { ItemSpacing = 8, LineSpacing = 3, Children = { total, citation } };

        NativeControlPresentation.ConstrainWrapItems(actions);
        NativeControlPresentation.ConstrainWrapItems(details);
        var footer = new ScrollViewer
        {
            Name = "NativeQualityDetails", Content = details, MaxHeight = 90,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        NativeControlPresentation.AddTheme(footer.Styles);
        void UpdateDetails(object? sender, EventArgs args)
        {
            total.Text = costCaption.Text + " " + cost.Text;
            total.IsVisible = !string.IsNullOrWhiteSpace(cost.Text);
            string text = sourceCaption.Text + " " + source.Text;
            citationText.Text = text;
            AutomationProperties.SetName(citation, text);
            citation.IsVisible = !string.IsNullOrWhiteSpace(source.Text);
            footer.IsVisible = total.IsVisible || citation.IsVisible;
            Dispatcher.UIThread.Post(() =>
            {
                if (!editor.IsDisposed)
                    ToolTip.SetTip(citation, tips.GetToolTip(source));
            }, DispatcherPriority.Loaded);
        }

        actions.Margin = new Thickness(0, 0, 0, 5);
        footer.Margin = new Thickness(0, 5, 0, 0);
        DockPanel.SetDock(actions, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        layout.Children.Add(actions);
        layout.Children.Add(footer);
        layout.Children.Add(tree);
        cost.TextChanged += UpdateDetails;
        source.TextChanged += UpdateDetails;
        UpdateDetails(null, EventArgs.Empty);
        NativeControlPresentation.AddTheme(layout.Styles);
        void Resize(object? sender, EventArgs args) => presenter.Refresh();
        controller.SizeChanged += Resize;
        controller.LocationChanged += Resize;
        editor.Disposed += (_, _) =>
        {
            controller.SizeChanged -= Resize;
            controller.LocationChanged -= Resize;
            cost.TextChanged -= UpdateDetails;
            source.TextChanged -= UpdateDetails;
            presenter.Dispose();
            foreach (var label in labels)
                label.Dispose();
        };
    }
}
