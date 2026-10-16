#nullable enable
using System.Globalization;
using Avalonia.Controls;

namespace Chummer;

// Shared standard Avalonia integer editor. Layout uses logical units; the framework
// owns buttons, typing, keyboard navigation, clipping and disabled state.
internal static class NativeIntegerInput
{
    internal static NumericUpDown Create(string name, int minimum, int maximum) => new()
    {
        Name = name,
        Minimum = minimum,
        Maximum = maximum,
        Increment = 1,
        FormatString = "0",
        ParsingNumberStyle = NumberStyles.Integer,
        ClipValueToMinMax = true,
        Width = 120,
        MinHeight = 34
    };
}
internal interface IRatedCatalogPicker
{
    int Rating { get; set; }
    int MinimumRating { get; }
    int MaximumRating { get; }
    bool IsRatingEnabled { get; }
}
