#nullable enable
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;
namespace Chummer;

// Owns presentation only. Collapsing never clears the controller's specialization.
internal sealed class SkillSpecializationDisclosure : IDisposable
{
    private readonly Control content;
    private readonly Func<bool> available;
    internal Button Toggle { get; } = new() { Content = "+", MinHeight = 32, Padding = new Thickness(8, 2) };
    internal bool Expanded { get; private set; }
    internal SkillSpecializationDisclosure(Control content, string label, Func<bool> available)
    {
        this.content = content; this.available = available;
        Avalonia.Automation.AutomationProperties.SetName(Toggle, label);
        Toggle.Click += Click;
        Refresh();
    }
    private void Click(object? sender, RoutedEventArgs args)
    {
        if (!Toggle.IsEffectivelyEnabled || !available()) return;
        Expanded = !Expanded; Refresh();
    }
    internal void Refresh()
    {
        content.IsVisible = Expanded;
        Toggle.Content = Expanded ? "−" : "+";
        Toggle.IsVisible = available();
    }
    public void Dispose() => Toggle.Click -= Click;
}
