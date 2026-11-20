#nullable enable
using System;
using System.ComponentModel;
using System.IO;
using Avalonia.Media.Imaging;

namespace Chummer;

internal sealed class RepositoryCharacterRow(RepositoryFile file) : INotifyPropertyChanged, IDisposable
{
    internal RepositoryFile File { get; } = file;
    public Bitmap? Portrait { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetPortrait(byte[] png)
    {
        using var stream = new MemoryStream(png, writable: false);
        Portrait = new Bitmap(stream);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Portrait)));
    }

    public void Dispose() => Portrait?.Dispose();
}
