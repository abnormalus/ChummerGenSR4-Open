#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;
namespace Chummer;

// Capture settings at opening. Choosing a different build method uses that
// profile's existing default, or the established 400 BP / 750 Karma fallback.
internal sealed class BuildPointConfiguration
{
    internal CharacterBuildMethod Method { get; }
    internal bool MethodLocked { get; }
    internal int Points { get; }
    internal int Availability { get; }
    internal bool IgnoreRules { get; }
    private readonly string preferred;
    private readonly int preferredPoints;
    internal BuildPointConfiguration(Character character, bool current)
    {
        preferred = character.Options.BuildMethod; preferredPoints = character.Options.BuildPoints;
        MethodLocked = current;
        Method = current ? character.BuildMethod : preferred == "Karma" ? CharacterBuildMethod.Karma : CharacterBuildMethod.BP;
        Points = Math.Clamp(current ? Method == CharacterBuildMethod.Karma ? character.BuildKarma : character.BuildPoints : DefaultPoints(Method), 0, 5000);
        Availability = Math.Clamp(current ? character.MaximumAvailability : character.Options.Availability, 0, 100);
        IgnoreRules = current && character.IgnoreRules;
    }
    internal int DefaultPoints(CharacterBuildMethod method) => preferred == (method == CharacterBuildMethod.BP ? "BP" : "Karma") ? preferredPoints : method == CharacterBuildMethod.BP ? 400 : 750;
}
internal sealed record BuildPointSelection(CharacterBuildMethod Method, int Points, int Availability, bool IgnoreRules)
{
    internal void ApplyTo(Character character)
    {
        if (Points < 0 || Points > 5000 || Availability < 0 || Availability > 100 || (Method != CharacterBuildMethod.BP && Method != CharacterBuildMethod.Karma))
            throw new ArgumentOutOfRangeException(nameof(Points), "Build limits are outside the supported editor range.");
        if (Method == CharacterBuildMethod.BP) { character.BuildPoints = Points; character.NuyenMaximumBP = 50; }
        else { character.BuildPoints = 0; character.BuildKarma = Points; character.NuyenMaximumBP = 100; }
        character.BuildMethod = Method; character.MaximumAvailability = Availability; character.IgnoreRules = IgnoreRules;
    }
}
public sealed class frmSelectBP : IDisposable
{
    private readonly Character character;
    private readonly BuildPointConfiguration configuration;
    private BuildPointWindow? window;
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;
    public frmSelectBP(Character character, bool blnUseCurrentValues = false) { this.character = character; configuration = new(character, blnUseCurrentValues); }
    public Forms.DialogResult ShowDialog(Forms.Form? owner = null)
    {
        owner ??= Forms.Form.ActiveForm ?? Forms.Application.OpenForms.Cast<Forms.Form>().LastOrDefault(form => form.Visible);
        if (owner == null) throw new InvalidOperationException("Build-point selection requires an open owner.");
        var pending = ShowDialogAsync(owner); Forms.Backends.Platform.Backend.RunModalLoop(pending); return pending.GetAwaiter().GetResult();
    }
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Build-point selection is already open.");
        DialogResult = Forms.DialogResult.Cancel; window = new BuildPointWindow(configuration);
        try
        {
            var selection = await NativeDialogHost.ShowAsync<BuildPointSelection>(window, owner);
            if (selection != null) { selection.ApplyTo(character); DialogResult = Forms.DialogResult.OK; }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() => window?.Close();
}
internal sealed class BuildPointWindow : Window
{
    internal ComboBox MethodInput { get; }
    internal NumericUpDown PointsInput { get; } = NativeIntegerInput.Create("buildPoints", 0, 5000);
    internal NumericUpDown AvailabilityInput { get; } = NativeIntegerInput.Create("maximumAvailability", 0, 100);
    internal CheckBox IgnoreRulesInput { get; }
    private readonly BuildPointConfiguration configuration;
    private readonly TextBlock description = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private CharacterBuildMethod Method => configuration.MethodLocked ? configuration.Method : MethodInput.SelectedIndex == 1 ? CharacterBuildMethod.Karma : CharacterBuildMethod.BP;
    internal BuildPointWindow(BuildPointConfiguration configuration)
    {
        this.configuration = configuration;
        Title = LanguageManager.Instance.GetString("Title_SelectBP"); Width = 520; MinWidth = 380; SizeToContent = SizeToContent.Height; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);
        MethodInput = new ComboBox { ItemsSource = new[] { LanguageManager.Instance.GetString("String_BP"), LanguageManager.Instance.GetString("String_Karma") }, SelectedIndex = configuration.Method == CharacterBuildMethod.Karma ? 1 : 0, IsEnabled = !configuration.MethodLocked, HorizontalAlignment = HorizontalAlignment.Stretch };
        PointsInput.Value = configuration.Points; AvailabilityInput.Value = configuration.Availability;
        IgnoreRulesInput = new CheckBox { Content = LanguageManager.Instance.GetString("Checkbox_SelectBP_IgnoreRules"), IsChecked = configuration.IgnoreRules };
        ToolTip.SetTip(IgnoreRulesInput, LanguageManager.Instance.GetString("Tip_SelectBP_IgnoreRules"));
        MethodInput.SelectionChanged += (_, _) => { if (!configuration.MethodLocked) PointsInput.Value = configuration.DefaultPoints(Method); RefreshDescription(); };
        var layout = new StackPanel { Margin = new Thickness(20), Spacing = 14 };
        layout.Children.Add(description); layout.Children.Add(MethodInput); layout.Children.Add(PointsInput);
        layout.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString("Label_SelectBP_MaxAvail") }); layout.Children.Add(AvailabilityInput); layout.Children.Add(IgnoreRulesInput);
        var cancel = new Button { Content = LanguageManager.Instance.GetString("String_Cancel"), IsCancel = true }; cancel.Click += (_, _) => Close();
        var accept = new Button { Content = LanguageManager.Instance.GetString("String_OK"), IsDefault = true }; accept.Click += (_, _) => Accept();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        buttons.Children.Add(cancel); buttons.Children.Add(accept); layout.Children.Add(buttons); Content = layout;
        Opened += (_, _) => PointsInput.Focus(); RefreshDescription();
    }
    private void RefreshDescription() => description.Text = LanguageManager.Instance.GetString(Method == CharacterBuildMethod.BP ? "String_SelectBP_BPSummary" : "String_SelectBP_KarmaSummary").Replace("{0}", configuration.DefaultPoints(Method).ToString(GlobalOptions.Instance.CultureInfo));
    internal void Accept() => Close(new BuildPointSelection(Method, (int)(PointsInput.Value ?? 0), (int)(AvailabilityInput.Value ?? 0), IgnoreRulesInput.IsChecked == true));
}
