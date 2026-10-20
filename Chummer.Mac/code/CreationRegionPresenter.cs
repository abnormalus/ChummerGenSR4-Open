#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Creation-owned native viewports attach to the visible presenter. This is not a
// change to global NativeControlHost behavior or a per-spinner overlay.
internal sealed class CreationRegionPresenter : IDisposable
{
    private readonly Forms.Form editor, presenter;
    private readonly Forms.Control source, root;
    private readonly Forms.NativeControlHost host;
    private readonly Func<Rectangle> localBounds;
    private readonly List<Forms.Control> ancestors = new();
    private bool disposed;
    internal CreationRegionPresenter(Forms.Form editor, Forms.Control source, object native, string name,
        Func<Rectangle>? localBounds = null)
    {
        this.editor = editor; this.source = source;
        this.localBounds = localBounds ?? (() => new Rectangle(Point.Empty, source.Size));
        presenter = editor.MdiParent ?? editor;
        root = presenter.Controls[0].Parent!;
        host = new Forms.NativeControlHost { Name = name, NativeControl = native };
        presenter.Controls.Add(host);
        editor.Shown += Refresh; presenter.MdiChildActivate += Refresh;
        presenter.SizeChanged += Refresh; presenter.LocationChanged += Refresh;
        for (Forms.Control? control = source; control != null; control = control.Parent)
        {
            ancestors.Add(control);
            control.VisibleChanged += Refresh; control.SizeChanged += Refresh; control.LocationChanged += Refresh;
        }
        Sync();
    }
    private void Refresh(object? sender, EventArgs args) => Sync();
    private void Sync()
    {
        if (disposed) return;
        var bounds = localBounds();
        var point = root.PointToClient(source.PointToScreen(bounds.Location));
        host.Bounds = new Rectangle(point, bounds.Size);
        host.Visible = editor.Visible && (editor.MdiParent == null || editor.MdiParent.ActiveMdiChild == editor) && source.Visible;
        host.SyncNativeControl();
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        editor.Shown -= Refresh; presenter.MdiChildActivate -= Refresh;
        presenter.SizeChanged -= Refresh; presenter.LocationChanged -= Refresh;
        foreach (var control in ancestors)
        { control.VisibleChanged -= Refresh; control.SizeChanged -= Refresh; control.LocationChanged -= Refresh; }
        host.NativeControl = null; presenter.Controls.Remove(host); host.Dispose(); ancestors.Clear();
    }
}
