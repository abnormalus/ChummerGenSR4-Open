#nullable enable
using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class SpiritControl
{
    private void AttachEntityActions(Forms.Form editor, bool sprite)
    {
        string prefix = sprite ? "NativeSprite" : "NativeSpirit";
        Controls.Remove(imgLink);
        Controls.Remove(imgNotes);
        var link = new Button { Name = prefix + "Link", Content = "Link" };
        var notes = new Button { Name = prefix + "Notes", Content = LanguageManager.Instance.GetString("Menu_Notes") };
        ContextMenu? linkMenu = null;
        link.Click += (_, _) =>
        {
            linkMenu?.Close();
            UpdateLinkCommands();
            linkMenu = Forms.AvaloniaContextCommands.Show(link, new Avalonia.Point(0, link.Bounds.Height),
                cmsSpirit, imgLink, () => !IsDisposed && link.IsEffectivelyVisible && link.IsEnabled,
                NativeControlPresentation.AddTheme);
        };
        notes.Click += (_, _) => imgNotes_Click(imgNotes, EventArgs.Empty);
        link.PointerEntered += (_, _) => ToolTip.SetTip(link, tipTooltip.GetToolTip(imgLink));
        notes.PointerEntered += (_, _) => ToolTip.SetTip(notes, tipTooltip.GetToolTip(imgNotes));
        var type = NativeListSelection.Attach(editor, cboSpiritName, sprite ? "Sprite type" : "Spirit type", fitPopupToItems: true,
            row: this, layoutManaged: true)!;
        Controls.Remove(cboSpiritName);
        Controls.Remove(txtCritterName);
        var name = new NativeInlineTextBinding(txtCritterName, () => _objSpirit != null);
        name.Input.Name = prefix + "Name";
        name.Input.ClearValue(TextBox.FontSizeProperty);
        AutomationProperties.SetName(name.Input, sprite ? "Sprite name" : "Spirit name");
        var actions = new WrapPanel
        {
            ItemSpacing = 8,
            LineSpacing = 4,
            Children =
            {
                type,
                name.Input,
                AddNumber(lblForce, nudForce),
                AddNumber(lblServices, nudServices),
                GearReadoutsLayout.BindOption(this, this, chkBound),
                link,
                notes,
                ArmorEquipmentLayout.BindCommand(this, this, cmdDelete)
            }
        };
        NativeControlPresentation.AddTheme(actions.Styles);
        // The compatibility host assigns its own height to the root control.
        // Keep that constraint off the wrapping panel so it can measure all lines.
        var surface = new Border { Child = actions };
        CreationRegionPresenter? presenter = null;
        Forms.Control? list = null;

        void Resize(object? sender, EventArgs args)
        {
            int width = Parent == null ? Width : Math.Max(0, Parent.ClientSize.Width - 18);
            Width = width;
            // Give text inputs room on wide sheets and a full line on compact ones.
            double baseWidth = Avalonia.Controls.Documents.TextElement.GetFontSize(actions) * 12;
            type.Width = Math.Min(width, width < 320 ? width : Math.Max(baseWidth, width * 0.20));
            name.Input.Width = Math.Min(width, width < 320 ? width : Math.Max(baseWidth, width * 0.22));
            foreach (var child in actions.Children)
                child.MaxWidth = width;
            actions.Measure(new Avalonia.Size(width, double.PositiveInfinity));
            Height = Math.Max(30, (int)Math.Ceiling(actions.DesiredSize.Height)) + 4;
            presenter?.Refresh();
        }

        void Mount(object? sender, EventArgs args)
        {
            if (list != null)
                list.SizeChanged -= Resize;
            linkMenu?.Close();
            presenter?.Dispose();
            presenter = null;
            list = Parent;
            if (list == null || FindForm() is not { } form)
                return;

            list.SizeChanged += Resize;
            presenter = new CreationRegionPresenter(form, this, surface, prefix + "Actions",
                () => new System.Drawing.Rectangle(0, 0, Width, Height), clipToSource: true);
            Resize(null, EventArgs.Empty);
        }

        actions.PropertyChanged += (_, change) =>
        {
            if (change.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty)
                Resize(null, EventArgs.Empty);
        };
        ParentChanged += Mount;
        Disposed += (_, _) =>
        {
            ParentChanged -= Mount;
            if (list != null)
                list.SizeChanged -= Resize;
            linkMenu?.Close();
            presenter?.Dispose();
            name.Dispose();
            cboSpiritName.Dispose();
            txtCritterName.Dispose();
            imgLink.Dispose();
            imgNotes.Dispose();
        };
        Mount(null, EventArgs.Empty);

        StackPanel AddNumber(Forms.Label label, Forms.NumericUpDown source)
        {
            Controls.Remove(label);
            Controls.Remove(source);
            var binding = new NativeInlineNumericBinding(source, tipTooltip, wholeNumbers: true);
            binding.Input.Width = 96;
            binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
            var caption = new TextBlock { Text = label.Text, TextWrapping = TextWrapping.Wrap };
            AutomationProperties.SetLabeledBy(binding.Input, caption);
            Disposed += (_, _) =>
            {
                binding.Dispose();
                source.Dispose();
                label.Dispose();
            };
            return new StackPanel { Spacing = 2, Children = { caption, binding.Input } };
        }
    }
}
