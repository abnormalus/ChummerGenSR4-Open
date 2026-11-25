#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

public sealed class frmSelectSetting : IDisposable
{
    private NativeChoiceWindow? window;

    public string SettingsFile { get; private set; } = "default.xml";
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
            throw new InvalidOperationException("The settings picker is already open.");

        SettingsFile = "default.xml";
        DialogResult = Forms.DialogResult.Cancel;
        var profiles = LoadProfiles();
        var language = LanguageManager.Instance;
        int defaultIndex = profiles.FindIndex(profile =>
            string.Equals(profile.Name, "Default Settings", StringComparison.CurrentCultureIgnoreCase));
        window = new NativeChoiceWindow(profiles, language.GetString("Title_SelectSetting"),
            language.GetString("Label_SelectSetting_SettingsDescription"), language.GetString("Label_Options_SettingsFile"), defaultIndex);
        try
        {
            var selected = await NativeDialogHost.ShowAsync<ListItem>(window, owner);
            if (selected != null)
            {
                SettingsFile = selected.Value;
                DialogResult = Forms.DialogResult.OK;
            }
            return DialogResult;
        }
        finally { window = null; }
    }

    private static List<ListItem> LoadProfiles()
    {
        var profiles = new List<ListItem>();
        foreach (var path in Directory.GetFiles(MacPaths.SettingsDirectory, "*.xml"))
        {
            var document = new XmlDocument();
            document.Load(path);
            profiles.Add(new ListItem
            {
                Value = Path.GetFileName(path),
                Name = document.SelectSingleNode("/settings/name")!.InnerText
            });
        }
        profiles.Sort(new SortListItem().Compare);
        return profiles;
    }

    public void Dispose() => window?.Close();
}
