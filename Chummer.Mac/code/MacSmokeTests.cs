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
                Check(character.BOD.TotalValue == 3, "attribute calculation");
                using (var creation = new frmCreate(character))
                {
                    creation.MdiParent = main;
                    creation.Show();
                    Field<NumericUpDown>(creation, "nudBOD").Value = 4;
                    Check(character.BOD.Value == 4, "creation spinner updates rule model");
                    character.FileName = Path.Combine(output, "toolbar-save.chum");
                    Field<ToolStripButton>(creation, "tsbSave").PerformClick();
                    Check(File.Exists(character.FileName), "creation toolbar saves character");
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
