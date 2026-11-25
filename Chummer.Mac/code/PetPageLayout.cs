#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class PetPageLayout
{
    internal static void Attach(Forms.Form owner, Forms.TabPage page, Forms.Panel controllers, Forms.Button add)
    {
        if (Application.Current == null)
            return;

        var workspace = owner.MdiParent ?? owner;
        var rows = new StackPanel { Spacing = 4 };
        var scroll = new ScrollViewer { Content = rows, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        NativeControlPresentation.AddTheme(scroll.Styles);
        var command = ArmorEquipmentLayout.BindCommand(owner, page, add);
        command.HorizontalAlignment = HorizontalAlignment.Left;
        var layout = new DockPanel { LastChildFill = true };
        command.Margin = new Thickness(0, 0, 0, 4);
        DockPanel.SetDock(command, Dock.Top);
        layout.Children.Add(command);
        layout.Children.Add(scroll);
        var surface = new Border { Padding = new Thickness(6), Child = layout };
        NativeControlPresentation.AddTheme(surface.Styles);
        var presenter = new CreationRegionPresenter(owner, page, surface, "NativePets",
            () => new System.Drawing.Rectangle(System.Drawing.Point.Empty, InventoryTreeLayout.VisiblePageSize(workspace, page)),
            clipToSource: true);
        var bindings = new Dictionary<PetControl, (Control View, Action Dispose)>();
        // The original row stays parented for modal ownership and editor rules.
        controllers.Visible = false;
        foreach (var pet in controllers.Controls.OfType<PetControl>())
            Add(pet);
        void Added(object? sender, Forms.ControlEventArgs args)
        {
            if (args.Control is PetControl pet)
                Add(pet);
        }
        void Removed(object? sender, Forms.ControlEventArgs args)
        {
            if (args.Control is PetControl pet && bindings.Remove(pet, out var binding))
            {
                rows.Children.Remove(binding.View);
                binding.Dispose();
            }
        }
        void Add(PetControl pet)
        {
            if (page.Visible)
                pet.InitializePetMetatype();
            var binding = pet.CreateNativeRow();
            bindings.Add(pet, binding);
            rows.Children.Add(binding.View);
        }
        void ShowPets(object? sender, EventArgs args)
        {
            if (page.Visible)
                foreach (var pet in bindings.Keys)
                    pet.InitializePetMetatype();
        }
        page.VisibleChanged += ShowPets;
        controllers.ControlAdded += Added;
        controllers.ControlRemoved += Removed;
        owner.Disposed += (_, _) =>
        {
            page.VisibleChanged -= ShowPets;
            controllers.ControlAdded -= Added;
            controllers.ControlRemoved -= Removed;
            foreach (var binding in bindings.Values)
                binding.Dispose();
            presenter.Dispose();
        };
    }
}

public partial class PetControl
{
    internal (Control View, Action Dispose) CreateNativeRow()
    {
        Controls.Remove(txtContactName);
        var binding = new NativeInlineTextBinding(txtContactName, () => !IsDisposed && Enabled);
        binding.Input.Name = "NativePetName";
        binding.Input.ClearValue(TextBox.FontSizeProperty);
        binding.Input.ClearValue(TextBox.MinHeightProperty);
        var name = new TextBlock { Text = lblName.Text, TextWrapping = TextWrapping.Wrap };
        Avalonia.Automation.AutomationProperties.SetLabeledBy(binding.Input, name);
        var metatype = new TextBlock { TextWrapping = TextWrapping.Wrap };
        void RefreshMetatype(object? sender, EventArgs args)
        {
            metatype.Text = lblMetatypeLabel.Text + " " + lblMetatype.Text;
            metatype.IsVisible = !string.IsNullOrWhiteSpace(lblMetatype.Text);
        }
        lblMetatype.TextChanged += RefreshMetatype;
        RefreshMetatype(null, EventArgs.Empty);

        var actions = new WrapPanel { ItemSpacing = 6, LineSpacing = 4 };
        var link = new Button
        {
            Name = "NativePetLink",
            Content = new TextBlock
            {
                Text = LanguageManager.Instance.GetString("Tip_Contact_LinkFile"),
                TextWrapping = TextWrapping.Wrap
            }
        };
        var attach = new MenuItem { Header = LanguageManager.Instance.GetString(tsAttachCharacter.Tag!.ToString()!) };
        var open = new MenuItem { Header = LanguageManager.Instance.GetString(tsContactOpen.Tag!.ToString()!) };
        var remove = new MenuItem { Header = LanguageManager.Instance.GetString(tsRemoveCharacter.Tag!.ToString()!) };
        attach.Click += (_, _) => tsAttachCharacter_Click(this, EventArgs.Empty);
        open.Click += (_, _) => tsContactOpen_Click(this, EventArgs.Empty);
        remove.Click += (_, _) => tsRemoveCharacter_Click(this, EventArgs.Empty);
        var flyout = new MenuFlyout();
        flyout.Items.Add(attach);
        flyout.Items.Add(open);
        flyout.Items.Add(remove);
        flyout.Opening += (_, _) =>
        {
            bool linked = !string.IsNullOrEmpty(ContactObject.FileName);
            attach.IsVisible = !linked;
            open.IsVisible = remove.IsVisible = linked;
        };
        link.Flyout = flyout;
        actions.Children.Add(link);
        var notes = Command(LanguageManager.Instance.GetString("Tip_Contact_EditNotes"), () => imgNotes_Click(this, EventArgs.Empty));
        notes.Name = "NativePetNotes";
        actions.Children.Add(notes);
        var delete = Command(cmdDelete.Text, () => cmdDelete_Click(this, EventArgs.Empty));
        delete.Name = "NativePetDelete";
        actions.Children.Add(delete);
        NativeControlPresentation.ConstrainWrapItems(actions);
        var fields = new StackPanel { Spacing = 3, Children = { name, binding.Input, metatype, actions } };
        var row = new Border { Child = fields, Padding = new Thickness(4) };
        return (row, () =>
        {
            flyout.Hide();
            lblMetatype.TextChanged -= RefreshMetatype;
            binding.Dispose();
            txtContactName.Dispose();
        });

        static Button Command(string caption, Action action)
        {
            var button = new Button { Content = new TextBlock { Text = caption, TextWrapping = TextWrapping.Wrap } };
            button.Click += (_, _) => action();
            return button;
        }
    }
}
