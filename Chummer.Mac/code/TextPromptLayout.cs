#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmSelectText
{
    private void InitializeNativeLayout()
    {
        if (Application.Current == null)
            return;

        var controllers = Controls.Cast<Forms.Control>().ToArray();
        var dice = controllers.OfType<Forms.Button>().SingleOrDefault(button => button.Name == "cmdRandomPersonName");
        foreach (var controller in controllers)
            Controls.Remove(controller);

        var description = Label(lblDescription.Text);
        description.Name = "NativePromptDescription";
        void UpdateDescription(object? sender, EventArgs args)
        {
            description.Text = lblDescription.Text;
            description.IsVisible = !string.IsNullOrWhiteSpace(description.Text);
        }
        lblDescription.TextChanged += UpdateDescription;
        UpdateDescription(null, EventArgs.Empty);

        Control input;
        if (detailChoices != null)
        {
            var template = new FuncDataTemplate<DetailChoice>((item, _) =>
            {
                var text = Label(item?.ToString() ?? "");
                if (item?.Review == true)
                {
                    text.FontStyle = FontStyle.Italic;
                    text.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlErrorTextForegroundBrush"));
                }
                return text;
            });
            input = NativeListSelection.Attach(this, detailChoices, Text, search: detailSearch, itemTemplate: template, layoutManaged: true)!;
        }
        else
        {
            var binding = new NativeInlineTextBinding(txtValue, () => true);
            input = binding.Input;
            input.Name = "NativePromptValue";
            AutomationProperties.SetName(input, Text);
            Disposed += (_, _) => binding.Dispose();
        }

        var fields = new StackPanel { Spacing = 6, Children = { description, input } };
        if (dice != null)
        {
            var random = Command(dice, LanguageManager.Instance.GetString("PersonNames_Roll"));
            random.Click += (_, _) => input.Focus();
            fields.Children.Add(random);
        }
        var scroll = new ScrollViewer
        {
            Content = fields, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
        var actions = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4,
            Children = { Command(cmdCancel, cmdCancel.Text), Command(cmdOK, cmdOK.Text) }
        };
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 10 };
        layout.Children.Add(scroll);
        Grid.SetRow(actions, 1);
        layout.Children.Add(actions);
        var surface = new Border { Child = layout };
        NativeControlPresentation.AddTheme(surface.Styles);
        var host = new Forms.NativeControlHost { Name = "NativeTextPrompt", Dock = Forms.DockStyle.Fill, NativeControl = surface };
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(270, 165);
        ClientSize = new System.Drawing.Size(460, 240);
        Shown += (_, _) =>
        {
            if (DialogResult == Forms.DialogResult.None)
                input.Focus();
        };
        Disposed += (_, _) =>
        {
            lblDescription.TextChanged -= UpdateDescription;
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        static Button Command(Forms.Button controller, string caption)
        {
            var command = new Button
            {
                Name = "Native" + controller.Name, Content = Label(caption),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            AutomationProperties.SetName(command, caption);
            command.Click += (_, _) => controller.PerformClick();
            return command;
        }

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
