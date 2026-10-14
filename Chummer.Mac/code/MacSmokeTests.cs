using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Xsl;
using System.Text;
using Majorsilence.Forms;
using Majorsilence.Forms.Headless;

namespace Chummer
{
    internal static class MacSmokeTests
    {
        public class RetinaScreenBackend : DispatchProxy
        {
            public Majorsilence.Forms.Backends.IWindowBackend Inner;
            protected override object Invoke(MethodInfo method, object[] arguments)
                => method.Name == "get_Scaling" ? 2.0 : method.Invoke(Inner, arguments);
        }
        private static T Field<T>(object instance, string name) =>
            (T)instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            Console.WriteLine("PASS " + message);
        }
        private static void Capture(Form form, string output, string name)
        {
            form.Show();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), HeadlessRenderer.CapturePng(form, 1200, 850));
            Console.WriteLine("PASS render " + name);
        }
        public static int Run(string[] args)
        {
            if (args.Length < 2) { Console.Error.WriteLine("Usage: --smoke-test OUTPUT [FIXTURE]"); return 2; }
            string output = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(output);
            MacPaths.TestUserDirectory = Path.Combine(output, "user");
            try
            {
                HeadlessRenderer.Use();
                MacPaths.Initialize();
                GlobalOptions.Instance.AutomaticUpdate = false;
                LanguageManager.Instance.Load("en-us", null);
                Check(LanguageManager.Instance.Loaded, "English language loaded");
                AppDiagnostics.Action("diagnostics.privacy-test");
                AppDiagnostics.Record("simulated-error", new System.IO.IOException("private-character-name /private/character.chum"));
                string diagnosticText = File.ReadAllText(AppDiagnostics.LogPath);
                Check(diagnosticText.Contains("IOException") && diagnosticText.Contains("diagnostics.privacy-test") &&
                    !diagnosticText.Contains("private-character-name") && !diagnosticText.Contains("character.chum"),
                    "diagnostics retain error types and action IDs without exception messages or character paths");
                File.WriteAllText(AppDiagnostics.LogPath, new string('x', 2 * 1024 * 1024 + 1));
                AppDiagnostics.Record("rotation-test");
                Check(File.Exists(AppDiagnostics.LogPath + ".1") && new FileInfo(AppDiagnostics.LogPath).Length < 4096,
                    "diagnostic logs rotate before growing without bound");
                Check(Form.ApplicationIconOverride != null && Form.ApplicationIconOverride.ToBitmap().Width > 0,
                    "shared dragon application icon decodes");
                using (var branding = new frmAbout())
                    Check(Field<PictureBox>(branding, "logoPictureBox").Image.Width == 1254,
                        "About uses the supplied dragon emblem");
                Check(Properties.Resources.add != null, "embedded toolbar image decoded");
                foreach (string file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data"), "*.xml"))
                    XmlManager.Instance.Load(Path.GetFileName(file));
                Console.WriteLine("PASS ruleset XML loads");
                using (var refreshForm = new Form { Width = 400, Height = 140 })
                {
                    var refreshTree = new TreeView { Dock = DockStyle.Fill };
                    var refreshNode = new TreeNode("Addiction (Mild) (<placeholder>)");
                    refreshTree.Nodes.Add(refreshNode);
                    refreshForm.Controls.Add(refreshTree);
                    refreshForm.Show();
                    byte[] beforeEdit = HeadlessRenderer.CapturePng(refreshForm, 800, 280);
                    refreshNode.Text = "Addiction (Mild) (DRUGS)";
                    Check((bool)typeof(Control).GetProperty("NeedsPaint", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(refreshTree), "changing a tree detail immediately requests a redraw");
                    byte[] afterEdit = HeadlessRenderer.CapturePng(refreshForm, 800, 280);
                    Check(!beforeEdit.SequenceEqual(afterEdit), "edited detail appears in the next frame without mouse interaction");
                }
                using (var blankPrompt = new frmSelectText { AutoAcceptPrefilled = false, SelectedValue = "  " })
                {
                    blankPrompt.Show();
                    Field<Button>(blankPrompt, "cmdOK").PerformClick();
                    Check(blankPrompt.SelectedValue == PromptDetails.Placeholder,
                        "blank shared free-text prompt accepts the literal placeholder");
                }
                using (var editPrompt = new frmSelectText { AutoAcceptPrefilled = false, SelectedValue = PromptDetails.Placeholder })
                {
                    editPrompt.Show();
                    Check(editPrompt.DialogResult != DialogResult.OK, "prefilled detail editor stays open for editing");
                    Field<TextBox>(editPrompt, "txtValue").Text = "Later detail";
                    Field<Button>(editPrompt, "cmdOK").PerformClick();
                    Check(editPrompt.SelectedValue == "Later detail", "shared prompt accepts edited detail");
                }

                var choiceCharacter = new Character();
                choiceCharacter.Options.Books.Clear();
                choiceCharacter.Options.Books.Add("SR4");
                var coreChoices = DetailChoices.ForQuality(choiceCharacter, "Codeslinger");
                Check(coreChoices.Length == 23 && !coreChoices.Any(item => item.Name == "Corrupt Data"),
                    "Codeslinger hides Unwired actions when its source is disabled");
                Check(coreChoices.All(item => !item.ToString().Contains("SR4A") && !item.ToString().Contains("Unwired")),
                    "choice display hides sourcebook metadata while settings still filter the list");
                Check(DetailChoices.RequiresReview("Codeslinger", "Control Device") &&
                    !DetailChoices.RequiresReview("Codeslinger", "Hack on the Fly"),
                    "review styling follows the saved action name");
                Check(coreChoices.Count(item => item.Review) == 5, "all five review-required actions retain their styling flag");
                choiceCharacter.Options.Books.Add("UN");
                Check(DetailChoices.ForQuality(choiceCharacter, "Codeslinger").Length == 26,
                    "Codeslinger includes Unwired actions when enabled");
                using (var choicesPrompt = new frmSelectText { AutoAcceptPrefilled = false })
                {
                    choicesPrompt.ConfigureChoices(choiceCharacter, "Codeslinger");
                    choicesPrompt.Show();
                    var combo = Field<ComboBox>(choicesPrompt, "detailChoices");
                    var input = (TextBox)typeof(ComboBox).GetField("edit", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(combo);
                    input.Text = "hack";
                    Check(combo.DroppedDown, "typing into the editable region opens filtered results");
                    var popup = typeof(ComboBox).GetField("popup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(combo);
                    Check(!(bool)typeof(PopupWindow).GetProperty("ShowsActivated", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(popup), "searchable results do not activate and steal typing from the input");
                    Check(combo.Items.Count == 1 && combo.Items[0].ToString().StartsWith("Hack on the Fly"),
                        "Codeslinger typing narrows the list case-insensitively");
                    typeof(Control).GetMethod("RaiseKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(combo, new object[] { new KeyEventArgs(Keys.Down) });
                    Check(combo.SelectedIndex == 0, "Down selects the first filtered match without an out-of-range focus");
                    Field<Button>(choicesPrompt, "cmdOK").PerformClick();
                    Check(choicesPrompt.SelectedValue == "Hack on the Fly", "Codeslinger stores only the action name in the existing detail field");
                }

                using (var genericPrompt = new frmSelectText { AutoAcceptPrefilled = false })
                {
                    genericPrompt.ConfigureChoices(new[] { new DetailChoice { Name = "Pollen" },
                        new DetailChoice { Name = "Dust", Review = true } });
                    genericPrompt.Show();
                    var combo = Field<ComboBox>(genericPrompt, "detailChoices");
                    combo.SelectedIndex = 0;
                    combo.Text = "pol";
                    Check(combo.Items.Count == 1 && combo.Items[0].ToString() == "Pollen",
                        "generic detail picker can filter new text after an earlier selection");
                    combo.Text = "";
                    Check(combo.Items.Count == 2, "clearing a detail filter restores all choices");
                    Field<Button>(genericPrompt, "cmdOK").PerformClick();
                    Check(genericPrompt.SelectedValue == PromptDetails.Placeholder,
                        "blank searchable detail retains the shared placeholder behavior");
                }

                var recordsChoices = DetailChoices.ForQuality(choiceCharacter, "Records on File");
                Check(recordsChoices.Length == 52 && recordsChoices.Any(item => item.Name == "Republic of Québec") &&
                    recordsChoices.Any(item => item.Name == "Fuchi Industrial Electronics"),
                    "Records on File includes all supplied organizations, Unicode names and legacy records");
                using (var recordsPrompt = new frmSelectText { AutoAcceptPrefilled = false })
                {
                    recordsPrompt.ConfigureChoices(choiceCharacter, "Records on File");
                    recordsPrompt.Show();
                    var combo = Field<ComboBox>(recordsPrompt, "detailChoices");
                    combo.Text = "ares";
                    Check(combo.Items.Count == 1, "Records on File search narrows the supplied suggestions");
                    combo.SelectedIndex = 0;
                    Field<Button>(recordsPrompt, "cmdOK").PerformClick();
                    Check(recordsPrompt.SelectedValue == "Ares Macrotechnology", "Records on File stores the organization name as the existing detail");
                }
                using (var customRecords = new frmSelectText { AutoAcceptPrefilled = false })
                {
                    customRecords.ConfigureChoices(choiceCharacter, "Records on File");
                    customRecords.Show();
                    Field<ComboBox>(customRecords, "detailChoices").Text = "My custom records holder";
                    Field<Button>(customRecords, "cmdOK").PerformClick();
                    Check(customRecords.SelectedValue == "My custom records holder", "Records on File accepts custom entries beyond the supplied list");
                }

                using (var portraitPixels = new SkiaSharp.SKBitmap(200, 100))
                {
                    using (var canvas = new SkiaSharp.SKCanvas(portraitPixels))
                    using (var paint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Blue })
                    {
                        canvas.Clear(SkiaSharp.SKColors.Red);
                        canvas.DrawRect(new SkiaSharp.SKRect(100, 0, 200, 100), paint);
                    }
                    var fullPortrait = PortraitImage.Export(portraitPixels, new SkiaSharp.SKRect(0, 0, 200, 100), 100);
                    File.WriteAllBytes(Path.Combine(output, "portrait-input.png"), fullPortrait);
                    using var full = PortraitImage.Decode(fullPortrait);
                    Check(full.Width == 200 && full.Height == 100, "portrait accepts arbitrary image dimensions without the legacy fixed size");
                    var croppedPortrait = PortraitImage.Export(portraitPixels, new SkiaSharp.SKRect(100, 0, 200, 100), 50);
                    using var cropped = PortraitImage.Decode(croppedPortrait);
                    Check(cropped.Width == 50 && cropped.Height == 50 && cropped.GetPixel(25, 25).Blue > 200,
                        "portrait crop preserves the selected region and applies output scaling");
                    var clamped = PortraitImage.ClampCrop(new SkiaSharp.SKRect(250, 120, -20, -10), 200, 100);
                    Check(clamped.Left == 0 && clamped.Top == 0 && clamped.Right == 200 && clamped.Bottom == 100,
                        "portrait crop handles reversed drags and clamps to image boundaries");
                    var portraitCharacter = new Character { Mugshot = Convert.ToBase64String(croppedPortrait),
                        FileName = Path.Combine(output, "portrait-roundtrip.chum") };
                    portraitCharacter.Save();
                    var portraitReload = new Character { FileName = portraitCharacter.FileName };
                    Check(portraitReload.Load() && portraitReload.Mugshot == portraitCharacter.Mugshot,
                        "edited portrait round-trips in the existing embedded character-image field");
                    bool invalidPortraitRejected = false;
                    try { using var invalidPortrait = PortraitImage.Decode(new byte[] { 1, 2, 3 }); }
                    catch (InvalidDataException) { invalidPortraitRejected = true; }
                    Check(invalidPortraitRejected, "invalid portrait bytes are rejected before applying character changes");
                }

                var commonAllergies = DetailChoices.ForQuality(choiceCharacter, "Allergy (Common, Mild)");
                var uncommonAllergies = DetailChoices.ForQuality(choiceCharacter, "Allergy (Uncommon, Severe)");
                Check(commonAllergies.Length == 18 && uncommonAllergies.Length == 32 &&
                    commonAllergies.Any(item => item.Name == "Pollen") && uncommonAllergies.Any(item => item.Name == "Silver"),
                    "allergy suggestions use the supplied recommended rarity across severity levels");
                using (var allergyPrompt = new frmSelectText { AutoAcceptPrefilled = false })
                {
                    allergyPrompt.ConfigureChoices(choiceCharacter, "Allergy (Common, Mild)");
                    allergyPrompt.Show();
                    var combo = Field<ComboBox>(allergyPrompt, "detailChoices");
                    combo.Text = "poll";
                    Check(combo.Items.Count == 2, "allergy search narrows suggestions by contained text");
                    combo.Text = "Custom allergen";
                    Field<Button>(allergyPrompt, "cmdOK").PerformClick();
                    Check(allergyPrompt.SelectedValue == "Custom allergen", "allergy picker retains custom details");
                }
                using (var knowledgeHost = new Form { Width = 900, Height = 160 })
                using (var knowledgeControl = new SkillControl())
                {
                    var knowledge = new Skill(choiceCharacter) { SkillCategory = "Academic", Attribute = "LOG" };
                    knowledgeControl.SkillObject = knowledge;
                    knowledgeControl.RatingChanged += _ => { };
                    knowledgeControl.KnowledgeSkill = true;
                    knowledgeHost.Controls.Add(knowledgeControl);
                    knowledgeHost.Show();
                    var category = Field<ComboBox>(knowledgeControl, "cboKnowledgeSkillCategory");
                    category.SelectedValue = "Interest";
                    var combo = Field<ComboBox>(knowledgeControl, "cboSkillName");
                    Check(KnowledgeSuggestions.Names.Length == 150 && combo.Items.Cast<object>().Any(item => item.ToString() == "Combat Biking"),
                        "both supplied knowledge lists supplement existing skills");
                    Check(category.Left < combo.Left && !combo.Items.Cast<object>().Any(item => item.ToString() == "Seattle Gangs"),
                        "knowledge type precedes the name and curates its suggestions");
                    int fullCount = combo.Items.Count;
                    combo.Text = "biking";
                    Check(combo.Items.Count == 1 && knowledge.Name == "biking", "knowledge search narrows without losing typed text");
                    combo.SelectedIndex = 0;
                    Check(knowledge.Name == "Combat Biking" && knowledge.SkillCategory == "Interest",
                        "selecting a knowledge suggestion preserves the chosen type");
                    combo.Text = "biking";
                    combo.DroppedDown = true;
                    Check(combo.Items.Count == fullCount && combo.Text == "biking", "opening dropdown restores the full type list while retaining text");
                    var browsingPopup = (PopupWindow)typeof(ComboBox).GetField("popup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(combo);
                    Check((bool)typeof(PopupWindow).GetProperty("ShowsActivated", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(browsingPopup),
                        "knowledge browsing popup takes focus for mouse selection");
                    HeadlessRenderer.CapturePng(browsingPopup, 720, 500);
                    var popupWindowType = typeof(Form).BaseType;
                    int choiceX = (int)(20 * browsingPopup.Scaling);
                    int choiceY = (int)(8 * browsingPopup.Scaling);
                    popupWindowType.GetMethod("HandlePointerPressed", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(browsingPopup, new object[] { MouseButtons.Left, choiceX, choiceY, Keys.None });
                    popupWindowType.GetMethod("HandlePointerReleased", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(browsingPopup, new object[] { MouseButtons.Left, choiceX, choiceY, Keys.None });
                    Check(combo.SelectedIndex == 0 && knowledge.Name == combo.Items[0].ToString(),
                        "mouse selection from knowledge dropdown updates the skill name");
                    var nameInput = (TextBox)typeof(ComboBox).GetField("edit", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(combo);
                    nameInput.Text = "biking";
                    Check(combo.Items.Count == 1 && combo.DroppedDown, "typing after browsing switches back to narrowed suggestions");
                    combo.DroppedDown = false;
                    category.SelectedValue = "Street";
                    Check(combo.Items.Cast<object>().Any(item => item.ToString() == "Seattle Gangs") &&
                        !combo.Items.Cast<object>().Any(item => item.ToString() == "Combat Biking"),
                        "changing knowledge type refreshes the full matching list");
                    Check(knowledgeControl.Controls.Find("addKnowledgeSpecialty", false).Length == 1,
                        "knowledge skills offer an explicit specialty button");
                    combo.Text = "My custom knowledge";
                    Check(knowledge.Name == "My custom knowledge", "custom knowledge skill names remain supported");
                    combo.Text = "";
                    Check(combo.Items.Count > 12, "clearing knowledge search restores the full selected type list");
                }

                using (var prefixList = new ListBox())
                {
                    prefixList.Items.AddRange(new object[] { "Alchemy", "Gamma", "Machinery", "Magician", "Magical Theory" });
                    prefixList.SelectedIndex = 0;
                    var keyDown = typeof(Control).GetMethod("RaiseKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (Keys letter in new[] { Keys.M, Keys.A, Keys.G })
                        keyDown.Invoke(prefixList, new object[] { new KeyEventArgs(letter) });
                    Check(prefixList.SelectedIndex == 3, "typing mag on a list selects the first complete prefix match");
                    keyDown.Invoke(prefixList, new object[] { new KeyEventArgs(Keys.Down) });
                    Check(prefixList.SelectedIndex == 4, "arrow navigation still works after a typed prefix");
                    keyDown.Invoke(prefixList, new object[] { new KeyEventArgs(Keys.A) });
                    Check(prefixList.SelectedIndex == 0, "arrow navigation starts a fresh subsequent prefix");
                    typeof(ListBox).GetField("lastPrefixKey", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(prefixList, 0L);
                    keyDown.Invoke(prefixList, new object[] { new KeyEventArgs(Keys.G) });
                    Check(prefixList.SelectedIndex == 1, "idle timeout starts a fresh list prefix");
                }

                foreach (double labelScale in new[] { 1.0, 2.0 })
                {
                    double savedScale = Application.UiScale;
                    try
                    {
                        Application.UiScale = labelScale;
                        using var labelHost = new Form { Width = 300, Height = 120 };
                        var panel = new Panel { Dock = DockStyle.Fill };
                        var valueLabel = new Label { Left = 30, Top = 20, AutoSize = true, Text = "1250¥" };
                        panel.Controls.Add(valueLabel);
                        labelHost.Controls.Add(panel);
                        labelHost.Show();
                        HeadlessRenderer.CapturePng(labelHost, 600, 240);
                        int oldWidth = valueLabel.Width;
                        valueLabel.Text = "750¥";
                        Check(valueLabel.Width < oldWidth && (bool)typeof(Control).GetProperty("NeedsPaint", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel),
                            "shrinking cost redraws the old label background at scale " + labelScale);
                        valueLabel.Text = "12";
                        HeadlessRenderer.CapturePng(labelHost, 600, 240);
                        valueLabel.Text = "8";
                        Check((bool)typeof(Control).GetProperty("NeedsPaint", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel),
                            "shrinking capacity redraws its old trailing digit at scale " + labelScale);
                    }
                    finally { Application.UiScale = savedScale; }
                }

                var originalNotice = AppErrors.Notify;
                int errorNotices = 0;
                try
                {
                    AppErrors.Notify = (_, _, _) => errorNotices++;
                    Application.ThreadException += AppErrors.OnThreadException;
                    using var recoveryHost = new Form { Width = 300, Height = 120 };
                    recoveryHost.Show();
                    var failedCommand = new ToolStripMenuItem("Test command");
                    failedCommand.Click += (_, _) => throw new NullReferenceException("test command");
                    failedCommand.PerformClick();
                    Check(errorNotices == 1 && Application.OpenForms.Contains(recoveryHost),
                        "null-reference command errors show a recoverable notice without closing the app");
                    int followingClicks = 0;
                    var followingCommand = new ToolStripMenuItem("Next command");
                    followingCommand.Click += (_, _) => followingClicks++;
                    followingCommand.PerformClick();
                    Check(followingClicks == 1, "commands still run after a recoverable failure");
                    using var emptyPlugins = new frmSelectCyberware(choiceCharacter) {
                        PluginSelection = true, ShowOnlySubsystems = true, Subsystems = "No matching category" };
                    emptyPlugins.Show();
                    Check(emptyPlugins.DialogResult == DialogResult.Cancel && errorNotices == 2,
                        "no compatible add-ins produces a notice and cancels the picker safely");
                }
                finally
                {
                    Application.ThreadException -= AppErrors.OnThreadException;
                    AppErrors.Notify = originalNotice;
                }

                using (var screenForm = new Form { Width = 600, Height = 200 })
                {
                    var backendField = typeof(Form).BaseType.GetField("Backend", BindingFlags.NonPublic | BindingFlags.Instance);
                    var nativeBackend = (Majorsilence.Forms.Backends.IWindowBackend)backendField.GetValue(screenForm);
                    var retinaBackend = DispatchProxy.Create<Majorsilence.Forms.Backends.IWindowBackend, RetinaScreenBackend>();
                    ((RetinaScreenBackend)(object)retinaBackend).Inner = nativeBackend;
                    backendField.SetValue(screenForm, retinaBackend);
                    var anchor = new Button { Left = 180, Top = 24, Width = 100, Height = 30 };
                    screenForm.Controls.Add(anchor);
                    screenForm.Show();
                    var local = new System.Drawing.Point(13, 7);
                    var expected = nativeBackend.PointToScreen(new System.Drawing.Point(anchor.Left + local.X, anchor.Top + local.Y));
                    Check(anchor.PointToScreen(local) == expected,
                        "Retina screen placement uses desktop points rather than framebuffer pixels");
                    var screenBounds = (System.Drawing.Rectangle)typeof(Form).BaseType.GetProperty("ScreenBounds",
                        BindingFlags.NonPublic | BindingFlags.Instance).GetValue(screenForm);
                    Check(screenBounds.Width == nativeBackend.ClientSize.Width,
                        "dialog centering measures Retina window size in native desktop points");
                    Check(anchor.PointToClient(expected) == local,
                        "Retina screen-to-control conversion reverses native placement");
                }

                using (var centerOwner = new Form { Width = 600, Height = 400, Location = new System.Drawing.Point(120, 80),
                    StartPosition = FormStartPosition.Manual })
                using (var firstDialog = new Form { Width = 200, Height = 100, StartPosition = FormStartPosition.CenterScreen })
                using (var nestedDialog = new Form { Width = 100, Height = 80, StartPosition = FormStartPosition.Manual })
                {
                    Form.CenterModalDialogsOnRootOwner = true;
                    centerOwner.Show();
                    var firstTask = firstDialog.ShowDialogAsync(centerOwner);
                    Check(firstDialog.Location == new System.Drawing.Point(320, 230),
                        "legacy screen-centered dialog opens centered on the moved Chummer owner");
                    firstDialog.Location = new System.Drawing.Point(50, 50);
                    var nestedTask = nestedDialog.ShowDialogAsync(firstDialog);
                    Check(nestedDialog.Location == new System.Drawing.Point(370, 240),
                        "nested dialogs center on the Chummer root window");
                    nestedDialog.DialogResult = DialogResult.Cancel;
                    firstDialog.DialogResult = DialogResult.Cancel;
                }

                double originalScale = Application.UiScale;
                try
                {
                    foreach (double scale in new[] { 1.0, 2.0 })
                    {
                        Application.UiScale = scale;
                        using (var equipmentForm = new Form { Width = 400, Height = 240 })
                        {
                            var equipmentTree = new TreeView { Dock = DockStyle.Fill };
                            var equipmentRoot = new TreeNode("Selected Gear");
                            equipmentRoot.Nodes.Add(new TreeNode("Certified Credstick, Standard (Rating 1) (A long editable equipment detail that must remain accessible)"));
                            equipmentRoot.Expand();
                            equipmentTree.Nodes.Add(equipmentRoot);
                            equipmentForm.Controls.Add(equipmentTree);
                            equipmentForm.Show();
                            byte[] before = HeadlessRenderer.CapturePng(equipmentForm, 400, 240);
                            var horizontal = Field<HorizontalScrollBar>(equipmentTree, "hscrollbar");
                            Check(horizontal.Bottom <= equipmentTree.Height && horizontal.Top >= equipmentTree.Height - horizontal.Height - 5,
                                "horizontal scrollbar stays at the bottom of the equipment viewport at scale " + scale);
                            Check(horizontal.Visible && horizontal.Maximum >= horizontal.LargeChange,
                                "long equipment names enable horizontal scrolling at scale " + scale);
                            horizontal.Value = horizontal.Maximum - horizontal.LargeChange + 1;
                            byte[] after = HeadlessRenderer.CapturePng(equipmentForm, 400, 240);
                            Check(!before.SequenceEqual(after) && equipmentRoot.Bounds.Left < 0,
                                "horizontal scrollbar moves equipment labels at scale " + scale);
                            equipmentRoot.Collapse();
                            HeadlessRenderer.CapturePng(equipmentForm, 400, 240);
                            Check(!horizontal.Visible && horizontal.Value == 0,
                                "collapsing long equipment resets horizontal scrolling at scale " + scale);
                            equipmentRoot.Expand();
                            HeadlessRenderer.CapturePng(equipmentForm, 400, 240);
                            Check(horizontal.Visible, "equipment expansion restores scrolling at scale " + scale);
                        }
                        using (var statusForm = new Form { Width = 800, Height = 120 })
                        {
                            var status = new StatusStrip();
                            var amount = new ToolStripStatusLabel { Text = "0¥", Size = new System.Drawing.Size(23, 19) };
                            status.Items.Add(amount);
                            statusForm.Controls.Add(status);
                            statusForm.Show();
                            HeadlessRenderer.CapturePng(statusForm, 1600, 240);
                            int smallWidth = amount.Bounds.Width;
                            amount.Text = "3,875,000¥";
                            HeadlessRenderer.CapturePng(statusForm, 1600, 240);
                            Check(amount.Bounds.Width > smallWidth && amount.Bounds.Width > 23,
                                "Nuyen status label grows to show the full amount at scale " + scale);
                        }
                        using var spinnerForm = new Form { Width = 600, Height = 120 };
                        var spinner = new NumericUpDown { Left = 20, Top = 20, Width = 100, Height = 24, Value = 40 };
                        spinnerForm.Controls.Add(spinner);
                        spinnerForm.Show();
                        HeadlessRenderer.CapturePng(spinnerForm, 1200, 240);
                        var increment = (System.Drawing.Rectangle)typeof(NumericUpDown)
                            .GetMethod("GetIncrementArea", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(spinner, null);
                        Check(increment.Left == 82 && increment.Width == 18,
                            "spinner arrow strip keeps logical bounds at scale " + scale);
                        int x = (int)((spinner.Left + increment.Left + increment.Width / 2) * scale);
                        int y = (int)((spinner.Top + increment.Top + increment.Height / 2) * scale);
                        var windowType = typeof(Form).BaseType;
                        windowType.GetMethod("HandlePointerPressed", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(spinnerForm, new object[] { MouseButtons.Left, x, y, Keys.None });
                        windowType.GetMethod("HandlePointerReleased", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(spinnerForm, new object[] { MouseButtons.Left, x, y, Keys.None });
                        Check(spinner.Value == 41, "spinner arrow click increments at scale " + scale);
                        var menuBar = new MenuStrip();
                        spinnerForm.Controls.Add(menuBar);
                        foreach (string caption in new[] { "File", "Tools", "Window", "Help" }) menuBar.Items.Add(caption);
                        HeadlessRenderer.CapturePng(spinnerForm, 1200, 240);
                        foreach (MenuItem item in ((Menu)menuBar).Items)
                        {
                            int hoverX = (int)((menuBar.Left + item.Bounds.Left + item.Bounds.Width / 2) * scale);
                            int hoverY = (int)((menuBar.Top + item.Bounds.Top + item.Bounds.Height / 2) * scale);
                            windowType.GetMethod("HandlePointerMoved", BindingFlags.NonPublic | BindingFlags.Instance)
                                .Invoke(spinnerForm, new object[] { MouseButtons.None, hoverX, hoverY, Keys.None });
                            Check(item.Hovered, "top menu hover aligns with " + item.Text + " at scale " + scale);
                        }

                        using var menu = new ContextMenuStrip();
                        menu.Items.Add("First command");
                        menu.Show(spinner, new System.Drawing.Point(0, 24));
                        menu.Hide();
                        var added = menu.Items.Add("Character added after first opening");
                        menu.Show(spinner, new System.Drawing.Point(0, 24));
                        var dropdown = (MenuDropDown)menu;
                        var popup = (PopupWindow)typeof(MenuDropDown).GetField("popup",
                            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dropdown);
                        Check(added.Bounds.Height > 0 && added.Bounds.Bottom < popup.Size.Height,
                            "reopened menu includes late entries without clipping at scale " + scale);
                        typeof(MenuBase).GetMethod("OnMouseMove", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(dropdown, new object[] { new MouseEventArgs(MouseButtons.None, 0,
                                added.Bounds.Left + 5, added.Bounds.Top + added.Bounds.Height / 2, 0) });
                        Check(dropdown.GetItemAtLocation(new System.Drawing.Point(added.Bounds.Left + 5,
                            added.Bounds.Top + added.Bounds.Height / 2)) == added,
                            "menu hover hit area matches late entry at scale " + scale);
                        menu.Hide();
                    }
                }
                finally { Application.UiScale = originalScale; }

                try
                {
                    foreach (double scale in new[] { 1.0, 2.0 })
                    foreach (FormWindowState state in new[] { FormWindowState.Normal, FormWindowState.Maximized })
                    {
                        Application.UiScale = scale;
                        using var mdiParent = new Form { Width = 500, Height = 300, IsMdiContainer = true };
                        var mdiMenu = new MenuStrip { Dock = DockStyle.Top };
                        mdiParent.MainMenuStrip = mdiMenu;
                        mdiParent.Controls.Add(mdiMenu);
                        mdiParent.Show();
                        using var mdiChild = new Form { Width = 260, Height = 160, MdiParent = mdiParent };
                        var button = new Button { Left = 40, Top = 40, Width = 80, Height = 28, Text = "Click" };
                        int clicks = 0;
                        button.Click += (_, _) => clicks++;
                        mdiChild.Controls.Add(button);
                        mdiChild.Show();
                        mdiChild.WindowState = state;
                        HeadlessRenderer.CapturePng(mdiParent, 1000, 600);
                        var frame = (Control)typeof(Form).GetField("MdiHost", BindingFlags.NonPublic | BindingFlags.Instance)
                            .GetValue(mdiChild);
                        int border = (int)frame.GetType().GetProperty("Border", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(frame);
                        int caption = (int)frame.GetType().GetProperty("Caption", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(frame);
                        var click = new MouseEventArgs(MouseButtons.Left, 1, border + button.Left + 40,
                            border + caption + button.Top + 14, 0);
                        frame.GetType().GetMethod("OnMouseDown", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(frame, new object[] { click });
                        frame.GetType().GetMethod("OnMouseUp", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(frame, new object[] { click });
                        Check(clicks == 1, "hosted child button click at scale " + scale + " in " + state);
                        clicks = 0;
                        var center = mdiParent.PointToClient(button.PointToScreen(new System.Drawing.Point(40, 14)));
                        int x = (int)Math.Round(center.X * mdiParent.Scaling);
                        int y = (int)Math.Round(center.Y * mdiParent.Scaling);
                        var windowType = typeof(Form).BaseType;
                        windowType.GetMethod("HandlePointerPressed", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(mdiParent, new object[] { MouseButtons.Left, x, y, Keys.None });
                        windowType.GetMethod("HandlePointerReleased", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(mdiParent, new object[] { MouseButtons.Left, x, y, Keys.None });
                        Check(clicks == 1, "outer window routes hosted press and captured release at scale " + scale + " in " + state);
                        using var inventoryMenu = new ContextMenuStrip();
                        var customItem = inventoryMenu.Items.Add("Custom Lifestyle");
                        int customClicks = 0;
                        customItem.Click += (_, _) => customClicks++;
                        var splitButton = new SplitButton { Left = 30, Top = 10, Width = 160, Height = 30,
                            AutoSize = false, Text = "Add Lifestyle", SplitMenuStrip = inventoryMenu };
                        int primaryClicks = 0;
                        splitButton.Click += (_, _) => primaryClicks++;
                        mdiChild.Controls.Add(splitButton);
                        HeadlessRenderer.CapturePng(mdiParent, 1000, 600);
                        var arrow = (System.Drawing.Rectangle)typeof(SplitButton)
                            .GetField("dropDownRectangle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(splitButton);
                        var arrowCenter = mdiParent.PointToClient(splitButton.PointToScreen(new System.Drawing.Point(
                            arrow.Left + arrow.Width / 2, arrow.Top + arrow.Height / 2)));
                        int arrowX = (int)Math.Round(arrowCenter.X * mdiParent.Scaling);
                        int arrowY = (int)Math.Round(arrowCenter.Y * mdiParent.Scaling);
                        windowType.GetMethod("HandlePointerPressed", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(mdiParent, new object[] { MouseButtons.Left, arrowX, arrowY, Keys.None });
                        windowType.GetMethod("HandlePointerReleased", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(mdiParent, new object[] { MouseButtons.Left, arrowX, arrowY, Keys.None });
                        Check(primaryClicks == 0, "inventory arrow does not fire primary Add at scale " + scale + " in " + state);
                        var inventoryPopup = (PopupWindow)typeof(MenuDropDown)
                            .GetField("popup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inventoryMenu);
                        Check(inventoryPopup != null && inventoryPopup.Visible,
                            "inventory arrow opens menu at scale " + scale + " in " + state);
                        HeadlessRenderer.CapturePng(inventoryPopup, 600, 100);
                        int itemX = (int)((customItem.Bounds.Left + customItem.Bounds.Width / 2) * inventoryPopup.Scaling);
                        int itemY = (int)((customItem.Bounds.Top + customItem.Bounds.Height / 2) * inventoryPopup.Scaling);
                        windowType.GetMethod("HandlePointerPressed", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(inventoryPopup, new object[] { MouseButtons.Left, itemX, itemY, Keys.None });
                        windowType.GetMethod("HandlePointerReleased", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(inventoryPopup, new object[] { MouseButtons.Left, itemX, itemY, Keys.None });
                        Check(customClicks == 1, "inventory submenu selection fires once at scale " + scale + " in " + state);
                        inventoryMenu.Hide();
                        var primaryCenter = mdiParent.PointToClient(splitButton.PointToScreen(new System.Drawing.Point(30, 15)));
                        int primaryX = (int)Math.Round(primaryCenter.X * mdiParent.Scaling);
                        int primaryY = (int)Math.Round(primaryCenter.Y * mdiParent.Scaling);
                        windowType.GetMethod("HandlePointerPressed", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(mdiParent, new object[] { MouseButtons.Left, primaryX, primaryY, Keys.None });
                        windowType.GetMethod("HandlePointerReleased", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(mdiParent, new object[] { MouseButtons.Left, primaryX, primaryY, Keys.None });
                        Check(primaryClicks == 1, "inventory primary Add fires once at scale " + scale + " in " + state);
                        var tradition = new ComboBox { Left = 100, Top = 90, Width = 140, Height = 24,
                            DropDownStyle = ComboBoxStyle.DropDownList };
                        tradition.Items.AddRange(new object[] { "Aboriginal", "Hermetic", "Shamanic" });
                        mdiChild.Controls.Add(tradition);
                        HeadlessRenderer.CapturePng(mdiParent, 1000, 600);
                        tradition.DroppedDown = true;
                        var traditionPopup = (PopupWindow)typeof(ComboBox).GetField("popup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tradition);
                        var popupBackend = (Majorsilence.Forms.Backends.IWindowBackend)windowType
                            .GetField("Backend", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(traditionPopup);
                        Check(popupBackend.Location == tradition.PointToScreen(new System.Drawing.Point(1, tradition.Height)),
                            "hosted Tradition dropdown stays beneath its control at scale " + scale + " in " + state);
                        tradition.DroppedDown = false;
                        var detailTree = new TreeView { Left = 140, Top = 70, Width = 110, Height = 70 };
                        using var nodeMenu = new ContextMenuStrip();
                        nodeMenu.Items.Add("Edit Detail");
                        var detailNode = new TreeNode("Detail") { ContextMenuStrip = nodeMenu };
                        detailTree.Nodes.Add(detailNode);
                        mdiChild.Controls.Add(detailTree);
                        int doubleClicks = 0;
                        bool menuOpened = false;
                        detailTree.NodeMouseDoubleClick += (_, _) => doubleClicks++;
                        nodeMenu.Opening += (_, _) => menuOpened = true;
                        HeadlessRenderer.CapturePng(mdiParent, 1000, 600);
                        var rowCenter = mdiParent.PointToClient(detailTree.PointToScreen(new System.Drawing.Point(
                            (int)((detailNode.Bounds.Left + detailNode.Bounds.Width / 2) / detailTree.Scaling),
                            (int)((detailNode.Bounds.Top + detailNode.Bounds.Height / 2) / detailTree.Scaling))));
                        int rowX = (int)Math.Round(rowCenter.X * mdiParent.Scaling);
                        int rowY = (int)Math.Round(rowCenter.Y * mdiParent.Scaling);
                        for (int press = 0; press < 2; press++)
                        {
                            windowType.GetMethod("HandlePointerPressed", BindingFlags.NonPublic | BindingFlags.Instance)
                                .Invoke(mdiParent, new object[] { MouseButtons.Left, rowX, rowY, Keys.None });
                            windowType.GetMethod("HandlePointerReleased", BindingFlags.NonPublic | BindingFlags.Instance)
                                .Invoke(mdiParent, new object[] { MouseButtons.Left, rowX, rowY, Keys.None });
                        }
                        Check(doubleClicks == 1, "hosted tree receives double-click at scale " + scale + " in " + state);
                        windowType.GetMethod("HandlePointerPressed", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(mdiParent, new object[] { MouseButtons.Right, rowX, rowY, Keys.None });
                        windowType.GetMethod("HandlePointerReleased", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(mdiParent, new object[] { MouseButtons.Right, rowX, rowY, Keys.None });
                        Check(menuOpened, "tree node context menu opens at scale " + scale + " in " + state);
                        nodeMenu.Hide();

                    }
                }
                finally { Application.UiScale = originalScale; }

                using var main = new frmMain();
                Capture(main, output, "main");
                var karmaBuild = new Character();
                using (var bp = new frmSelectBP(karmaBuild))
                {
                    bp.Show();
                    Field<ComboBox>(bp, "cboBuildMethod").SelectedValue = "Karma";
                    Field<NumericUpDown>(bp, "nudBP").Value = 750;
                    Field<Button>(bp, "cmdOK").PerformClick();
                    Check(karmaBuild.BuildMethod == CharacterBuildMethod.Karma && karmaBuild.BuildKarma == 750,
                        "Karma creation method selection");
                }
                var character = new Character { Name = "Mac Smoke Test", BuildPoints = 400 };
                using (var metatype = new frmMetatype(character))
                {
                    metatype.Show();
                    Field<ListBox>(metatype, "lstMetatypes").SelectedValue = "Human";
                    Field<Button>(metatype, "cmdOK").PerformClick();
                    Check(character.Metatype == "Human", "metatype selection creates a Human");
                }
                using (var dialog = new frmSelectWeapon(character)) Capture(dialog, output, "weapon-dialog");
                var qualityXml = XmlManager.Instance.Load("qualities.xml");
                var detailQuality = new Quality(character);
                detailQuality.Create(qualityXml.SelectSingleNode("/chummer/qualities/quality[name='Codeslinger']"),
                    character, QualitySource.Selected, new TreeNode(), new System.Collections.Generic.List<Weapon>(),
                    new System.Collections.Generic.List<TreeNode>(), PromptDetails.Placeholder);
                character.Qualities.Add(detailQuality);
                Check(detailQuality.Extra == PromptDetails.Placeholder && PromptDetails.CanEdit(detailQuality),
                    "quality stores an editable placeholder");
                PromptDetails.Apply(character, detailQuality, "Hack on the Fly");
                Check(detailQuality.Extra == "Hack on the Fly" && character.Improvements.Any(item =>
                    item.SourceName == detailQuality.InternalId && item.ImprovedName == "Hack on the Fly"),
                    "editing quality detail also updates its stored improvement");
                using (var reviewForm = new Form { Width = 500, Height = 180 })
                {
                    var reviewTree = new TreeView { Dock = DockStyle.Fill, HideSelection = false };
                    var node = new TreeNode(detailQuality.DisplayName) { Tag = detailQuality.InternalId };
                    reviewTree.Nodes.Add(node);
                    reviewForm.Controls.Add(reviewTree);
                    PromptDetails.Attach(reviewForm, character, reviewTree,
                        id => character.Qualities.FirstOrDefault(item => item.InternalId == id), null, () => {});
                    PromptDetails.Apply(character, detailQuality, "Control Device");
                    node.Text = detailQuality.DisplayName;
                    reviewTree.SelectedNode = node;
                    reviewForm.Show();
                    HeadlessRenderer.CapturePng(reviewForm, 1000, 360);
                    Check(node.ForeColor == System.Drawing.Color.Red,
                        "selected review-required Codeslinger remains red in the quality list");
                    PromptDetails.Apply(character, detailQuality, "Hack on the Fly");
                    node.Text = detailQuality.DisplayName;
                    HeadlessRenderer.CapturePng(reviewForm, 1000, 360);
                    Check(node.ForeColor == System.Drawing.Color.Empty,
                        "changing to an ordinary action clears the review coloring");
                }
                var gearXml = XmlManager.Instance.Load("gear.xml");
                foreach (string gearName in new[] { "Fake SIN", "Fake License", "Certified Credstick, Standard" })
                {
                    var detailGear = new Gear(character);
                    detailGear.Create(gearXml.SelectSingleNode("/chummer/gears/gear[name='" + gearName + "']"),
                        character, new TreeNode(), 1, new System.Collections.Generic.List<Weapon>(),
                        new System.Collections.Generic.List<TreeNode>(), PromptDetails.Placeholder);
                    character.Gear.Add(detailGear);
                    Check(PromptDetails.CanEdit(detailGear), gearName + " supports later detail editing");
                    PromptDetails.Apply(character, detailGear, "Updated detail");
                    Check(detailGear.DisplayName.Contains("Updated detail"), gearName + " displays edited detail");
                    if (gearName == "Fake SIN") PromptDetails.Apply(character, detailGear, " ");
                }
                using (var licensePrompt = new frmSelectItem { Character = character, AllowAutoSelect = false })
                {
                    licensePrompt.Show();
                    Field<ComboBox>(licensePrompt, "cboAmmo").Text = "  ";
                    Field<Button>(licensePrompt, "cmdOK").PerformClick();
                    Check(licensePrompt.SelectedItem == PromptDetails.Placeholder, "blank license detail accepts placeholder");
                }
                using (var dialog = new frmSelectArmor(character)) Capture(dialog, output, "armor-dialog");
                using (var dialog = new frmSelectGear(character)) Capture(dialog, output, "gear-dialog");
                using (var dialog = new frmSelectCyberware(character)) Capture(dialog, output, "cyberware-dialog");
                using (var dialog = new frmSelectQuality(character)) Capture(dialog, output, "quality-dialog");
                using (var dialog = new frmSelectSpell(character)) Capture(dialog, output, "spell-dialog");
                character.BOD.Value = 3;
                var namingTime = new DateTime(2026, 10, 13, 14, 5, 9);
                character.Alias = "";
                Check(CharacterFileDialogs.SuggestedFileName(character, namingTime) == "chummer-20261013-140509.chum",
                    "blank alias suggests timestamped Chummer filename");
                character.Alias = "Runner.Name";
                Check(CharacterFileDialogs.SuggestedFileName(character, namingTime) == "Runner.Name.chum",
                    "alias with punctuation retains character extension");
                character.Alias = "";
                Check(character.BOD.TotalValue == 3, "attribute calculation");
                using (var creation = new frmCreate(character))
                {
                    creation.MdiParent = main;
                    creation.Show();
                    var originalPluginNotice = AppErrors.Notify;
                    try
                    {
                        int missingNotices = 0;
                        AppErrors.Notify = (_, _, _) => missingNotices++;
                        var cyberTree = Field<TreeView>(creation, "treCyberware");
                        typeof(frmCreate).GetField("_blnSkipRefresh", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(creation, true);
                        var staleNode = new TreeNode("Stale selected cyberware") { Tag = Guid.NewGuid().ToString() };
                        cyberTree.Nodes[0].Nodes.Add(staleNode);
                        cyberTree.SelectedNode = staleNode;
                        bool added = (bool)typeof(frmCreate).GetMethod("PickCyberware", BindingFlags.NonPublic | BindingFlags.Instance)
                            .Invoke(creation, new object[] { Improvement.ImprovementSource.Cyberware });
                        Check(!added && missingNotices == 1, "missing selected cyberware cancels plugin addition without a null-reference crash");
                        staleNode.Remove();
                        cyberTree.SelectedNode = cyberTree.Nodes[0];
                        typeof(frmCreate).GetField("_blnSkipRefresh", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(creation, false);
                    }
                    finally { AppErrors.Notify = originalPluginNotice; }

                    Check(!Field<TreeView>(creation, "treQualities").ShowLines, "creation quality list does not draw connector lines");
                    Field<NumericUpDown>(creation, "nudBOD").Value = 4;
                    Check(character.BOD.Value == 4, "creation spinner updates rule model");
                    string pathBeforeCancel = character.FileName;
                    bool dirtyBeforeCancel = Field<bool>(creation, "_blnIsDirty");
                    var saveAsMenu = Field<ToolStripMenuItem>(main, "_mnuSaveCharacterAs");
                    Check(saveAsMenu.Enabled, "main File menu enables Save As for an active character");
                    saveAsMenu.PerformClick();
                    Check(character.FileName == pathBeforeCancel &&
                        Field<bool>(creation, "_blnIsDirty") == dirtyBeforeCancel,
                        "cancelled native Save As preserves file path and unsaved state");
                    character.FileName = Path.Combine(output, "toolbar-save.chum");
                    Field<ToolStripButton>(creation, "tsbSave").PerformClick();
                    Check(File.Exists(character.FileName), "creation toolbar saves character");
                    byte[] savedBytes = File.ReadAllBytes(character.FileName);
                    Check(savedBytes.Length > 2 && savedBytes[0] == 0xff && savedBytes[1] == 0xfe,
                        "character save retains UTF-16 little-endian encoding");

                    File.WriteAllBytes(Path.Combine(output, "creation.png"), HeadlessRenderer.CapturePng(main, 1200, 850));
                    Field<TabControl>(creation, "tabCharacterTabs").SelectedTab = Field<TabPage>(creation, "tabStreetGear");
                    Field<TabControl>(creation, "tabStreetGearTabs").SelectedTab = Field<TabPage>(creation, "tabGear");
                    File.WriteAllBytes(Path.Combine(output, "equipment-layout.png"), HeadlessRenderer.CapturePng(main, 1200, 850));
                    var gearTree = Field<TreeView>(creation, "treGear");
                    var gearBar = Field<HorizontalScrollBar>(gearTree, "hscrollbar");
                    var gearTabs = Field<TabControl>(creation, "tabStreetGearTabs");
                    var gearPosition = (System.Drawing.Point)typeof(Control).GetMethod("GetPositionInForm", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gearTree, null);
                    Check(gearPosition.Y + gearBar.Bottom <= creation.ClientSize.Height,
                        "equipment scrollbar remains within the visible character content");
                    Check(gearTabs.Bottom <= gearTabs.Parent.ClientRectangle.Bottom && gearTree.Bottom <= gearTree.Parent.ClientRectangle.Bottom,
                        "nested equipment tabs and tree fit within the character viewport");
                    Check(main.MdiChildren.Length == 1, "creation hosted as MDI child");
                }
                character.FileName = Path.Combine(output, "creation.chum");
                character.Save();
                var loaded = new Character { FileName = character.FileName };
                Check(loaded.Load(), "new character reloads");
                Check(loaded.Qualities.Any(item => item.InternalId == detailQuality.InternalId && item.Extra == "Hack on the Fly" &&
                    PromptDetails.CanEdit(item)), "edited quality detail survives save/load and remains editable");
                Check(loaded.Gear.Any(item => item.Name == "Fake SIN" && item.Extra == PromptDetails.Placeholder) &&
                    loaded.Gear.Any(item => item.Name == "Fake License" && item.Extra == "Updated detail") &&
                    loaded.Gear.Any(item => item.Name == "Certified Credstick, Standard" && item.GearName == "Updated detail"),
                    "placeholder, license detail, and credstick name survive the existing file format");
                Check(loaded.Name == character.Name && loaded.BOD.Value == 4 && !loaded.Created,
                    "creation identity and attributes survive save");

                loaded.Created = true;
                loaded.Karma = 8;
                loaded.Nuyen = 5000;
                var karma = new ExpenseLogEntry();
                karma.Create(8, "Session reward", ExpenseType.Karma, new DateTime(2026, 10, 13));
                loaded.ExpenseEntries.Add(karma);
                var nuyen = new ExpenseLogEntry();
                nuyen.Create(5000, "Run payment", ExpenseType.Nuyen, new DateTime(2026, 10, 13));
                loaded.ExpenseEntries.Add(nuyen);
                loaded.FileName = Path.Combine(output, "career.chum");
                loaded.Save();
                var careerCharacter = new Character { FileName = loaded.FileName };
                Check(careerCharacter.Load(), "career character reloads");
                Check(careerCharacter.Created && careerCharacter.Karma == 8 && careerCharacter.Nuyen == 5000
                    && careerCharacter.ExpenseEntries.Count == 2, "career balances and ledger survive save");
                using (var career = new frmCareer(careerCharacter))
                {
                    Check(!Field<TreeView>(career, "treQualities").ShowLines, "career quality list does not draw connector lines");
                    Capture(career, output, "career");
                    Field<TabControl>(career, "tabCharacterTabs").SelectedTab = Field<TabPage>(career, "tabKarma");
                    File.WriteAllBytes(Path.Combine(output, "career-ledger.png"), HeadlessRenderer.CapturePng(career, 1200, 850));
                    var chart = Field<HistoryChart>(career, "chtKarma");
                    Check(chart.Visible && chart.Top >= 0 && chart.Bottom <= chart.Parent.Height, "history chart fits the ledger viewport");
                }
                using (var stream = new MemoryStream())
                {
                    var writer = new XmlTextWriter(stream, Encoding.UTF8);
                    writer.WriteStartDocument();
                    writer.WriteStartElement("characters");
                    careerCharacter.PrintToStream(stream, writer);
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                    stream.Position = 0;
                    var transform = new XslCompiledTransform();
                    transform.Load(Path.Combine(AppContext.BaseDirectory, "sheets", "Shadowrun 4.xsl"), XsltSettings.Default, new BundledXmlResolver(Path.Combine(AppContext.BaseDirectory, "sheets")));
                    using var html = new StringWriter();
                    using var reader = XmlReader.Create(stream);
                    transform.Transform(reader, null, html);
                    Check(html.ToString().Contains("Mac Smoke Test"), "HTML character sheet transformation");
                }

                if (args.Length > 2)
                {
                    string[] fixtures = Directory.Exists(args[2])
                        ? Directory.GetFiles(args[2], "*.chum", SearchOption.AllDirectories)
                        : new[] { args[2] };
                    int count = 0;
                    foreach (string file in fixtures)
                    {
                        string working = Path.Combine(output, "fixture-input.chum");
                        File.Copy(Path.GetFullPath(file), working, true);
                        var fixture = new Character { FileName = working };
                        if (!fixture.Load()) throw new InvalidOperationException("Fixture failed to load: " + file);
                        string name = fixture.Name;
                        int weapons = fixture.Weapons.Count, gear = fixture.Gear.Count, skills = fixture.Skills.Count;
                        fixture.FileName = Path.Combine(output, "fixture-copy.chum");
                        fixture.Save();
                        var original = XDocument.Load(fixture.FileName);
                        var copy = new Character { FileName = fixture.FileName };
                        if (!copy.Load() || copy.Name != name || copy.Weapons.Count != weapons || copy.Gear.Count != gear
                            || copy.Skills.Count != skills) throw new InvalidOperationException("Fixture contents changed: " + file);
                        copy.Save();
                        if (!XNode.DeepEquals(original, XDocument.Load(copy.FileName)))
                            throw new InvalidOperationException("Serialized round trip changed: " + file);
                        count++;
                    }
                    Console.WriteLine("PASS upstream fixture round trips: " + count);
                }
                using (var quitGuard = new Form { Width = 300, Height = 140 })
                {
                    bool allowQuit = false;
                    int closeChecks = 0;
                    quitGuard.FormClosing += (_, args) => { closeChecks++; args.Cancel = !allowQuit; };
                    quitGuard.Show();
                    AppPlatform.QuitApplication();
                    Check(Application.OpenForms.Contains(quitGuard) && !AppPlatform.CanTerminateAfterQuit,
                        "cancelled Quit preserves the open window and prevents process termination");
                    allowQuit = true;
                    AppPlatform.QuitApplication();
                    Check(closeChecks == 2, "each Quit attempt checks closing once without repeated prompts");
                    Check(Application.OpenForms.Count == 0 && AppPlatform.CanTerminateAfterQuit,
                        "approved Quit closes all forms before enabling final process termination");
                }
                Console.WriteLine("SMOKE TESTS PASSED");
                return 0;
            }
            catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        }
    }
}
