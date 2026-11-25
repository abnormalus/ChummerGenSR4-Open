#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

public sealed class frmSelectSkillGroup : IDisposable
{
    private NativeChoiceWindow? window;

    public string Description { private get; set; } = "";
    public string OnlyGroup { private get; set; } = "";
    public string ExcludeCategory { private get; set; } = "";
    public string SelectedSkillGroup { get; private set; } = "";
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;

    public Forms.DialogResult ShowDialog(Forms.Form? owner = null)
    {
        var pending = ShowDialogAsync(owner);
        if (!pending.IsCompleted)
            Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }

    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form? owner = null)
    {
        if (window != null)
            throw new InvalidOperationException("The skill group picker is already open.");

        SelectedSkillGroup = "";
        DialogResult = Forms.DialogResult.Cancel;
        if (!string.IsNullOrEmpty(OnlyGroup))
            return Accept(OnlyGroup);

        var groups = LoadGroups();
        if (groups.Count == 0)
            return DialogResult;
        if (groups.Count == 1)
            return Accept(groups[0].Value);

        owner ??= Forms.Form.ActiveForm ?? Forms.Application.OpenForms.Cast<Forms.Form>().LastOrDefault(form => form.Visible)
            ?? throw new InvalidOperationException("A skill group picker requires an open owner window.");

        string title = LanguageManager.Instance.GetString("Title_SelectSkillGroup");
        window = new NativeChoiceWindow(groups, title, Description, title);
        try
        {
            var selected = await NativeDialogHost.ShowAsync<ListItem>(window, owner);
            return selected == null ? DialogResult : Accept(selected.Value);
        }
        finally { window = null; }
    }

    private Forms.DialogResult Accept(string group)
    {
        SelectedSkillGroup = group;
        return DialogResult = Forms.DialogResult.OK;
    }

    private List<ListItem> LoadGroups()
    {
        var document = XmlManager.Instance.Load("skills.xml");
        HashSet<string>? includedGroups = null;
        if (!string.IsNullOrEmpty(ExcludeCategory))
        {
            var excluded = ExcludeCategory.Split(',').Select(category => category.Trim()).ToHashSet(StringComparer.Ordinal);
            includedGroups = document.SelectNodes("/chummer/skills/skill")!.Cast<XmlNode>()
                .Where(skill => skill["category"] is { } category && !excluded.Contains(category.InnerText))
                .Select(skill => skill["skillgroup"]?.InnerText ?? "").ToHashSet(StringComparer.Ordinal);
        }

        var groups = document.SelectNodes("/chummer/skillgroups/name")!.Cast<XmlNode>()
            .Where(group => includedGroups == null || includedGroups.Contains(group.InnerText))
            .Select(group => new ListItem { Value = group.InnerText, Name = group.Attributes?["translate"]?.InnerText ?? group.InnerText })
            .ToList();
        groups.Sort(new SortListItem().Compare);
        return groups;
    }

    public void Dispose() => window?.Close();
}
