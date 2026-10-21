#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
namespace Chummer;

// Shared single-line editor alignment for every direct native themed surface.
// Keep local height/padding rules (including compact attributes) authoritative.
internal static class NativeControlPresentation
{
    internal static void AddTheme(Styles styles)
    {
        styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        styles.Add(new Style(s => s.OfType<TextBox>().PropertyEquals(TextBox.AcceptsReturnProperty, false))
        { Setters = { new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        styles.Add(new Style(s => s.OfType<ComboBox>())
        { Setters = { new Setter(ComboBox.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        styles.Add(new Style(s => s.OfType<NumericUpDown>())
        { Setters = { new Setter(NumericUpDown.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        styles.Add(new Style(s => s.OfType<NumericUpDown>().Template().OfType<TextBox>())
        { Setters = { new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        styles.Add(new Style(s => s.OfType<AutoCompleteBox>().Template().OfType<TextBox>())
        { Setters = { new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
    }
}
