#nullable enable
using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CharacterPortraitLayout
{
    internal static StackPanel Build(Forms.Form editor, Forms.TabPage page, Character character, Control reputation,
        Forms.PictureBox source, Forms.Label caption, Forms.Button add, Forms.Button remove)
    {
        var edit = (Forms.Button)page.Controls.Find("editPortrait", false).Single();
        var commands = new WrapPanel();
        foreach (var controller in new[] { add, edit, remove })
        {
            page.Controls.Remove(controller);
            var button = new Button
            {
                Name = "Native" + controller.Name,
                Content = Label(controller.Text),
                Margin = new Thickness(0, 0, 4, 4)
            };
            button.Bind(Button.BorderBrushProperty, new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("ControlStrokeColorDefaultBrush"));
            button.BorderThickness = new Thickness(1);
            button.Click += (_, _) => controller.PerformClick();
            void Refresh() { button.IsEnabled = controller.Enabled; button.IsVisible = controller.Visible; }
            var observation = new NativeControlObservation(controller, Refresh);
            Refresh();
            commands.Children.Add(button);
            editor.Disposed += (_, _) => { observation.Dispose(); controller.Dispose(); };
        }

        var image = new Image
        {
            Name = "NativeCharacterPortrait", Stretch = Stretch.Uniform,
            MaxHeight = 300, HorizontalAlignment = HorizontalAlignment.Center
        };
        Avalonia.Automation.AutomationProperties.SetName(image, caption.Text);
        image.DoubleTapped += (_, _) =>
        {
            if (!string.IsNullOrEmpty(character.Mugshot))
                edit.PerformClick();
        };
        string? displayed = null;
        void RefreshPortrait()
        {
            string encoded = character.Mugshot;
            if (displayed == encoded) return;
            displayed = encoded;
            (image.Source as Bitmap)?.Dispose();
            image.Source = null;
            if (string.IsNullOrEmpty(encoded)) return;

            try
            {
                using var stream = new MemoryStream(Convert.FromBase64String(encoded));
                image.Source = new Bitmap(stream);
            }
            catch (Exception error)
            {
                AppDiagnostics.Record("portrait.preview-failed", error);
            }
        }
        Forms.InvalidateEventHandler invalidated = (_, _) => RefreshPortrait();
        source.Invalidated += invalidated;
        page.Controls.Remove(source);
        source.CreateControl();
        RefreshPortrait();
        var heading = MoveLabel(editor, page, caption);
        editor.Disposed += (_, _) =>
        {
            source.Invalidated -= invalidated;
            (image.Source as Bitmap)?.Dispose();
            image.Source = null;
            source.Image?.Dispose();
            source.Dispose();
        };
        return new StackPanel
        {
            Name = "NativeCharacterInfoSidebar", Spacing = 6,
            Children = { reputation, heading, commands, image }
        };
    }

    internal static StackPanel ReadOnlyReputation(Forms.Form editor, Forms.TabPage page,
        params (Forms.Label Caption, Forms.Label Total)[] fields)
    {
        var rows = new StackPanel { Spacing = 5 };
        foreach (var (caption, total) in fields)
        {
            var row = new Grid { HorizontalAlignment = HorizontalAlignment.Left, ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
            var value = MoveLabel(editor, page, total);
            value.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(value, 1);
            var label = MoveLabel(editor, page, caption);
            label.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(label);
            row.Children.Add(value);
            rows.Children.Add(row);
        }
        return rows;
    }

    private static TextBlock MoveLabel(Forms.Form editor, Forms.TabPage page, Forms.Label source)
    {
        page.Controls.Remove(source);
        var label = Label(source.Text);
        EventHandler changed = (_, _) => label.Text = source.Text;
        source.TextChanged += changed;
        editor.Disposed += (_, _) =>
        {
            source.TextChanged -= changed;
            source.Dispose();
        };
        return label;
    }

    private static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
}
