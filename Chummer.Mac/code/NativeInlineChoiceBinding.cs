#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Editable suggestions retain source choice objects and custom literal text.
// Existing controller TextChanged handlers remain the model/policy boundary.
internal sealed class NativeInlineChoiceBinding : IDisposable
{
    private readonly Forms.ComboBox source;
    private readonly NativeControlObservation observation;
    private object[] choices = Array.Empty<object>();
    private bool syncing, disposed;
    internal AutoCompleteBox Input { get; }
    internal NativeInlineChoiceBinding(Forms.ComboBox source)
    {
        this.source = source;
        Input = new AutoCompleteBox { MinHeight = 32, MinimumPrefixLength = 0,
            IsTextCompletionEnabled = false, FilterMode = AutoCompleteFilterMode.Contains };
        observation = new NativeControlObservation(source, Refresh);
        source.TextChanged += SourceChanged;
        Input.PropertyChanged += NativeChanged;
        Refresh();
    }
    private void SourceChanged(object? sender, EventArgs args) => Refresh();
    internal void Refresh()
    {
        if (disposed || syncing) return;
        syncing = true;
        try
        {
            var current = source.Items.Cast<object>().ToArray();
            if (!choices.SequenceEqual(current)) { choices = current; Input.ItemsSource = choices; }
            // The legacy editable combo deliberately retains SelectedItem when
            // custom text no longer matches it. AutoCompleteBox treats selection
            // as the displayed choice, so do not reassert that stale suggestion.
            Input.SelectedItem = source.SelectedItem is object selected && source.Text == source.GetItemText(selected)
                ? selected : null;
            Input.Text = source.Text;
            Input.IsEnabled = source.Enabled; Input.IsVisible = source.Visible;
        }
        finally { syncing = false; }
    }
    private void NativeChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (disposed || syncing || (args.Property != AutoCompleteBox.TextProperty && args.Property != AutoCompleteBox.SelectedItemProperty)) return;
        if (!source.Enabled || !source.Visible) { Refresh(); return; }
        // Clearing a suggestion is part of typing/filtering, not a request to
        // restore the former selection before the corresponding text event.
        if (args.Property == AutoCompleteBox.SelectedItemProperty && Input.SelectedItem == null) return;
        syncing = true;
        try
        {
            if (args.Property == AutoCompleteBox.SelectedItemProperty && Input.SelectedItem != null && source.Items.Contains(Input.SelectedItem))
                source.SelectedItem = Input.SelectedItem;
            else if (args.Property == AutoCompleteBox.TextProperty)
                source.Text = Input.Text ?? "";
        }
        finally { syncing = false; Refresh(); }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Input.PropertyChanged -= NativeChanged;
        source.TextChanged -= SourceChanged;
        observation.Dispose();
    }
}
