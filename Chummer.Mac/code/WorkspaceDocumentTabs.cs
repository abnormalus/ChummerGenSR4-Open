#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal sealed class WorkspaceDocumentTabs : IDisposable
{
    private sealed class Document(Forms.Form editor, Character? character, TabItem? header)
    {
        internal Forms.Form Editor { get; } = editor;
        internal Character? Character { get; } = character;
        internal TabItem? Header { get; } = header;
    }

    private readonly frmMain owner;
    private readonly Forms.NativeControlHost host;
    private readonly TabControl? tabs;
    private static readonly FuncDataTemplate<string> HeaderTemplate = new((title, _) => new TextBlock
    {
        Text = title,
        MaxWidth = 220,
        TextTrimming = TextTrimming.CharacterEllipsis
    });

    private readonly List<Document> documents = new();
    private bool syncing;
    private bool disposed;

    internal WorkspaceDocumentTabs(frmMain owner, Forms.NativeControlHost host)
    {
        this.owner = owner;
        this.host = host;
        if (Avalonia.Application.Current == null)
            return;

        tabs = new TabControl { Name = "WorkspaceDocumentTabs", Padding = new Avalonia.Thickness(0) };
        AutomationProperties.SetName(tabs, "Open documents");
        var viewport = NativeEditorTabStrip.CreateViewport(tabs);
        host.Height = NativeEditorTabStrip.HeaderHeight;
        tabs.SelectionChanged += (_, _) =>
        {
            if (!syncing && tabs.SelectedItem is TabItem { Tag: Forms.Form editor } && !editor.IsDisposed)
                editor.Select();
        };
        host.NativeControl = viewport;
        NativeEditorTabStrip.TrackTypography(owner, tabs, host);
        host.VisibleChanged += (_, _) => host.SyncNativeControl();
        host.LocationChanged += (_, _) => host.SyncNativeControl();
        host.SizeChanged += (_, _) => host.SyncNativeControl();
    }

    internal int Count => documents.Count;
    internal bool Visible => host.Visible;
    internal TabControl? NativeTabs => tabs;

    internal void Synchronize()
    {
        if (disposed)
            return;

        syncing = true;
        try
        {
            var active = owner.ActiveMdiChild;
            if (active != null && !active.IsDisposed && documents.All(document => document.Editor != active))
            {
                var character = active switch
                {
                    frmCreate creation => creation.DocumentCharacter,
                    frmCareer career => career.DocumentCharacter,
                    _ => null
                };
                var header = tabs == null ? null : new TabItem
                {
                    Tag = active,
                    HeaderTemplate = HeaderTemplate
                };
                if (header != null)
                {
                    NativeEditorTabStrip.BindHeaderTypography(tabs!, header);
                    SetTitle(header, Title(active, character));
                }
                documents.Add(new Document(active, character, header));
                if (header != null)
                    tabs!.Items.Add(header);
                if (character != null)
                    character.CharacterNameChanged += UpdateTitle;
                active.FormClosed += DocumentClosed;
            }

            host.Visible = active != null && documents.Count > 1;
            if (tabs != null)
            {
                var selected = documents.Find(document => document.Editor == active)?.Header;
                tabs.SelectedItem = selected;
                if (host.Visible && selected != null)
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (tabs.SelectedItem == selected)
                            selected.BringIntoView();
                    }, DispatcherPriority.Loaded);
            }
        }
        finally { syncing = false; }
    }

    private void DocumentClosed(object? sender, Forms.FormClosedEventArgs args)
    {
        if (sender is not Forms.Form editor)
            return;

        var document = documents.Find(candidate => candidate.Editor == editor);
        if (document == null)
            return;

        syncing = true;
        try
        {
            Unsubscribe(document);
            documents.Remove(document);
            if (document.Header != null)
                tabs!.Items.Remove(document.Header);
        }
        finally { syncing = false; }

        editor.Dispose();
        Synchronize();
    }

    private void UpdateTitle(object sender)
    {
        foreach (var document in documents.Where(document => ReferenceEquals(document.Character, sender)))
            if (document.Header != null)
                SetTitle(document.Header, Title(document.Editor, document.Character));
    }

    private static void SetTitle(TabItem header, string title)
    {
        header.Header = title;
        ToolTip.SetTip(header, title);
    }

    private static string Title(Forms.Form editor, Character? character)
    {
        string? title = character == null ? editor.Text : string.IsNullOrWhiteSpace(character.Alias) ? character.Name : character.Alias;
        return string.IsNullOrWhiteSpace(title) ? LanguageManager.Instance.GetString("String_UnnamedCharacter") : title;
    }

    private void Unsubscribe(Document document)
    {
        document.Editor.FormClosed -= DocumentClosed;
        if (document.Character != null)
            document.Character.CharacterNameChanged -= UpdateTitle;
        if (document.Header != null)
            document.Header.Tag = null;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        syncing = true;
        foreach (var document in documents)
            Unsubscribe(document);
        documents.Clear();
        tabs?.Items.Clear();
        host.NativeControl = null;
    }
}
