#nullable enable
using System.Collections.Generic;
using System.Drawing;
using Majorsilence.Forms;
using Native = Avalonia.Controls;

namespace Chummer;

internal sealed class FoundryExportDialog : Form
{
    private readonly ComboBox _character = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _version = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    internal int CharacterIndex => _character.SelectedIndex;
    internal FoundryActorExporter.Target Target => _version.SelectedIndex == 1 ? FoundryActorExporter.Target.Version126 : FoundryActorExporter.Target.Version16;

    internal FoundryExportDialog(IReadOnlyList<Character> characters)
    {
        Text = "Export to Foundry"; ClientSize = new Size(580, 255);
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        var characterLabel = new Label { Text = "Character:", AutoSize = false }; characterLabel.SetBounds(16, 18, 125, 26);
        _character.SetBounds(150, 14, 410, 28);
        foreach (var character in characters) _character.Items.Add(string.IsNullOrWhiteSpace(character.Alias) ? character.Name : character.Alias);
        _character.SelectedIndex = 0;
        var versionLabel = new Label { Text = "Foundry SR4 version:", AutoSize = false }; versionLabel.SetBounds(16, 61, 135, 26);
        _version.SetBounds(150, 57, 410, 28);
        _version.Items.Add("1.6 — legacy SR4 system");
        _version.Items.Add("1.26 — current supported SR4 system"); _version.SelectedIndex = 1;
        var help = new Label { AutoSize = false, Text = "Exports one character as an actor JSON file. Supported ware gets an activation effect; unsupported behavior is explained in the item's notes.\n\nImport from the actor's menu in Foundry. Match the version above to your Shadowrun 4e system." };
        help.SetBounds(16, 100, 544, 100);
        var save = new Button { Text = "Export…", DialogResult = DialogResult.OK }; save.SetBounds(320, 214, 115, 28);
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel }; cancel.SetBounds(445, 214, 115, 28);
        Controls.AddRange(new Control[] { characterLabel, _character, versionLabel, _version, help, save, cancel });
        AcceptButton = save; CancelButton = cancel;
        Load += (_, _) =>
        {
            if (Avalonia.Application.Current == null)
                return;

            var characterPicker = NativeListSelection.Attach(this, _character, characterLabel.Text, layoutManaged: true)!;
            var versionPicker = NativeListSelection.Attach(this, _version, versionLabel.Text, layoutManaged: true)!;
            var fields = new Native.StackPanel { Spacing = 6 };
            fields.Children.Add(Wrapped(characterLabel.Text));
            fields.Children.Add(characterPicker);
            fields.Children.Add(Wrapped(versionLabel.Text));
            fields.Children.Add(versionPicker);
            fields.Children.Add(Wrapped(help.Text));
            var content = new Native.ScrollViewer
            {
                Content = fields,
                HorizontalScrollBarVisibility = Native.Primitives.ScrollBarVisibility.Disabled
            };
            NativeControlPresentation.AddTheme(content.Styles);
            var commands = new Native.WrapPanel
            {
                ItemSpacing = 8, LineSpacing = 4, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };
            commands.Children.Add(Command(save, "NativeFoundryExport"));
            commands.Children.Add(Command(cancel, "NativeFoundryCancel"));
            var layout = new Native.DockPanel { LastChildFill = true };
            Native.DockPanel.SetDock(commands, Native.Dock.Bottom);
            commands.Margin = new Avalonia.Thickness(0, 8, 0, 0);
            layout.Children.Add(commands);
            layout.Children.Add(content);
            var surface = new Native.Border { Child = layout, Padding = new Avalonia.Thickness(12) };
            NativeControlPresentation.AddTheme(surface.Styles);
            Control[] controllers = [characterLabel, _character, versionLabel, _version, help, save, cancel];
            foreach (var controller in controllers)
                Controls.Remove(controller);
            Controls.Add(new NativeControlHost { Name = "NativeFoundryExportOptions", NativeControl = surface, Dock = DockStyle.Fill });
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(260, 200);
            Disposed += (_, _) =>
            {
                foreach (var controller in controllers)
                    controller.Dispose();
            };
        };

        static Native.TextBlock Wrapped(string text) => new() { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        Native.Button Command(Button source, string name)
        {
            var button = new Native.Button
            {
                Name = name, Content = Wrapped(source.Text),
                IsDefault = source.DialogResult == DialogResult.OK,
                IsCancel = source.DialogResult == DialogResult.Cancel
            };
            button.Click += (_, _) => DialogResult = source.DialogResult;
            return button;
        }
    }
}
