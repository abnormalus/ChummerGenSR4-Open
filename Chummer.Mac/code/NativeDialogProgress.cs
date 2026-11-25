#nullable enable
using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeDialogProgress
{
    internal static ProgressBar Bind(Forms.Form owner, Forms.ProgressBar source, string label)
    {
        var value = new ProgressBar { Name = "Native" + source.Name, MinHeight = 8 };
        AutomationProperties.SetName(value, label);
        NativeControlPresentation.AddTheme(value.Styles);
        source.Parent?.Controls.Remove(source);
        source.CreateControl();

        void Refresh()
        {
            value.Maximum = Math.Max(value.Maximum, source.Maximum);
            value.Minimum = source.Minimum;
            value.Maximum = source.Maximum;
            value.Value = source.Value;
            value.IsIndeterminate = source.Style == Forms.ProgressBarStyle.Marquee && source.MarqueeAnimationSpeed > 0;
        }

        var observation = new NativeControlObservation(source, Refresh);
        owner.Disposed += (_, _) =>
        {
            observation.Dispose();
            source.Dispose();
        };
        Refresh();
        return value;
    }
}
