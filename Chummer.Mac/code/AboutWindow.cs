#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal sealed class AboutWindow : Window
{
    private static readonly Assembly Assembly = typeof(Program).Assembly;
    internal static string LogoPath => Path.Combine(AppContext.BaseDirectory, "branding", "chummer.png");

    internal static string BuildDetails =>
        $"Version: {BuildIdentity.Version}\nChannel: {BuildIdentity.Channel}\n" +
        $"Source commit: {BuildIdentity.SourceCommit ?? "unavailable (development build)"}\n" +
        $"Build: {typeof(Program).Module.ModuleVersionId}";

    private const string Acknowledgements =
        "Big thanks to everyone in the Dumpshock community for supporting this project with all of their valuable feedback, great ideas, bug reports, and pointing out of my silly mistakes. Also a big thanks to everyone who has volunteered their time to translate Chummer in other languages!";
    private const string Disclaimer =
        "Chummer is completely unofficial and is in no way endorsed by The Topps Company, Inc. or Catalyst Game Labs. The Topps Company, Inc. has sole ownership of the names, logo, artwork, marks, photographs, sounds, audio, video and/or any proprietary material used in connection with the game Shadowrun.\n\nUnless agreed to in writing, the developer provides the Work (and each Contributor provides its Contributions) on an \"AS IS\" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either expressed or implied, including, without limitation, any warranties or conditions of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A PARTICULAR PURPOSE.\n\nBy using Chummer You agree that You legally own a copy of the Shadowrun rulebook and any sourcebook whose information you select to use. You are solely responsible for determining the appropriateness of using or redistributing the content You create and assume any risks associated with Your exercise of permissions under this License.\n\nChummer uses icons from the Silk icon set made by Mark James which is available at www.famfamfam.com.";

    internal static async Task ShowAsync(Forms.Form owner)
    {
        using var logo = new Bitmap(LogoPath);
        await NativeDialogHost.ShowAsync<object>(new AboutWindow(logo), owner);
    }

    internal AboutWindow(Bitmap logo)
    {
        var title = Assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        Title = "About " + (string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(Assembly.Location) : title);
        Width = 660;
        Height = 590;
        MinWidth = 280;
        MinHeight = 230;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        NativeControlPresentation.AddTheme(Styles);

        var details = new StackPanel { Spacing = 12 };
        details.Children.Add(new Image { Source = logo, MaxWidth = 100, MaxHeight = 100, HorizontalAlignment = HorizontalAlignment.Left, Stretch = Stretch.Uniform });
        var product = new TextBlock
        {
            Text = Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        details.Children.Add(product);
        details.Children.Add(Paragraph($"Version {BuildIdentity.ReleaseVersion}"));
        details.Children.Add(Paragraph(Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright));
        details.Children.Add(Paragraph(Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company));
        details.Children.Add(Paragraph(BuildDetails));
        details.Children.Add(Paragraph(Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description));
        details.Children.Add(Paragraph(Acknowledgements));
        details.Children.Add(Paragraph(Disclaimer));

        var scroll = new ScrollViewer
        {
            Content = details,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            AllowAutoHide = false
        };
        var close = new Button { Name = "AboutClose", Content = "_OK", IsDefault = true, IsCancel = true };
        close.Click += (_, _) => Close();
        var donate = new Button { Content = "Donate" };
        donate.Click += (_, _) =>
        {
            Close();
            Process.Start(new ProcessStartInfo("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=LG855DVUT8FDU")
            {
                UseShellExecute = true
            });
        };
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        actions.Children.Add(donate);
        actions.Children.Add(close);
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 10 };
        layout.Children.Add(scroll);
        Grid.SetRow(actions, 1);
        layout.Children.Add(actions);
        void SizeTitle() => product.FontSize = Avalonia.Controls.Documents.TextElement.GetFontSize(layout) * 1.6;
        layout.PropertyChanged += (_, args) =>
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty) SizeTitle();
        };
        SizeTitle();
        var captionTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((caption, _) =>
            new AccessText { Text = caption, TextWrapping = TextWrapping.Wrap });
        donate.ContentTemplate = close.ContentTemplate = captionTemplate;
        layout.SizeChanged += (_, args) =>
        {
            if (args.NewSize.Width > 0)
                donate.MaxWidth = close.MaxWidth = args.NewSize.Width;
        };
        Content = layout;
    }

    private static SelectableTextBlock Paragraph(string? value) =>
        new() { Text = value, TextWrapping = TextWrapping.Wrap, IsVisible = !string.IsNullOrEmpty(value) };
}
