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
                Check(Properties.Resources.add != null, "embedded toolbar image decoded");
                foreach (string file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data"), "*.xml"))
                    XmlManager.Instance.Load(Path.GetFileName(file));
                Console.WriteLine("PASS ruleset XML loads");

                double originalScale = Application.UiScale;
                try
                {
                    foreach (double scale in new[] { 1.0, 2.0 })
                    {
                        Application.UiScale = scale;
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
                    Check(main.MdiChildren.Length == 1, "creation hosted as MDI child");
                }
                character.FileName = Path.Combine(output, "creation.chum");
                character.Save();
                var loaded = new Character { FileName = character.FileName };
                Check(loaded.Load(), "new character reloads");
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
                Console.WriteLine("SMOKE TESTS PASSED");
                return 0;
            }
            catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        }
    }
}
