#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

public sealed class frmSelectExoticSkill : IDisposable
{
    private NativeChoiceWindow? window;

    public string SelectedExoticSkill { get; private set; } = "";
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;

    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner);
        Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }

    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null)
            throw new InvalidOperationException("The exotic skill picker is already open.");

        SelectedExoticSkill = "";
        DialogResult = Forms.DialogResult.Cancel;
        var skills = new List<ListItem>();
        foreach (XmlNode skill in XmlManager.Instance.Load("skills.xml").SelectNodes("/chummer/skills/skill[exotic = 'Yes']")!)
        {
            string name = skill["name"]!.InnerText;
            skills.Add(new ListItem { Value = name, Name = skill["translate"]?.InnerText ?? name });
        }
        skills.Sort(new SortListItem().Compare);
        var language = LanguageManager.Instance;
        string title = language.GetString("Title_SelectExoticSkill");
        window = new NativeChoiceWindow(skills, title, language.GetString("Label_SelectExoticSkill_Description"), title);
        try
        {
            var selected = await NativeDialogHost.ShowAsync<ListItem>(window, owner);
            if (selected != null)
            {
                SelectedExoticSkill = selected.Value;
                DialogResult = Forms.DialogResult.OK;
            }
            return DialogResult;
        }
        finally { window = null; }
    }

    public void Dispose() => window?.Close();
}
