#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal sealed class CategoryPicker(CategoryPicker.Catalog catalog) : IDisposable
{
    internal enum Catalog { Skill, Spell, Weapon }

    private NativeChoiceWindow? window;

    internal string Description { private get; set; } = "";
    internal string OnlyCategory { private get; set; } = "";
    internal string SelectedCategory { get; private set; } = "";

    internal Forms.DialogResult ShowDialog(Forms.Form? owner = null)
    {
        var pending = ShowDialogAsync(owner);
        if (!pending.IsCompleted)
            Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }

    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form? owner = null)
    {
        if (window != null)
            throw new InvalidOperationException("The category picker is already open.");

        SelectedCategory = "";
        var choices = XmlManager.Instance.Load(catalog.ToString().ToLowerInvariant() + "s.xml")
            .SelectNodes("/chummer/categories/category")!.Cast<XmlNode>()
            .Where(node => OnlyCategory.Length == 0 || node.InnerText == OnlyCategory)
            .Select(node => new ListItem { Value = node.InnerText, Name = node.Attributes?["translate"]?.InnerText ?? node.InnerText })
            .ToList();
        if (catalog == Catalog.Weapon && (OnlyCategory.Length == 0 || OnlyCategory == "Cyberware"))
            choices.Add(new ListItem { Value = "Cyberware", Name = "Cyberware" });

        if (choices.Count == 0)
            return Forms.DialogResult.Cancel;

        ListItem? selected;
        if (choices.Count == 1)
            selected = choices[0];
        else
        {
            owner ??= Forms.Form.ActiveForm ?? Forms.Application.OpenForms.Cast<Forms.Form>().LastOrDefault(form => form.Visible)
                ?? throw new InvalidOperationException("A category picker requires an open owner window.");

            string title = LanguageManager.Instance.GetString("Title_Select" + catalog + "Category");
            window = new NativeChoiceWindow(choices, title, Description, title);
            try { selected = await NativeDialogHost.ShowAsync<ListItem>(window, owner); }
            finally { window = null; }
        }

        if (selected == null)
            return Forms.DialogResult.Cancel;
        SelectedCategory = selected.Value;
        return Forms.DialogResult.OK;
    }

    public void Dispose() => window?.Close();
}
