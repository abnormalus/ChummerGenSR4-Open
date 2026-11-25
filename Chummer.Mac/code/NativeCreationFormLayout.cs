#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCreationFormLayout
{
    internal static void Attach(Forms.Form owner, string hostName,
        (Forms.Label Label, Forms.Control Source, string Name)[] inputs, Forms.Button cancel, Forms.Button accept,
        System.Drawing.Size size, Forms.CheckBox[]? options = null, Forms.Button? selection = null, Forms.Label? help = null,
        (Forms.Label Caption, Forms.ListBox List)? results = null, Action<StackPanel>? appendFields = null)
    {
        if (Application.Current == null)
            return;

        var sources = owner.Controls.Cast<Forms.Control>().ToArray();
        foreach (var source in sources)
            owner.Controls.Remove(source);

        var fields = new StackPanel { Spacing = 10 };
        foreach (var (label, source, name) in inputs)
        {
            Control input;
            switch (source)
            {
                case Forms.TextBox text:
                    var binding = new NativeInlineTextBinding(text, () => true);
                    input = binding.Input;
                    binding.Input.IsReadOnly = text.ReadOnly;
                    input.ClearValue(TextBox.FontSizeProperty);
                    input.ClearValue(TextBox.MinHeightProperty);
                    owner.Disposed += (_, _) => binding.Dispose();
                    break;
                case Forms.NumericUpDown number:
                    var numeric = new NativeInlineNumericBinding(number, null, wholeNumbers: number.DecimalPlaces == 0);
                    input = numeric.Input;
                    input.ClearValue(NumericUpDown.WidthProperty);
                    input.ClearValue(NumericUpDown.MinHeightProperty);
                    input.ClearValue(NumericUpDown.FontSizeProperty);
                    input.ClearValue(NumericUpDown.PaddingProperty);
                    owner.Disposed += (_, _) => numeric.Dispose();
                    break;
                case Forms.ComboBox choice:
                    input = NativeListSelection.Attach(owner, choice, label.Text, layoutManaged: true)!;
                    break;
                default:
                    throw new ArgumentException("Unsupported native creation form field.", nameof(inputs));
            }
            AddField(label, source, name, input);
        }
        if (selection != null)
            fields.Children.Add(CreateCommand(selection));
        foreach (var source in options ?? [])
        {
            var toggle = new CheckBox
            {
                Name = "Native" + source.Name,
                IsChecked = source.Checked,
                Content = new TextBlock { Text = source.Text, TextWrapping = TextWrapping.Wrap }
            };
            AutomationProperties.SetName(toggle, source.Text);
            void Refresh()
            {
                toggle.IsChecked = source.Checked;
                toggle.IsVisible = source.Visible;
                toggle.IsEnabled = source.Enabled;
            }
            var observer = new NativeControlObservation(source, Refresh);
            void CheckedChanged(object? sender, EventArgs args) => Refresh();
            source.CheckedChanged += CheckedChanged;
            toggle.IsCheckedChanged += (_, _) => source.Checked = toggle.IsChecked == true;
            fields.Children.Add(toggle);
            Refresh();
            owner.Disposed += (_, _) =>
            {
                observer.Dispose();
                source.CheckedChanged -= CheckedChanged;
            };
        }
        if (help != null)
        {
            var note = new TextBlock { Text = help.Text, TextWrapping = TextWrapping.Wrap };
            void UpdateHelp(object? sender, EventArgs args) => note.Text = help.Text;
            help.TextChanged += UpdateHelp;
            fields.Children.Add(note);
            owner.Disposed += (_, _) => help.TextChanged -= UpdateHelp;
        }
        appendFields?.Invoke(fields);
        var scroll = new ScrollViewer
        {
            Content = fields,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            AllowAutoHide = false,
            BringIntoViewOnFocusChange = true
        };
        NativeControlPresentation.AddTheme(scroll.Styles);

        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        actions.Children.Add(CreateCommand(cancel));
        actions.Children.Add(CreateCommand(accept));
        var layout = new Grid
        {
            RowSpacing = 8,
            RowDefinitions = new RowDefinitions(results.HasValue ? "2*,*,Auto" : "*,Auto")
        };
        layout.Children.Add(scroll);
        if (results is { } output)
        {
            var list = NativeListSelection.Attach(owner, output.List, output.Caption.Text, layoutManaged: true)!;
            list.ClearValue(ListBox.FontSizeProperty);
            var caption = new TextBlock { Text = output.Caption.Text, TextWrapping = TextWrapping.Wrap };
            void UpdateResults(object? sender, EventArgs args) => caption.Text = output.Caption.Text;
            output.Caption.TextChanged += UpdateResults;
            owner.Disposed += (_, _) => output.Caption.TextChanged -= UpdateResults;
            var resultLayout = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 4 };
            resultLayout.Children.Add(caption);
            Grid.SetRow(list, 1);
            resultLayout.Children.Add(list);
            Grid.SetRow(resultLayout, 1);
            layout.Children.Add(resultLayout);
        }
        Grid.SetRow(actions, results.HasValue ? 2 : 1);
        layout.Children.Add(actions);
        layout.SizeChanged += (_, args) =>
        {
            if (args.NewSize.Width <= 0) return;
            foreach (var action in actions.Children)
                action.MaxWidth = args.NewSize.Width;
        };
        var surface = new Border { Padding = new Thickness(12), Child = layout };
        NativeControlPresentation.AddTheme(surface.Styles);
        var host = new Forms.NativeControlHost
        {
            Name = hostName,
            Dock = Forms.DockStyle.Fill,
            NativeControl = surface
        };
        owner.Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        owner.FormBorderStyle = Forms.FormBorderStyle.Sizable;
        owner.MinimumSize = new System.Drawing.Size(260, 180);
        owner.ClientSize = size;
        owner.Disposed += (_, _) =>
        {
            host.NativeControl = null;
            foreach (var source in sources)
                source.Dispose();
        };

        Button CreateCommand(Forms.Button command)
        {
            var caption = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap, MaxLines = 3, TextTrimming = TextTrimming.CharacterEllipsis
            };
            var button = new Button { Name = "Native" + command.Name, Content = caption };
            button.Click += (_, _) => command.PerformClick();
            void Refresh()
            {
                caption.Text = command.Text;
                AutomationProperties.SetName(button, caption.Text);
                ToolTip.SetTip(button, caption.Text);
                button.IsVisible = command.Visible;
                button.IsEnabled = command.Enabled;
            }
            var observer = new NativeControlObservation(command, Refresh);
            void CaptionChanged(object? sender, EventArgs args) => Refresh();
            command.TextChanged += CaptionChanged;
            owner.Disposed += (_, _) => { command.TextChanged -= CaptionChanged; observer.Dispose(); };
            Refresh();
            return button;
        }

        void AddField(Forms.Label label, Forms.Control source, string controlName, Control input)
        {
            input.Name = controlName;
            var caption = new TextBlock { TextWrapping = TextWrapping.Wrap };
            void UpdateCaption(object? sender, EventArgs args)
            {
                caption.Text = label.Text;
                AutomationProperties.SetName(input, label.Text);
            }
            label.TextChanged += UpdateCaption;
            owner.Disposed += (_, _) => label.TextChanged -= UpdateCaption;
            UpdateCaption(null, EventArgs.Empty);
            var row = new StackPanel { Spacing = 4, Children = { caption, input } };
            void Refresh() => row.IsVisible = source.Visible;
            var observer = new NativeControlObservation(source, Refresh);
            owner.Disposed += (_, _) => observer.Dispose();
            Refresh();
            fields.Children.Add(row);
        }
    }
}
