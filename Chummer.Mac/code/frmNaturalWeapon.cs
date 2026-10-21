#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;
namespace Chummer;

// Keep catalog identities and legacy damage/AP notation independent of layout.
internal sealed record NaturalWeaponSelection(string Name, string Skill, string DamageBase, int Modifier, string DamageType, int AP, int Reach)
{
    internal string Damage => DamageBase + (Modifier == 0 ? "" : Modifier.ToString("+0;-0", CultureInfo.InvariantCulture)) + DamageType;
    internal string ArmorPenetration => AP == 0 ? "0" : AP.ToString("+0;-0", CultureInfo.InvariantCulture);
    internal Weapon Create(Character character)
    {
        var power = XmlManager.Instance.Load("critterpowers.xml").SelectSingleNode("/chummer/powers/power[name = 'Natural Weapon']")
            ?? throw new InvalidOperationException("Natural Weapon catalog definition is missing.");
        return new Weapon(character) { Name = Name, Category = LanguageManager.Instance.GetString("Tab_Critter"), WeaponType = "Melee",
            Reach = Reach, Damage = Damage, AP = ArmorPenetration, Mode = "0", RC = "0", Concealability = 0, Avail = "0", Cost = 0,
            UseSkill = Skill, Source = power["source"]!.InnerText, Page = power["page"]!.InnerText };
    }
}
public sealed class frmNaturalWeapon : IDisposable
{
    private readonly Character character;
    private NaturalWeaponWindow? window;
    private Weapon? selectedWeapon;
    public frmNaturalWeapon(Character objCharacter) => character = objCharacter;
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;
    public Weapon SelectedWeapon => selectedWeapon ?? throw new InvalidOperationException("No natural weapon was accepted.");
    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner); Forms.Backends.Platform.Backend.RunModalLoop(pending); return pending.GetAwaiter().GetResult();
    }
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Natural weapon editor is already open.");
        DialogResult = Forms.DialogResult.Cancel; selectedWeapon = null; window = new NaturalWeaponWindow();
        try
        {
            var selection = await NativeDialogHost.ShowAsync<NaturalWeaponSelection>(window, owner);
            // Preparation creates an unattached weapon. Only the accepted caller
            // inserts it into inventory and marks its character dirty.
            if (selection != null) { selectedWeapon = selection.Create(character); DialogResult = Forms.DialogResult.OK; }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() => window?.Close();
}
internal sealed class NaturalWeaponWindow : Window
{
    private sealed record Choice(string Value, string Label) { public override string ToString() => Label; }
    internal TextBox NameInput { get; } = new();
    internal ComboBox SkillInput { get; }
    internal ComboBox DamageBaseInput { get; }
    internal ComboBox DamageTypeInput { get; }
    internal NumericUpDown ModifierInput { get; } = NativeIntegerInput.Create("naturalDamageModifier", -10, 10);
    internal NumericUpDown APInput { get; } = NativeIntegerInput.Create("naturalArmorPenetration", -10, 10);
    internal NumericUpDown ReachInput { get; } = NativeIntegerInput.Create("naturalReach", -3, 3);
    internal Button AcceptButton { get; }
    internal NaturalWeaponWindow()
    {
        string Text(string key) => LanguageManager.Instance.GetString(key);
        ModifierInput.Value = 0; APInput.Value = 0; ReachInput.Value = 0;
        APInput.HorizontalAlignment = HorizontalAlignment.Left; ReachInput.HorizontalAlignment = HorizontalAlignment.Left;
        Title = Text("Title_CreateNaturalWeapon"); Width = 560; MinWidth = 440; SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; NativeControlPresentation.AddTheme(Styles);
        var skills = XmlManager.Instance.Load("skills.xml").SelectNodes("/chummer/skills/skill[category = 'Combat Active']")!.Cast<XmlNode>()
            .Select(node => new Choice(node["name"]!.InnerText, node["translate"]?.InnerText ?? node["name"]!.InnerText)).ToArray();
        SkillInput = new ComboBox { ItemsSource = skills, SelectedIndex = skills.Length == 0 ? -1 : 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        DamageBaseInput = new ComboBox { ItemsSource = new[] { new Choice("(STR/2)", "(" + Text("String_AttributeSTRShort") + "/2)") }
            .Concat(Enumerable.Range(1, 20).Select(n => new Choice(n.ToString(CultureInfo.InvariantCulture), n.ToString(CultureInfo.CurrentCulture)))).ToArray(), SelectedIndex = 0, MinWidth = 110 };
        DamageTypeInput = new ComboBox { ItemsSource = new[] { new Choice("P", Text("String_DamagePhysical")), new Choice("S", Text("String_DamageStun")) }, SelectedIndex = 0, MinWidth = 110 };
        var layout = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 12 };
        void Add(string label, Control input) { layout.Children.Add(new TextBlock { Text = Text(label) }); layout.Children.Add(input); }
        Add("Label_Name", NameInput); Add("Label_ActiveSkill", SkillInput);
        var damage = new WrapPanel();
        foreach (var input in new Control[] { DamageBaseInput, ModifierInput, DamageTypeInput }) { input.Margin = new Avalonia.Thickness(0, 0, 10, 8); damage.Children.Add(input); }
        Add("Label_DV", damage); Add("Label_AP", APInput); Add("Label_Reach", ReachInput);
        var cancel = new Button { Content = Text("String_Cancel"), IsCancel = true }; cancel.Click += (_, _) => Close();
        AcceptButton = new Button { Content = Text("String_OK"), IsDefault = true, IsEnabled = skills.Length != 0 }; AcceptButton.Click += (_, _) => Accept();
        SkillInput.SelectionChanged += (_, _) => AcceptButton.IsEnabled = SkillInput.SelectedItem is Choice;
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        buttons.Children.Add(cancel); buttons.Children.Add(AcceptButton); layout.Children.Add(buttons); Content = layout;
        Opened += (_, _) => NameInput.Focus();
    }
    internal void Accept()
    {
        if (SkillInput.SelectedItem is not Choice skill || DamageBaseInput.SelectedItem is not Choice damageBase || DamageTypeInput.SelectedItem is not Choice damageType) return;
        Close(new NaturalWeaponSelection(NameInput.Text ?? "", skill.Value, damageBase.Value, (int)(ModifierInput.Value ?? 0), damageType.Value, (int)(APInput.Value ?? 0), (int)(ReachInput.Value ?? 0)));
    }
}
