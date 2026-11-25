#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

public sealed class frmSelectSkill(Character character) : IDisposable
{
    private NativeChoiceWindow? window;
    private string forcedSkill = "";

    public string OnlyCategory { private get; set; } = "";
    public string ExcludeCategory { private get; set; } = "";
    public string OnlySkillGroup { private get; set; } = "";
    public string ExcludeSkillGroup { private get; set; } = "";
    public string LimitToSkill { private get; set; } = "";
    public string Description { private get; set; } = "";
    public bool ShowKnowledgeSkills { private get; set; }
    public string SelectedSkill { get; private set; } = "";
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;

    public string OnlySkill
    {
        set => forcedSkill = value.Replace(", " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked"), "");
    }

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
            throw new InvalidOperationException("The skill picker is already open.");

        SelectedSkill = "";
        DialogResult = Forms.DialogResult.Cancel;
        var choices = LoadChoices();
        if (choices.Count == 0)
            return DialogResult;
        if (choices.Count == 1)
            return Accept(choices[0]);

        owner ??= Forms.Form.ActiveForm ?? Forms.Application.OpenForms.Cast<Forms.Form>().LastOrDefault(form => form.Visible)
            ?? throw new InvalidOperationException("A skill picker requires an open owner window.");

        string title = LanguageManager.Instance.GetString("Title_SelectSkill");
        window = new NativeChoiceWindow(choices, title, Description, title);
        try
        {
            var selected = await NativeDialogHost.ShowAsync<ListItem>(window, owner);
            return selected == null ? DialogResult : Accept(selected);
        }
        finally { window = null; }
    }

    private Forms.DialogResult Accept(ListItem choice)
    {
        SelectedSkill = choice.Value;
        return DialogResult = Forms.DialogResult.OK;
    }

    private List<ListItem> LoadChoices()
    {
        var choices = ShowKnowledgeSkills
            ? character.Skills.Where(skill => skill.KnowledgeSkill)
                .Select(skill => new ListItem { Value = skill.Name, Name = skill.DisplayName }).ToList()
            : LoadActiveChoices();
        choices.Sort(new SortListItem().Compare);
        return choices;
    }

    private List<ListItem> LoadActiveChoices()
    {
        var catalog = XmlManager.Instance.Load("skills.xml").SelectNodes("/chummer/skills/skill")!.Cast<XmlNode>().ToArray();
        var exclusions = ExcludeCategory.Split(',').Select(category => category.Trim()).ToHashSet(StringComparer.Ordinal);
        var limits = LimitToSkill.Split(',').Select(name => name.Trim()).ToHashSet(StringComparer.Ordinal);
        var choices = new List<ListItem>();
        foreach (var skill in catalog.Where(skill => skill["exotic"] == null))
        {
            var name = skill["name"]!.InnerText;
            if (Matches(name, skill["category"]?.InnerText, skill["skillgroup"]?.InnerText))
                choices.Add(new ListItem { Value = name, Name = skill["translate"]?.InnerText ?? name });
        }

        foreach (var skill in character.Skills.Where(skill => skill.ExoticSkill))
        {
            string value = $"{skill.Name} ({skill.Specialization})";
            if (!Matches(skill.Name, skill.SkillCategory, skill.SkillGroup, value))
                continue;

            var definition = Array.Find(catalog, node => node["exotic"]?.InnerText == "Yes" && node["name"]?.InnerText == skill.Name);
            var label = definition?["translate"]?.InnerText ?? skill.Name;
            choices.Add(new ListItem { Value = value, Name = $"{label} ({skill.Specialization})" });
        }
        return choices;

        bool Matches(string name, string? category, string? group, string? exoticValue = null)
        {
            if (forcedSkill.Length != 0)
                return forcedSkill == (exoticValue ?? name);
            if (OnlyCategory.Length != 0)
                return OnlyCategory == category;
            if (ExcludeCategory.Length != 0)
                return category != null && (exoticValue != null ? !ExcludeCategory.Contains(category) : !exclusions.Contains(category));
            if (OnlySkillGroup.Length != 0)
                return OnlySkillGroup == group;
            if (ExcludeSkillGroup.Length != 0)
                return group != null && ExcludeSkillGroup != group;
            return LimitToSkill.Length == 0 || (exoticValue != null ? LimitToSkill.Contains(name) : limits.Contains(name));
        }
    }

    public void Dispose() => window?.Close();
}
