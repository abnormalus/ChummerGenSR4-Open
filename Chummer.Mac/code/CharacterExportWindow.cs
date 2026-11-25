#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal sealed class CharacterExportWindow : Window
{
    private static string TemplateDirectory => Path.Combine(AppContext.BaseDirectory, "export");

    internal ComboBox FormatInput { get; }
    internal Button ExportButton { get; }
    internal Button CancelButton { get; }

    internal static async Task ShowAsync(Forms.Form owner, XmlDocument character,
        Func<CharacterExportWindow, string, Task<string?>>? selectDestination = null)
    {
        var window = new CharacterExportWindow(character, selectDestination ?? PickDestinationAsync);
        await NativeDialogHost.ShowAsync<object>(window, owner);
    }

    private CharacterExportWindow(XmlDocument character, Func<CharacterExportWindow, string, Task<string?>> selectDestination)
    {
        var language = LanguageManager.Instance;
        Title = "Export Character";
        Width = 420;
        MinWidth = 260;
        MinHeight = 160;
        MaxHeight = 500;
        SizeToContent = SizeToContent.Height;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);

        var templates = Directory.GetFiles(TemplateDirectory, "*.xsl")
            .Where(path => Path.GetExtension(path) == ".xsl")
            .ToArray();
        FormatInput = new ComboBox
        {
            ItemsSource = templates.Select(Path.GetFileNameWithoutExtension).ToArray(),
            SelectedIndex = templates.Length == 0 ? -1 : 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        string label = language.GetString("Label_ExportTo");
        Avalonia.Automation.AutomationProperties.SetName(FormatInput, label);

        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, IsVisible = false };
        ExportButton = new Button { Content = language.GetString("String_OK"), IsDefault = true, IsEnabled = templates.Length > 0 };
        CancelButton = new Button { Content = language.GetString("String_Cancel"), IsCancel = true };
        CancelButton.Click += (_, _) => Close();
        bool busy = false, closed = false;
        Closed += (_, _) => closed = true;
        ExportButton.Click += async (_, _) =>
        {
            if (busy || closed || FormatInput.SelectedIndex < 0)
                return;

            string template = templates[FormatInput.SelectedIndex];
            busy = true;
            ExportButton.IsEnabled = FormatInput.IsEnabled = false;
            status.IsVisible = false;
            try
            {
                string extension = File.ReadLines(template)
                    .LastOrDefault(line => line.StartsWith("<!-- ext:", StringComparison.Ordinal))?
                    .Replace("<!-- ext:", "").Replace("-->", "").Trim() ?? "xml";
                string? destination = await selectDestination(this, extension);
                if (closed || string.IsNullOrEmpty(destination))
                    return;

                await File.WriteAllTextAsync(destination, Render(character, template));
                if (!closed)
                    Close();
            }
            catch (Exception error)
            {
                AppDiagnostics.Record("character-export-failed", error);
                if (!closed)
                {
                    status.Text = language.GetString("Message_CommandFailed");
                    status.IsVisible = true;
                }
            }
            finally
            {
                busy = false;
                if (!closed)
                    ExportButton.IsEnabled = FormatInput.IsEnabled = true;
            }
        };

        var formatCaption = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap };
        Avalonia.Automation.AutomationProperties.SetLabeledBy(FormatInput, formatCaption);
        var fields = new StackPanel { Spacing = 10, Children = { formatCaption, FormatInput, status } };
        Content = NativeControlPresentation.CreatePromptLayout(fields, ExportButton, CancelButton);
    }

    private static async Task<string?> PickDestinationAsync(CharacterExportWindow window, string extension)
    {
        var selected = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = LanguageManager.Instance.GetString("Button_Viewer_SaveAsHtml"),
            DefaultExtension = extension,
            FileTypeChoices = [new FilePickerFileType(extension.ToUpperInvariant()) { Patterns = ["*." + extension] }]
        });
        return selected?.TryGetLocalPath();
    }

    private static string Render(XmlDocument character, string template)
    {
        var transform = new XslCompiledTransform();
        transform.Load(template, XsltSettings.Default, new BundledXmlResolver(TemplateDirectory));
        var settings = transform.OutputSettings!.Clone();
        settings.CheckCharacters = false;
        settings.ConformanceLevel = ConformanceLevel.Fragment;
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
            transform.Transform(character, null, writer);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
