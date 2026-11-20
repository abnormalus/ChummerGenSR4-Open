#nullable enable
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Chummer reorders or reparents rows inside one tree. A captured pointer keeps
// those gestures within the native tree and leaves the existing move rules in the editor.
internal sealed class NativeTreeDrag
{
    private readonly Forms.TreeView source;
    private readonly Func<Forms.MouseButtons, bool> prepare;
    private readonly Action<Forms.TreeNode?, Forms.DragEventArgs> preview;
    private readonly Action<Forms.TreeNode?, Forms.DragEventArgs> commit;
    private readonly Action cleanUp;
    private Forms.TreeNode? origin;
    private TreeViewItem? highlighted;

    internal NativeTreeDrag(Forms.TreeView source, Func<Forms.MouseButtons, bool> prepare,
        Action<Forms.TreeNode?, Forms.DragEventArgs> preview, Action<Forms.TreeNode?, Forms.DragEventArgs> commit,
        Action cleanUp)
    {
        this.source = source;
        this.prepare = prepare;
        this.preview = preview;
        this.commit = commit;
        this.cleanUp = cleanUp;
    }

    internal bool SuppressContextMenu { get; private set; }

    internal bool Begin(Forms.TreeNode node, Forms.MouseButtons button)
    {
        if (origin != null || node.TreeView != source)
            return false;
        source.SelectedNode = node;
        if (!prepare(button))
        {
            cleanUp();
            return false;
        }
        origin = node;
        return true;
    }

    internal void Complete(Forms.TreeNode? target, bool inside)
    {
        try
        {
            if (!inside || origin == null || ReferenceEquals(origin, target) || origin.TreeView != source ||
                !ReferenceEquals(source.SelectedNode, origin) || (target != null && target.TreeView != source))
                return;
            var args = MoveArgs();
            preview(target, args);
            if (args.Effect == Forms.DragDropEffects.Move)
                commit(target, args);
        }
        finally { Cancel(); }
    }

    internal void Cancel()
    {
        if (origin == null)
            return;
        origin = null;
        ClearHighlight();
        cleanUp();
    }

    internal void Wire(TreeView tree)
    {
        IPointer? pointer = null;
        TreeViewItem? pressed = null;
        Point pressPoint = default;
        Forms.MouseButtons button = Forms.MouseButtons.None;

        tree.AddHandler(InputElement.PointerPressedEvent, (_, args) =>
        {
            if (origin != null)
                return;
            SuppressContextMenu = false;
            pressPoint = args.GetPosition(tree);
            pressed = NativeEditorTree.FindRow(args.Source) ?? NativeEditorTree.RowAt(tree, pressPoint);
            pointer = args.Pointer;
            var properties = args.GetCurrentPoint(tree).Properties;
            button = properties.IsRightButtonPressed ? Forms.MouseButtons.Right :
                properties.IsLeftButtonPressed ? Forms.MouseButtons.Left : Forms.MouseButtons.None;
        }, RoutingStrategies.Tunnel);

        tree.AddHandler(InputElement.PointerMovedEvent, (_, args) =>
        {
            if (args.Pointer != pointer || pressed?.Tag is not Forms.TreeNode node || button == Forms.MouseButtons.None)
                return;
            Point position = args.GetPosition(tree);
            if (origin == null)
            {
                if (Math.Abs(position.X - pressPoint.X) < 5 && Math.Abs(position.Y - pressPoint.Y) < 5)
                    return;
                var properties = args.GetCurrentPoint(tree).Properties;
                bool isPressed = button == Forms.MouseButtons.Right ? properties.IsRightButtonPressed : properties.IsLeftButtonPressed;
                if (!isPressed || !Begin(node, button))
                {
                    pressed = null;
                    return;
                }
                tree.SelectedItem = pressed;
                tree.Focus();
                args.Pointer.Capture(tree);
                SuppressContextMenu = true;
            }
            var row = NativeEditorTree.RowAt(tree, position);
            var target = row?.Tag as Forms.TreeNode;
            ClearHighlight();
            var effect = MoveArgs();
            preview(target, effect);
            if (row != null && target != null && effect.Effect == Forms.DragDropEffects.Move &&
                target.BackColor.ToArgb() == Forms.SystemColors.ControlDark.ToArgb())
            {
                highlighted = row;
                row.Background = new SolidColorBrush(Color.FromArgb(80, 0, 120, 215));
            }
            args.Handled = true;
        }, RoutingStrategies.Tunnel);

        tree.AddHandler(InputElement.PointerReleasedEvent, (_, args) =>
        {
            if (args.Pointer != pointer)
                return;
            pressed = null;
            pointer = null;
            if (origin == null)
                return;
            Point position = args.GetPosition(tree);
            args.Handled = true;
            try
            {
                Complete(NativeEditorTree.RowAt(tree, position)?.Tag as Forms.TreeNode,
                    new Rect(tree.Bounds.Size).Contains(position));
            }
            finally { args.Pointer.Capture(null); }
        }, RoutingStrategies.Tunnel);

        tree.PointerCaptureLost += (_, args) =>
        {
            if (args.Pointer != pointer || ReferenceEquals(args.Pointer.Captured, tree))
                return;
            pressed = null;
            pointer = null;
            Cancel();
        };

        tree.KeyDown += (_, args) =>
        {
            if (args.Key != Key.Escape || origin == null)
                return;
            args.Handled = true;
            pressed = null;
            var captured = pointer;
            pointer = null;
            Cancel();
            captured?.Capture(null);
        };

    }

    private void ClearHighlight()
    {
        highlighted?.ClearValue(TreeViewItem.BackgroundProperty);
        highlighted = null;
    }

    private static Forms.DragEventArgs MoveArgs() =>
        new(null, 0, 0, 0, Forms.DragDropEffects.Move, Forms.DragDropEffects.Move);
}
