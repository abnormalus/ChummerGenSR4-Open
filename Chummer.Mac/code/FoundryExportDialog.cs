#nullable enable
using System.Collections.Generic;
using System.Drawing;
using Majorsilence.Forms;

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
        _version.Items.Add("1.6 — matches the supplied actor samples");
        _version.Items.Add("1.26 — newer SR4 system"); _version.SelectedIndex = 0;
        var help = new Label { AutoSize = false, Text = "Exports one character as an actor JSON file. Supported ware gets an activation effect; unsupported behavior is explained in the item's notes.\n\nImport from the actor's menu in Foundry. Match the version above to your Shadowrun 4e system." };
        help.SetBounds(16, 100, 544, 100);
        var save = new Button { Text = "Export…", DialogResult = DialogResult.OK }; save.SetBounds(320, 214, 115, 28);
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel }; cancel.SetBounds(445, 214, 115, 28);
        Controls.AddRange(new Control[] { characterLabel, _character, versionLabel, _version, help, save, cancel });
        AcceptButton = save; CancelButton = cancel;
    }
}
