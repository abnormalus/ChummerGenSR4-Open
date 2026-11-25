using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class MartialArtsDetailsLayout
{
    internal static ScrollViewer Create(Forms.Form owner, Forms.TabPage page, Control rating,
        Forms.Label caption, Forms.Label source, Forms.ToolTip tips, Action openSource)
    {
        var reference = GearReadoutsLayout.BindSource(owner, page, "NativeMartialArtSource",
            caption, source, tips, openSource);
        return new ScrollViewer
        {
            Name = "NativeMartialArtsDetails",
            Content = new StackPanel { Spacing = 7, Children = { rating, reference } },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };
    }
}
