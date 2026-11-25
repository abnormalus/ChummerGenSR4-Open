#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CharacterNarrativeLayout
{
    internal static void Attach(Forms.Form editor, Forms.TabPage page, Control sidebar,
        (Forms.Label Caption, Forms.Control Source, SearchableChoices? Search, string? HintName)[] identityFields,
        Forms.CheckBox? creationStatus, params (Forms.Label Caption, Forms.TextBox Source)[] fields)
    {
        if (Application.Current == null)
            return;

        const int margin = 8;
        var rows = new StackPanel { Name = "NativeCharacterInfoFields", Spacing = margin, Margin = new Thickness(0, 0, margin, margin) };
        var bindings = new List<NativeInlineTextBinding>();
        var identity = new UniformGrid { Name = "NativeCharacterIdentity", Columns = 1 };
        foreach (var (caption, source, search, hintName) in identityFields)
        {
            page.Controls.Remove(source);
            Control input;
            if (source is Forms.ComboBox palette)
            {
                input = NativeListSelection.Attach(editor, palette, caption.Text, search: search,
                    itemTemplate: AppearanceColors.NativeTemplate, layoutManaged: true)!;
            }
            else
            {
                var binding = new NativeInlineTextBinding((Forms.TextBox)source, Available);
                bindings.Add(binding);
                binding.Input.Name = "Native" + source.Name;
                binding.Input.ClearValue(TextBox.FontSizeProperty);
                input = binding.Input;
                editor.Disposed += (_, _) => binding.Dispose();
            }
            editor.Disposed += (_, _) => source.Dispose();
            var label = MoveLabel(caption);
            Avalonia.Automation.AutomationProperties.SetLabeledBy(input, label);
            var cell = new StackPanel { Spacing = 3, Margin = new Thickness(0, 0, margin, margin), Children = { label, input } };
            if (hintName != null && page.Controls.Find(hintName, false).SingleOrDefault() is Forms.Label hint)
                cell.Children.Add(MoveLabel(hint));
            identity.Children.Add(cell);
        }
        void ReflowIdentity()
        {
            double minimumColumn = Math.Max(120, TextElement.GetFontSize(identity) * 12);
            identity.Columns = Math.Clamp((int)(identity.Bounds.Width / minimumColumn), 1, 4);
        }
        identity.SizeChanged += (_, _) => ReflowIdentity();
        identity.PropertyChanged += (_, args) =>
        {
            if (args.Property == TextElement.FontSizeProperty) ReflowIdentity();
        };
        rows.Children.Add(identity);
        Action? refreshCreationStatus = null;
        if (creationStatus != null)
        {
            page.Controls.Remove(creationStatus);
            var toggle = new CheckBox
            {
                Name = "Native" + creationStatus.Name,
                Content = new TextBlock { Text = creationStatus.Text, TextWrapping = TextWrapping.Wrap }
            };
            void SyncStatus()
            {
                toggle.IsChecked = creationStatus.Checked;
                toggle.IsEnabled = creationStatus.Enabled && Available();
                toggle.IsVisible = creationStatus.Visible;
            }
            var observer = new NativeControlObservation(creationStatus, SyncStatus);
            EventHandler checkedChanged = (_, _) => SyncStatus();
            creationStatus.CheckedChanged += checkedChanged;
            toggle.IsCheckedChanged += (_, _) =>
            {
                if (Available() && creationStatus.Enabled) creationStatus.Checked = toggle.IsChecked == true;
            };
            refreshCreationStatus = SyncStatus;
            editor.Disposed += (_, _) =>
            {
                observer.Dispose();
                creationStatus.CheckedChanged -= checkedChanged;
                creationStatus.Dispose();
            };
            rows.Children.Add(toggle);
        }
        var controllers = fields.Select(field => field.Source).ToArray();
        for (int index = 0; index < fields.Length; index++)
        {
            var (caption, source) = fields[index];
            page.Controls.Remove(source);
            var binding = new NativeInlineTextBinding(source, Available);
            bindings.Add(binding);
            var input = binding.Input;
            input.Name = "Native" + source.Name;
            input.ClearValue(TextBox.FontSizeProperty);
            input.AcceptsReturn = source.Multiline;
            input.TextWrapping = TextWrapping.Wrap;
            input.MinLines = 4;
            input.MaxLines = 8;
            var label = MoveLabel(caption);
            Avalonia.Automation.AutomationProperties.SetLabeledBy(input, label);
            var expand = CharacterDetailsEditor.CreateExpandButton(editor, controllers, index);
            var header = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,2*"), ColumnSpacing = margin };
            expand.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(expand, 1);
            header.Children.Add(label);
            header.Children.Add(expand);
            rows.Children.Add(new StackPanel { Spacing = 3, Children = { header, input } });
            editor.Disposed += (_, _) =>
            {
                binding.Dispose();
                source.Dispose();
            };
        }

        var columns = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto"), RowSpacing = margin * 2 };
        columns.Children.Add(rows);
        columns.Children.Add(sidebar);
        sidebar.VerticalAlignment = VerticalAlignment.Top;
        Grid.SetRow(sidebar, 1);
        bool beside = false;
        columns.SizeChanged += (_, args) =>
        {
            bool wide = args.NewSize.Width >= 680;
            if (wide == beside) return;
            beside = wide;
            columns.ColumnDefinitions = new ColumnDefinitions(wide ? "2*,*" : "*");
            columns.RowDefinitions = new RowDefinitions(wide ? "Auto" : "Auto,Auto");
            columns.ColumnSpacing = wide ? margin * 2 : 0;
            Grid.SetColumn(sidebar, wide ? 1 : 0);
            Grid.SetRow(sidebar, wide ? 0 : 1);
        };

        var viewport = new ScrollViewer
        {
            Content = columns,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        NativeControlPresentation.AddTheme(viewport.Styles);
        var workspace = editor.MdiParent ?? editor;
        var presenter = new CreationRegionPresenter(editor, page, viewport, "NativeCharacterNarrative", () =>
        {
            var size = InventoryTreeLayout.VisiblePageSize(workspace, page);
            return new System.Drawing.Rectangle(margin, margin,
                Math.Max(0, size.Width - 2 * margin),
                Math.Max(0, size.Height - margin));
        }, clipToSource: true);
        page.VisibleChanged += RefreshInputs;
        editor.EnabledChanged += RefreshInputs;
        editor.Shown += RefreshInputs;
        workspace.MdiChildActivate += RefreshInputs;
        editor.Disposed += (_, _) =>
        {
            page.VisibleChanged -= RefreshInputs;
            editor.EnabledChanged -= RefreshInputs;
            editor.Shown -= RefreshInputs;
            workspace.MdiChildActivate -= RefreshInputs;
            presenter.Dispose();
        };

        bool Available() => !editor.IsDisposed && editor.Enabled && page.Visible
            && (editor.MdiParent == null || editor.MdiParent.ActiveMdiChild == editor);

        TextBlock MoveLabel(Forms.Label source)
        {
            page.Controls.Remove(source);
            var label = new TextBlock { Text = source.Text, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
            EventHandler changed = (_, _) => label.Text = source.Text;
            source.TextChanged += changed;
            editor.Disposed += (_, _) =>
            {
                source.TextChanged -= changed;
                source.Dispose();
            };
            return label;
        }

        void RefreshInputs(object? sender, EventArgs e)
        {
            foreach (var binding in bindings)
                binding.Refresh();
            refreshCreationStatus?.Invoke();
        }
        RefreshInputs(null, EventArgs.Empty);
    }
}
