#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class EditorSummaryPane
{
    internal static Button CreateToggle(Forms.Form editor, Forms.SplitContainer split)
    {
        const int narrowWidth = 760;
        const int preferredMainMinimum = 480;
        const int summaryMinimum = 150;
        var language = LanguageManager.Instance;
        var button = new Button
        {
            Name = "NativeSummaryToggle",
            Margin = new Thickness(4, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Top,
            MaxWidth = 152
        };
        var caption = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        button.Content = caption;
        bool? userExpanded = null;
        bool? wasNarrow = null;
        void Arrange()
        {
            int width = split.ClientSize.Width;
            bool narrow = width < narrowWidth;
            if (wasNarrow != narrow)
            {
                wasNarrow = narrow;
                userExpanded = null;
            }
            bool expanded = userExpanded ?? !narrow;
            split.Panel1MinSize = System.Math.Clamp(width - summaryMinimum - split.SplitterWidth, 0, preferredMainMinimum);
            split.Panel2MinSize = summaryMinimum;
            split.Panel2Collapsed = !expanded;
            caption.Text = language.GetString(expanded ? "Button_HideCharacterSummary" : "Button_ShowCharacterSummary");
        }
        button.Click += (_, _) =>
        {
            userExpanded = split.Panel2Collapsed;
            Arrange();
        };
        void Resize(object? sender, System.EventArgs args) => Arrange();
        split.SizeChanged += Resize;
        editor.Disposed += (_, _) => split.SizeChanged -= Resize;
        Arrange();
        return button;
    }
}
