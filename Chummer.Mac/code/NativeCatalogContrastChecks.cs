#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;
namespace Chummer;
internal static class NativeCatalogContrastChecks
{
    private static double Luminance(Color color)
    {
        static double Linear(byte channel) { double value=channel/255d; return value<=.04045 ? value/12.92 : Math.Pow((value+.055)/1.055,2.4); }
        return .2126*Linear(color.R)+.7152*Linear(color.G)+.0722*Linear(color.B);
    }
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool,string> check)
    {
        var character=new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Options.Books.Clear(); character.Options.Books.Add("SR4");
        var catalog=new XmlDocument(); catalog.LoadXml("<chummer><categories><category>Test</category></categories><armors><armor><name>Synthetic invalid definition with a long catalog name that needs several lines at narrow widths</name><category>Test</category><source>SR4</source></armor></armors></chummer>");
        var model=new ArmorPickerModel(character,false,catalog); var window=new ArmorPickerWindow(model,character);
        var nativeOwner=Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var previous=nativeOwner.RequestedThemeVariant;
        var pending=NativeDialogHost.ShowAsync<CatalogSelection>(window,owner);
        try
        {
            foreach(var theme in new[]{ThemeVariant.Dark,ThemeVariant.Light})
            {
                nativeOwner.RequestedThemeVariant=theme; await Task.Delay(100);
                var text=window.GetVisualDescendants().OfType<TextBlock>().Single(item=>item.Text==model.Error);
                var foreground=((ISolidColorBrush)text.Foreground!).Color;
                var background=((ISolidColorBrush)window.Background!).Color;
                double foregroundLuminance=Luminance(foreground),backgroundLuminance=Luminance(background);
                double contrast=(Math.Max(foregroundLuminance,backgroundLuminance)+.05)/(Math.Min(foregroundLuminance,backgroundLuminance)+.05);
                File.AppendAllText(Path.Combine(output,"native-catalog-error-contrast.txt"),$"{theme.Key}: {foreground}/{background} {contrast:F2}\n");
                using var image=new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width*2,(int)window.ClientSize.Height*2),new Vector(192,192));
                image.Render(window); image.Save(Path.Combine(output,"native-catalog-error-"+(theme==ThemeVariant.Dark?"dark":"light")+".png"),PngBitmapEncoderOptions.Default);
                check(contrast>=4.5,"Native catalog error text meets contrast in "+theme.Key);
                check(!window.AcceptButton.IsEnabled && model.HasError,"Native catalog error preserves rejected acceptance in "+theme.Key);
                var sourceWarning=window.GetVisualDescendants().OfType<TextBlock>().Single(item=>item.Name=="armorSourceWarning");
                check(window.TryFindResource("SystemControlErrorTextForegroundBrush",theme,out var semantic) && semantic is ISolidColorBrush brush && brush.Color==foreground &&
                    sourceWarning.Foreground is ISolidColorBrush warning && warning.Color==foreground,
                    "Native catalog and source errors use the Fluent semantic brush in "+theme.Key);
            }
            var root = (Grid)window.Content!;
            var body = root.Children.OfType<Grid>().Single(grid => grid.Name == "CatalogBody");
            var filters = root.Children.OfType<Grid>().Single(grid => grid.Name == "CatalogFilters");
            var footer = root.Children.OfType<Grid>().Single(grid => grid.Name == "CatalogFooter");
            var details = body.Children.OfType<ScrollViewer>().Single();
            var title = ((StackPanel)details.Content!).Children.OfType<TextBlock>().Single(block => block.Name == "CatalogSelectedName");
            double baseSize = title.FontSize;
            window.Width = 420;
            window.Height = 465;
            Avalonia.Controls.Documents.TextElement.SetFontSize(root, window.SearchBox.FontSize * 1.5);
            await Task.Delay(150);
            check(Grid.GetRow(details) == 1 && body.Children[0].Bounds.Bottom <= details.Bounds.Top &&
                filters.Bounds.Bottom <= body.Bounds.Top && body.Bounds.Bottom <= footer.Bounds.Top &&
                title.FontSize > baseSize && window.AcceptButton.IsEffectivelyVisible &&
                window.SearchBox.Bounds.Right <= filters.Bounds.Width + 1,
                "Narrow catalog stacks details below choices and keeps filters and confirmation clear at 150% text");
            var item = window.ItemList.GetVisualDescendants().OfType<ListBoxItem>().Single();
            var caption = item.GetVisualDescendants().OfType<TextBlock>()
                .Single(block => block.Text == model.SelectedEntry!.DisplayName);
            var itemOrigin = caption.TranslatePoint(default, window.ItemList);
            check(caption.FontSize == window.SearchBox.FontSize && caption.Bounds.Height > caption.FontSize * 2 &&
                itemOrigin.HasValue && itemOrigin.Value.X + caption.Bounds.Width <= window.ItemList.Bounds.Width + 1,
                "Long catalog item names wrap inside the list at 150% text");
            item.MinHeight = item.Bounds.Height + 16;
            await Task.Delay(100);
            var captionOrigin = caption.TranslatePoint(default, item);
            check(captionOrigin.HasValue &&
                Math.Abs(captionOrigin.Value.Y + caption.Bounds.Height / 2 - item.Bounds.Height / 2) <= 1,
                "The catalog caption remains vertically centered in the selection highlight");

        }
        finally { window.Close(); await pending; nativeOwner.RequestedThemeVariant=previous; }
    }
}
