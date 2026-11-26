#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeLegacyControlChecks
{
    internal static async Task CheckCharacterOpenAsync(frmMain main, string path, Action<bool, string> check)
    {
        using (var emptyHost = new Forms.NativeControlHost
        {
            Name = "HiddenEmptyHostRegression", Visible = false, Bounds = Rectangle.Empty,
            NativeControl = new TextBlock { Text = "Hidden native overlay" }
        })
        {
            main.Controls.Add(emptyHost);
            emptyHost.SyncNativeControl();
            var native = (Control)emptyHost.NativeControl;
            check(TopLevel.GetTopLevel(native) != null && !native.IsEffectivelyVisible,
                "A newly attached zero-size hidden host does not draw its native element at the window origin");
        }

        main.LoadCharacter(path, blnIncludeInMRU: false);
        var editor = main.ActiveMdiChild ?? throw new InvalidOperationException("Character did not open.");
        try
        {
            await Idle();
            var tabs = Field<Forms.TabControl>(editor, "tabCharacterTabs");
            var common = Field<Forms.TabPage>(editor, "tabCommon");
            var info = Field<Forms.TabPage>(editor, "tabCharacterInfo");
            var narrative = (Control)FindHost(main, "NativeCharacterNarrative").NativeControl!;
            check(tabs.SelectedTab == common && !narrative.IsEffectivelyVisible,
                "Opening a saved character shows Common without overlaying Character Info");
            CheckHiddenHosts();
            tabs.SelectedTab = info;
            await Idle();
            check(narrative.IsEffectivelyVisible, "Character Info appears when selected");
            CheckHiddenHosts();
            tabs.SelectedTab = common;
            await Idle();
            check(!narrative.IsEffectivelyVisible, "Character Info hides again on returning to Common");
            CheckHiddenHosts();
        }
        finally
        {
            editor.Close();
            await Idle();
        }

        void CheckHiddenHosts()
        {
            var leaked = main.Controls.OfType<Forms.NativeControlHost>()
                .Where(host => !host.Visible && host.NativeControl is Control { IsEffectivelyVisible: true })
                .Select(host => host.Name).ToArray();
            check(leaked.Length == 0, "Inactive native editor panels do not overlay the selected tab" +
                (leaked.Length == 0 ? "" : " [" + string.Join(", ", leaked) + "]"));
        }
    }

    internal static async Task RunAsync(frmMain main, Character fixture, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        var background = new SkiaSharp.SKColor(80, 80, 80);
        using (var glyph = new SkiaSharp.SKBitmap(19, 19))
        using (var canvas = new SkiaSharp.SKCanvas(glyph))
        {
            canvas.Clear(background);
            var paint = new Forms.PaintEventArgs(glyph.Info, canvas, 1);
            var bounds = new Rectangle(3, 3, 13, 13);
            Forms.ControlPaint.DrawCheckBox(paint, bounds, Forms.CheckState.Unchecked);
            var outline = glyph.GetPixel(8, 3);
            var emptyCenter = glyph.GetPixel(9, 9);
            canvas.Clear(background);
            Forms.ControlPaint.DrawCheckBox(paint, bounds, Forms.CheckState.Checked);
            check(Math.Abs(outline.Red - background.Red) >= 30 && glyph.GetPixel(9, 9) != emptyCenter,
                "Preferences checkbox outline remains visible on the dark panel and the checked state changes the glyph");
        }
        foreach (var (create, name) in new (Func<Forms.Form>, string)[]
        {
            (() => new frmDiceRoller(main), "lstResults"),
            (() => new frmMetatype(character), "lstMetatypes"),
            (() => new frmSelectMentorSpirit(character), "lstMentor"),
            (() => new frmSelectPower(character), "lstPowers"),
            (() => new frmSelectMetamagic(character), "lstMetamagic"),
            (() => new frmSelectProgramOption(character), "lstOptions"),
            (() => new frmSelectCyberwareSuite(Improvement.ImprovementSource.Cyberware, character), "lstCyberware"),
            (() => new frmSelectPACKSKit(character), "lstKits")
        })
        {
            using var dialog = create();
            dialog.Show();
            if (dialog is frmMetatype)
                Forms.AvaloniaHostInterop.ToAvaloniaWindow(dialog).RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            if (dialog is frmDiceRoller)
                Field<Forms.Button>(dialog, "cmdRollDice").PerformClick();
            await Idle();
            var source = Field<Forms.ListBox>(dialog, name);
            string hostName = dialog switch
            {
                frmDiceRoller => "NativeDiceRoller",
                frmSelectPACKSKit => "NativePACKSKitPicker",
                frmSelectCyberwareSuite => "NativeSuitePicker",
                frmMetatype => "NativeMetatypePicker",
                frmSelectPower => "NativePowerPicker",
                frmSelectMentorSpirit => "NativeMentorPicker",
                frmSelectMetamagic => "NativeMetamagicPicker",
                frmSelectProgramOption => "NativeProgramOptionPicker",
                _ => "Native" + name
            };
            var list = ((Control)FindHost(dialog, hostName).NativeControl!).GetVisualDescendants().OfType<ListBox>().Single();
            check(source.Parent == null && list.IsEffectivelyVisible && list.Bounds.Width > 30 &&
                list.Items.Cast<string>().SequenceEqual(source.Items.Cast<object>().Select(source.GetItemText)),
                dialog.GetType().Name + " renders the current native list in source order");
            if (dialog is frmMetatype)
            {
                var surface = (Control)FindHost(dialog, hostName).NativeControl!;
                var detail = surface.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Name == "NativelblBOD");
                check(detail.Foreground is Avalonia.Media.ISolidColorBrush { Color: var color } &&
                    color.R >= 200 && color.G >= 200 && color.B >= 200,
                    "Metatype details use a readable foreground in dark mode");
            }
            if (source.Items.Count > 1)
            {
                list.SelectedIndex = source.SelectedIndex == 1 ? 0 : 1;
                check(list.SelectedIndex >= 0 && source.SelectedIndex == list.SelectedIndex,
                    dialog.GetType().Name + " relays native selection to the existing command");
                await Idle();
                source.SelectedIndex = list.SelectedIndex == 1 ? 0 : 1;
                await Idle();
                check(list.SelectedIndex == source.SelectedIndex,
                    dialog.GetType().Name + " reflects controller selection");
            }
            dialog.Close();
            check(TopLevel.GetTopLevel(list) == null, dialog.GetType().Name + " detaches its native list on close");
        }

        var originalBooks = character.Options.Books.ToArray();
        if (!originalBooks.Contains("AR"))
            character.Options.Books.Add("AR");
        try
        {
            foreach (var kind in Enum.GetValues<MartialArtDetailPicker.Kind>())
            {
                using var picker = new MartialArtDetailPicker(character, kind, "Aikido");
                var pending = picker.ShowDialogAsync(main);
                var window = (MartialArtDetailWindow)NativeDialogHost.ActiveWindow!;
                window.Width = 260;
                await Idle();
                var choices = window.Choices.Items.Cast<MartialArtDetailChoice>().ToArray();
                check(choices.Length > 1 && choices.Select(choice => choice.Label)
                    .SequenceEqual(choices.Select(choice => choice.Label).OrderBy(label => label, StringComparer.CurrentCulture)),
                    "The native martial art detail picker preserves catalog choices and sort order");
                window.Choices.SelectedIndex = choices.Length - 1;
                await Idle();
                var layout = (Grid)window.Content!;
                double enlargedText = window.AddButton.FontSize * 1.5;
                Avalonia.Controls.Documents.TextElement.SetFontSize(layout, enlargedText);
                window.AddButton.Content = "Translated command to add this entry and choose another";
                await Idle();
                var actionRow = (WrapPanel)layout.Children[2];
                check(window.AddButton.FontSize == enlargedText && window.AddButton.Bounds.Right <= actionRow.Bounds.Width + 1 &&
                    window.AddButton.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Bounds.Height > enlargedText * 2) &&
                    window.CancelButton.Bounds.Bottom <= actionRow.Bounds.Height + 1,
                    "Martial art detail actions wrap a long command at 150% text");
                check(window.SourceButton.IsVisible == (kind != MartialArtDetailPicker.Kind.Advantage) &&
                    window.Choices.Bounds.Bottom <= layout.Children[2].Bounds.Top,
                    "Narrow martial art detail layout keeps the native action row clear and shows sources for maneuvers");
                window.AddButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                check(await pending == Forms.DialogResult.OK && picker.AddAgain && picker.SelectedItem == choices[^1].Name,
                    "The native martial art detail picker preserves the Add More result and canonical selection");
                pending = picker.ShowDialogAsync(main);
                window = (MartialArtDetailWindow)NativeDialogHost.ActiveWindow!;
                window.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                check(await pending == Forms.DialogResult.Cancel && !picker.AddAgain && picker.SelectedItem.Length == 0,
                    "Canceling the native martial art detail picker discards the draft");
            }
            using var forced = new MartialArtDetailPicker(character, MartialArtDetailPicker.Kind.Art) { ForcedArt = "Aikido" };
            check(forced.ShowDialog(main) == Forms.DialogResult.OK && forced.SelectedItem == "Aikido" &&
                NativeDialogHost.ActiveWindow == null, "A forced PACKS martial art resolves to its catalog name without a window");
        }
        finally
        {
            character.Options.Books.Clear();
            character.Options.Books.AddRange(originalBooks);
        }

        using (var categories = new frmSelectWeaponCategories(new[]
        {
            new ListItem { Name = string.Join(" ", Enumerable.Repeat("Long category label", 8)), Value = "one" },
            new ListItem { Name = "Second", Value = "two" }
        }, new[] { "one" }))
        {
            var pending = categories.ShowDialogAsync(main);
            var window = (WeaponCategoriesWindow)NativeDialogHost.ActiveWindow!;
            window.Width = 250;
            await Idle();
            var options = window.Options.Children.OfType<CheckBox>().ToArray();
            check(options[0].IsChecked == true && options[1].IsChecked == false && options.All(option => option.IsEffectivelyVisible),
                "Native weapon category list reflects initial checks");
            var layout = (Grid)window.Content!;
            check(options[0].Bounds.Height > options[1].Bounds.Height &&
                layout.Children[0].Bounds.Bottom <= layout.Children[1].Bounds.Top,
                "The narrow weapon category picker wraps labels and keeps the action row clear");
            options[0].IsChecked = false;
            options[1].IsChecked = true;
            window.AcceptButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK && categories.SelectedCategories.SequenceEqual(new[] { "two" }),
                "Accepting native weapon categories returns stable selected values");
            pending = categories.ShowDialogAsync(main);
            window = (WeaponCategoriesWindow)NativeDialogHost.ActiveWindow!;
            window.Options.Children.OfType<CheckBox>().First().IsChecked = false;
            window.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.Cancel && !categories.SelectedCategories.Any(),
                "Canceling native weapon categories discards the draft selection");
        }

        using (var side = new frmSelectSide { Description = string.Join(" ", Enumerable.Repeat("Long installation description", 8)) })
        {
            side.ForceValue("Right");
            check(side.DialogResult == Forms.DialogResult.OK && side.SelectedSide == "Right" && NativeDialogHost.ActiveWindow == null,
                "A forced installation side keeps its raw value without opening a window");
            var pending = side.ShowDialogAsync(main);
            var window = (SidePickerWindow)NativeDialogHost.ActiveWindow!;
            window.Width = 250;
            await Idle();
            var layout = (Grid)window.Content!;
            var viewport = (ScrollViewer)layout.Children[0];
            var controls = ((StackPanel)viewport.Content!).Children;
            check(window.Sides.SelectedIndex == 0 && controls[0].Bounds.Height > 40 &&
                controls[0].Bounds.Bottom <= controls[1].Bounds.Top && viewport.Bounds.Bottom <= layout.Children[1].Bounds.Top,
                "The narrow installation-side dialog wraps its description above the chooser and commands");
            window.Sides.SelectedIndex = 1;
            window.AcceptButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK &&
                side.SelectedSide == LanguageManager.Instance.GetString("String_Improvement_SideRight"),
                "Accepting an installation side returns the existing localized value");
            pending = side.ShowDialogAsync(main);
            window = (SidePickerWindow)NativeDialogHost.ActiveWindow!;
            window.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.Cancel && side.SelectedSide.Length == 0,
                "Canceling the native installation-side dialog discards the choice");
        }

        using (var picker = new frmSelectAttribute
        {
            Description = string.Join(" ", Enumerable.Repeat("Select the attribute to reduce", 8)),
            ShowMetatypeMaximum = true
        })
        {
            picker.LimitToList(new List<string> { "BOD", "REA" });
            picker.RemoveFromList(new List<string> { "BOD" });
            picker.AddMAG();
            picker.AddRES();
            var pending = picker.ShowDialogAsync(main);
            var window = (AttributePickerWindow)NativeDialogHost.ActiveWindow!;
            window.Width = 260;
            await Idle();
            var layout = (Grid)window.Content!;
            var viewport = (ScrollViewer)layout.Children[0];
            var fields = (StackPanel)viewport.Content!;
            check(window.AttributeInput.ItemCount == 3 && window.MaximumInput.IsEffectivelyVisible &&
                fields.Children[0].Bounds.Height > 40 && fields.Children[0].Bounds.Bottom <= window.AttributeInput.Bounds.Top &&
                viewport.Bounds.Bottom <= layout.Children[1].Bounds.Top,
                "Narrow native attribute picker wraps the description and keeps the optional maximum setting clear");
            window.AttributeInput.SelectedIndex = 2;
            window.MaximumInput.IsChecked = true;
            window.AcceptButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK && picker.SelectedAttribute == "RES" && picker.DoNotAffectMetatypeMaximum,
                "Native attribute picker returns the canonical code and maximum setting");
            pending = picker.ShowDialogAsync(main);
            window = (AttributePickerWindow)NativeDialogHost.ActiveWindow!;
            window.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.Cancel && picker.SelectedAttribute.Length == 0 && !picker.DoNotAffectMetatypeMaximum,
                "Canceling the attribute picker discards the draft");
            picker.SingleAttribute("EDG");
            check(picker.ShowDialog() == Forms.DialogResult.OK && picker.SelectedAttribute == "EDG" && NativeDialogHost.ActiveWindow == null,
                "A forced attribute is accepted without mounting a dialog");
        }

        using (var picker = new frmSelectSetting())
        {
            var pending = picker.ShowDialogAsync(main);
            var window = (NativeChoiceWindow)NativeDialogHost.ActiveWindow!;
            check(window.ChoiceInput.ItemTemplate!.Build(null) is TextBlock { Text: "" },
                "The native choice template draws an empty item safely");
            window.Width = 260;
            await Idle();
            var layout = (Grid)window.Content!;
            var viewport = layout.Children.OfType<ScrollViewer>().Single();
            var actions = layout.Children.OfType<WrapPanel>().Single();
            var fields = ((StackPanel)viewport.Content!).Children;
            check(window.ChoiceInput.ItemCount > 0 && window.ChoiceInput.SelectedIndex >= 0 &&
                fields[0].Bounds.Bottom <= fields[1].Bounds.Top && fields[1].Bounds.Bottom <= fields[2].Bounds.Top &&
                viewport.Bounds.Bottom <= actions.Bounds.Top,
                "The settings profile picker keeps its prompt, label, input, and actions separate at narrow widths");
            var description = (TextBlock)fields[0];
            description.Text = string.Join(" ", Enumerable.Repeat("Translated instructions for selecting the character settings profile", 20));
            window.SizeToContent = SizeToContent.Manual;
            window.Height = 250;
            Avalonia.Controls.Documents.TextElement.SetFontSize(layout, description.FontSize * 1.5);
            await Idle();
            window.ChoiceInput.BringIntoView();
            await Idle();
            check(viewport.Extent.Height > viewport.Viewport.Height && viewport.Offset.Y > 0 &&
                viewport.Bounds.Bottom <= actions.Bounds.Top && actions.Bounds.Bottom <= layout.Bounds.Height,
                "The profile picker scrolls long instructions at 150% text while retaining its confirmation buttons");
            string name = ((ListItem)window.ChoiceInput.SelectedItem!).Name;
            window.AcceptButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK &&
                System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(MacPaths.SettingsDirectory, picker.SettingsFile))
                    .Root?.Element("name")?.Value == name,
                "The native profile picker returns the XML filename for the displayed profile");
            pending = picker.ShowDialogAsync(main);
            window = (NativeChoiceWindow)NativeDialogHost.ActiveWindow!;
            window.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.Cancel, "Canceling the native settings picker discards the profile choice");
        }

        using (var picker = new frmSelectSkillGroup { ExcludeCategory = " Combat Active, Social Active " })
        {
            var pending = picker.ShowDialogAsync(main);
            var window = (NativeChoiceWindow)NativeDialogHost.ActiveWindow!;
            await Idle();
            var choices = window.ChoiceInput.Items.Cast<ListItem>().ToArray();
            check(choices.All(choice => choice.Value != "Firearms") && choices.Any(choice => choice.Value == "Athletics"),
                "The native skill group picker excludes groups without eligible member skills");
            window.ChoiceInput.SelectedItem = choices.Single(choice => choice.Value == "Athletics");
            window.AcceptButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK && picker.SelectedSkillGroup == "Athletics",
                "The native skill group picker returns the canonical group name");
            pending = picker.ShowDialogAsync(main);
            window = (NativeChoiceWindow)NativeDialogHost.ActiveWindow!;
            window.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.Cancel && picker.SelectedSkillGroup.Length == 0,
                "Canceling the native skill group picker discards the choice");
            picker.OnlyGroup = "Firearms";
            check(picker.ShowDialog() == Forms.DialogResult.OK && picker.SelectedSkillGroup == "Firearms" &&
                NativeDialogHost.ActiveWindow == null, "A forced skill group bypasses the dialog and the category filter");
        }

        using (var picker = new frmSelectSkill(character) { LimitToSkill = "Pistols, Blades" })
        {
            var pending = picker.ShowDialogAsync(main);
            var window = (NativeChoiceWindow)NativeDialogHost.ActiveWindow!;
            await Idle();
            var choices = window.ChoiceInput.Items.Cast<ListItem>().ToArray();
            check(choices.Length == 2 && choices.Any(choice => choice.Value == "Pistols") &&
                choices.Any(choice => choice.Value == "Blades"), "The native skill picker honors the caller's allowed skills");
            window.ChoiceInput.SelectedItem = choices.Single(choice => choice.Value == "Pistols");
            window.AcceptButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK && picker.SelectedSkill == "Pistols",
                "The native skill picker returns the canonical skill name");
            pending = picker.ShowDialogAsync(main);
            window = (NativeChoiceWindow)NativeDialogHost.ActiveWindow!;
            window.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.Cancel && picker.SelectedSkill.Length == 0,
                "Canceling the native skill picker discards the choice");
            picker.OnlySkill = "Pistols, " + LanguageManager.Instance.GetString("Label_SelectGear_Hacked");
            check(picker.ShowDialog() == Forms.DialogResult.OK && picker.SelectedSkill == "Pistols" &&
                NativeDialogHost.ActiveWindow == null, "A forced skill strips the hacked suffix and resolves without a window");
        }

        foreach (var catalog in Enum.GetValues<CategoryPicker.Catalog>())
        {
            using var picker = new CategoryPicker(catalog);
            var pending = picker.ShowDialogAsync(main);
            var window = (NativeChoiceWindow)NativeDialogHost.ActiveWindow!;
            await Idle();
            string[] expected = XmlManager.Instance.Load(catalog.ToString().ToLowerInvariant() + "s.xml")
                .SelectNodes("/chummer/categories/category")!.Cast<System.Xml.XmlNode>()
                .Select(node => node.InnerText).ToArray();
            if (catalog == CategoryPicker.Catalog.Weapon)
                expected = expected.Append("Cyberware").ToArray();
            var choices = window.ChoiceInput.Items.Cast<ListItem>().ToArray();
            check(choices.Select(choice => choice.Value).SequenceEqual(expected),
                $"Native {catalog} category choices preserve catalog order and canonical values");
            window.ChoiceInput.SelectedIndex = choices.Length - 1;
            window.AcceptButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK && picker.SelectedCategory == expected[^1],
                $"Accepting the native {catalog} category returns its canonical value");
            pending = picker.ShowDialogAsync(main);
            window = (NativeChoiceWindow)NativeDialogHost.ActiveWindow!;
            window.CancelButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.Cancel && picker.SelectedCategory.Length == 0,
                $"Canceling the native {catalog} category discards the draft");
        }
        using (var forced = new CategoryPicker(CategoryPicker.Catalog.Weapon) { OnlyCategory = "Cyberware" })
            check(forced.ShowDialog() == Forms.DialogResult.OK && forced.SelectedCategory == "Cyberware" &&
                NativeDialogHost.ActiveWindow == null, "A single Cyberware category resolves without opening a window");

        await Idle();
        check(!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any(),
            "Picker checks leave no unexpected error dialog open");

        foreach (var (create, names) in new (Func<Forms.Form>, string[])[]
        {
            (() => new frmCreateImprovement(character), new[] { "cboImprovemetType" }),
            (() => new frmCreateSpell(character), new[] { "cboCategory", "cboDuration", "cboRange", "cboType" }),
            (() => new frmDiceRoller(main), new[] { "cboMethod" }),
            (() => new frmMetatype(character), new[] { "cboCategory", "cboMetavariant", "cboPossessionMethod" }),
            (() => new frmSelectAdvancedLifestyle(new Lifestyle(character), character), new[] { "cboComforts", "cboEntertainment", "cboNecessities", "cboNeighborhood", "cboSecurity" }),
            (() => new frmSelectContactConnection(), new[] { "cboAreaOfInfluence", "cboMagicalResources", "cboMatrixResources", "cboMembership" }),
            (() => new frmSelectCritterPower(character), new[] { "cboCategory" }),
            (() => new frmSelectItem(), new[] { "cboAmmo" }),
            (() => new frmSelectMentorSpirit(character), new[] { "cboCategory", "cboChoice1", "cboChoice2" }),
            (() => new frmSelectPACKSKit(character), new[] { "cboCategory" }),
            (() => new frmViewer { Characters = new List<Character> { character } }, new[] { "cboXSLT" }),
        })
        {
            using (var dialog = create())
            {
                dialog.Show();
                await Idle();
                foreach (string name in names)
                {
                    var source = Field<Forms.ComboBox>(dialog, name);
                    string? layoutHost = dialog switch
                    {
                        frmSelectContactConnection => "NativeContactConnection",
                        frmCreateSpell => "NativeSpellCreation",
                        frmCreateImprovement => "NativeImprovementCreation",
                        frmDiceRoller => "NativeDiceRoller",
                        frmViewer => "NativeCharacterPreviewHeader",
                        frmSelectPACKSKit => "NativePACKSKitPicker",
                        frmSelectAdvancedLifestyle => "NativeAdvancedLifestyle",
                        frmSelectCritterPower => "NativeCritterPowerPicker",
                        frmMetatype => "NativeMetatypePicker",
                        frmSelectItem => "NativeItemPicker",
                        frmSelectMentorSpirit => "NativeMentorPicker",
                        _ => null
                    };
                    var native = layoutHost != null
                        ? ((Control)FindHost(dialog, layoutHost).NativeControl!).GetVisualDescendants()
                            .OfType<ComboBox>().Single(choice => choice.Name == "Native" + name)
                        : (ComboBox)((Border)FindHost(dialog, "Native" + name).NativeControl!).Child!;
                    check(source.Parent == null && native.IsVisible == source.Visible && native.IsEnabled == source.Enabled &&
                        native.SelectedIndex == source.SelectedIndex &&
                        native.Items.Cast<string>().SequenceEqual(source.Items.Cast<object>().Select(source.GetItemText)),
                        dialog.GetType().Name + "." + name + " preserves dialog choices and selection");
                }
                if (dialog is frmViewer)
                {
                    dialog.ClientSize = new Size(360, 320);
                    var surface = (Border)FindHost(dialog, "NativeCharacterPreviewHeader").NativeControl!;
                    var preview = (Grid)surface.Child!;
                    var viewport = (ScrollViewer)preview.Children[0];
                    var document = preview.Children[1];
                    var print = preview.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdPrint");
                    var printCaption = (TextBlock)print.Content!;
                    var printSource = Field<Forms.Button>(dialog, "cmdPrint");
                    printSource.Text = string.Join(" ", Enumerable.Repeat("Print the current character sheet", 12));
                    printCaption.FontSize *= 1.5;
                    for (int attempt = 0; attempt < 50 && (surface.Bounds.Width > 361 || surface.Bounds.Height > 321); attempt++)
                        await Task.Delay(20);
                    await Idle();
                    var browser = Field<Forms.WebBrowser>(dialog, "webBrowser1");
                    check(printCaption.Text == printSource.Text && printCaption.Bounds.Height > printCaption.FontSize * 2 &&
                        print.Bounds.Right <= viewport.Viewport.Width + 1 &&
                        viewport.Extent.Height > viewport.Viewport.Height &&
                        viewport.Bounds.Bottom <= document.Bounds.Top && browser.Height > 60 &&
                        browser.Top >= viewport.Bounds.Bottom + preview.Margin.Top - 1,
                        $"Short character preview scrolls its enlarged commands above the live document [header={viewport.Bounds}/{viewport.Extent}, browser={browser.Bounds}]");
                }
                dialog.Close();
            }
        }

        await CheckContactOptionsAsync(main, check);

        foreach (var (create, name) in new (Func<Forms.Form>, string)[]
        {
            (() => new frmPrintMultiple(), "prgProgress")
        })
        {
            using var dialog = create();
            dialog.Show();
            await Idle();
            var source = Field<Forms.ProgressBar>(dialog, name);
            var layout = (Grid)((Border)FindHost(dialog, "NativeBatchPrint").NativeControl!).Child!;
            var progress = layout.GetVisualDescendants().OfType<ProgressBar>().Single();
            source.Minimum = 2;
            source.Maximum = 8;
            source.Value = 5;
            await Idle();
            check(source.Parent == null && progress.IsEffectivelyVisible &&
                progress.Minimum == 2 && progress.Maximum == 8 && progress.Value == 5,
                dialog.GetType().Name + " updates native progress from the existing job");
            source.Value = 2;
            check(progress.Value == 2, dialog.GetType().Name + " resets native progress");
            dialog.ClientSize = new Size(280, 300);
            await Idle();
            var tree = layout.Children[0];
            var footer = (StackPanel)layout.Children[1];
            check(tree.Bounds.Bottom <= footer.Bounds.Top && footer.Bounds.Bottom <= layout.Bounds.Height &&
                footer.GetVisualDescendants().OfType<Button>().Count(button => button.IsEffectivelyVisible) == 3,
                "Narrow batch printing keeps the character tree and all three commands visible");
            var select = Field<Forms.Button>(dialog, "cmdSelectCharacter");
            select.Text = string.Join(" ", Enumerable.Repeat("Select character", 5));
            var add = footer.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Name == "NativecmdSelectCharacter");
            var caption = (TextBlock)add.Content!;
            caption.FontSize *= 1.5;
            await Idle();
            check(caption.Text == select.Text && caption.Bounds.Height > caption.FontSize * 2 &&
                add.Bounds.Left >= -1 && add.Bounds.Right <= footer.Bounds.Width + 1 &&
                tree.Bounds.Height > 0 && tree.Bounds.Bottom <= footer.Bounds.Top && footer.Bounds.Bottom <= layout.Bounds.Height,
                "Narrow batch printing wraps a long enlarged command without covering the character tree");
            var characters = Field<Forms.TreeView>(dialog, "treCharacters");
            var sample = new Forms.TreeNode("Synthetic print selection");
            characters.Nodes.Add(sample);
            characters.SelectedNode = sample;
            var remove = footer.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Name == "NativecmdDelete");
            remove.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(characters.Nodes.Count == 0, "The native batch print Remove button invokes the existing selection command");
            dialog.Close();
        }

        using (var detail = new frmSelectText
        {
            AutoAcceptPrefilled = false,
            Description = string.Join(" ", Enumerable.Repeat("Choose or enter a character detail.", 16))
        })
        {
            detail.ConfigureChoices(new[]
            {
                new DetailChoice { Name = "Standard detail" },
                new DetailChoice { Name = "Reviewed detail", Review = true }
            });
            detail.Show();
            await Idle();
            detail.ClientSize = new Size(280, 180);
            await Idle();
            var prompt = (Border)FindHost(detail, "NativeTextPrompt").NativeControl!;
            var promptLayout = (Grid)prompt.Child!;
            var fields = (ScrollViewer)promptLayout.Children[0];
            for (int attempt = 0; attempt < 50 && (prompt.Bounds.Width > 281 || prompt.Bounds.Height > 181); attempt++)
                await Task.Delay(20);
            await Idle();
            check(fields.Extent.Height > fields.Viewport.Height && fields.Bounds.Bottom <= promptLayout.Children[1].Bounds.Top,
                $"A long detail prompt scrolls above the confirmation buttons at narrow width [host={prompt.Bounds}, content={fields.Extent}, viewport={fields.Viewport}]");
            var acceptDetail = prompt.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOK");
            var acceptDetailCaption = (TextBlock)acceptDetail.Content!;
            var acceptDetailController = Field<Forms.Button>(detail, "cmdOK");
            acceptDetailController.Text = string.Join(" ", Enumerable.Repeat("Confirm this character detail", 6));
            acceptDetailCaption.FontSize *= 1.5;
            await Idle();
            check(acceptDetailCaption.TextLayout.TextLines.Count is > 1 and <= 3 &&
                Equals(ToolTip.GetTip(acceptDetail), acceptDetailController.Text) &&
                acceptDetail.Bounds.Right <= promptLayout.Children[1].Bounds.Width + 1 &&
                fields.Bounds.Height > 0 && fields.Bounds.Bottom <= promptLayout.Children[1].Bounds.Top,
                "A narrow detail prompt keeps its form above a long enlarged confirmation caption");
            var input = prompt.GetVisualDescendants().OfType<ComboBox>().Single();
            check(input.IsEditable && input.Items.OfType<DetailChoice>().Any(choice => choice.Review),
                "Native detail choices preserve review metadata");
            input.SelectedIndex = 1;
            await Idle();
            prompt.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOK")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(detail.SelectedValue == "Reviewed detail",
                "Native detail selection returns the canonical value without the review label");
            detail.Close();
        }

        using (var options = new frmOptions())
        {
            options.Show();
            await Idle();
            var sourcebookHost = FindHost(options, "NativetreSourcebook");
            var disclosureHost = FindHost(options, "NativeSourcebookDisclosure");
            var disclosure = (CheckBox)disclosureHost.NativeControl!;
            var generalTabs = Field<Forms.TabControl>(options, "tabControl2");
            Reveal(generalTabs);
            var originalSize = options.ClientSize;
            options.ClientSize = new Size(900, originalSize.Height);
            await Idle();
            check(sourcebookHost.Visible && sourcebookHost.Right <= generalTabs.Left && !disclosureHost.Visible,
                "Wide Preferences places the shared sourcebooks beside the General tabs");
            options.ClientSize = new Size(560, originalSize.Height);
            await Idle();
            var profileHost = FindHost(options, "NativePreferenceProfile");
            var footerHost = FindHost(options, "NativePreferenceFooter");
            var outerTabs = Field<Forms.TabControl>(options, "tabControl1");
            check(profileHost.Bottom <= outerTabs.Top && outerTabs.Bottom <= footerHost.Top &&
                footerHost.Bottom <= options.ClientSize.Height,
                "Narrow Preferences keeps the profile, tabs, and confirmation button in separate regions");
            options.ClientSize = new Size(450, originalSize.Height);
            await Idle();
            var profiles = (Grid)profileHost.NativeControl!;
            check(Grid.GetRow(profiles.Children[1]) == 1 &&
                profiles.Children[0].Bounds.Bottom <= profiles.Children[1].Bounds.Top &&
                profileHost.Bottom <= outerTabs.Top,
                "Very narrow Preferences stacks the profile fields above the tabs");
            var profileCaption = Field<Forms.Label>(options, "lblSettingName");
            string originalProfileCaption = profileCaption.Text;
            int compactProfileHeight = profileHost.Height;
            profileCaption.Text = string.Join(" ", Enumerable.Repeat("Translated character settings profile name", 10));
            await Idle();
            var nameCaption = ((StackPanel)profiles.Children[1]).Children.OfType<TextBlock>().Single();
            check(profileHost.Height > compactProfileHeight && profileHost.Bottom <= outerTabs.Top &&
                Equals(ToolTip.GetTip(nameCaption), profileCaption.Text),
                $"A long profile caption grows the compact header while keeping Preferences tabs clear [height={compactProfileHeight}/{profileHost.Height}, tabs={outerTabs.Top}, caption={nameCaption.Bounds}, tooltip={Equals(ToolTip.GetTip(nameCaption), profileCaption.Text)}]");
            profileCaption.Text = originalProfileCaption;
            options.ClientSize = new Size(560, originalSize.Height);
            await Idle();
            check(Grid.GetRow(profiles.Children[1]) == 0 && Grid.GetColumn(profiles.Children[1]) == 1,
                "Widening Preferences restores the side-by-side profile fields");
            var save = Field<Forms.Button>(options, "cmdOK");
            string originalCaption = save.Text;
            int originalFooterHeight = footerHost.Height;
            save.Text = string.Join(" ", Enumerable.Repeat("Save these preferences and close this window", 9));
            await Idle();
            var footerButton = (Button)((Border)footerHost.NativeControl!).Child!;
            check(footerHost.Height > originalFooterHeight && outerTabs.Bottom <= footerHost.Top &&
                footerButton.Bounds.Right <= footerHost.Width + 1,
                $"A long Preferences confirmation caption wraps without covering the tabs [height={originalFooterHeight}/{footerHost.Height}, tabs={outerTabs.Bottom}, footer={footerHost.Bounds}, button={footerButton.Bounds}]");
            save.Text = originalCaption;
            await Idle();
            int defaultProfileHeight = profileHost.Height;
            int defaultFooterHeight = footerHost.Height;
            double systemFontSize = footerButton.FontSize;
            var preferenceFooter = (Border)footerHost.NativeControl!;
            try
            {
                Avalonia.Controls.Documents.TextElement.SetFontSize(profiles, systemFontSize * 1.5);
                Avalonia.Controls.Documents.TextElement.SetFontSize(preferenceFooter, systemFontSize * 1.5);
                await Idle();
                check(footerButton.FontSize >= systemFontSize * 1.5 &&
                    profileHost.Height > defaultProfileHeight && footerHost.Height > defaultFooterHeight &&
                    profileHost.Bottom <= outerTabs.Top && outerTabs.Bottom <= footerHost.Top &&
                    footerHost.Bottom <= options.ClientSize.Height,
                    $"Preferences remeasures the shell after inherited text grows [profile={defaultProfileHeight}/{profileHost.Height}, footer={defaultFooterHeight}/{footerHost.Height}]");
            }
            finally
            {
                profiles.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                preferenceFooter.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                await Idle();
            }
            check(disclosure.IsEffectivelyVisible && !sourcebookHost.Visible && disclosureHost.Bottom <= generalTabs.Top,
                $"Narrow Preferences collapses the shared sourcebook pane [client={options.ClientSize}, tabs={generalTabs.Bounds}, pane={sourcebookHost.Visible}/{sourcebookHost.Bounds}, disclosure={disclosureHost.Visible}/{disclosure.IsEffectivelyVisible}/{disclosureHost.Bounds}]");
            disclosure.IsChecked = true;
            await Idle();
            check(sourcebookHost.Visible && sourcebookHost.Height > 0 && sourcebookHost.Bottom <= generalTabs.Top,
                "The shared sourcebook pane expands above the General tabs without covering them");
            var originalGeneralPage = generalTabs.SelectedTab;
            var sharedSourcebooks = (TreeView)sourcebookHost.NativeControl!;
            foreach (Forms.TabPage generalPage in generalTabs.TabPages)
            {
                generalTabs.SelectedTab = generalPage;
                await Idle();
                check(disclosure.IsChecked == true && sourcebookHost.Visible && sharedSourcebooks.IsEffectivelyVisible &&
                    sourcebookHost.Bottom <= generalTabs.Top && generalTabs.Height >= 96,
                    "Narrow Preferences keeps the expanded sourcebook tree available on " + generalPage.Text);
            }
            generalTabs.SelectedTab = originalGeneralPage;
            await Idle();
            var sourceCaption = Field<Forms.Label>(options, "label1");
            string originalSourceCaption = sourceCaption.Text;
            int disclosureHeight = disclosureHost.Height;
            sourceCaption.Text = string.Join(" ", Enumerable.Repeat("Select sourcebooks for this profile", 12));
            await Idle();
            check(disclosureHost.Height > disclosureHeight && disclosureHost.Bottom <= sourcebookHost.Top &&
                sourcebookHost.Bottom <= generalTabs.Top && Equals(ToolTip.GetTip(disclosure), sourceCaption.Text),
                "A long sourcebook caption wraps above the expanded tree and leaves the General tabs clear");
            sourceCaption.Text = originalSourceCaption;
            options.ClientSize = new Size(440, 460);
            await Idle();
            check(disclosure.IsChecked == true && sourcebookHost.Visible && sharedSourcebooks.IsEffectivelyVisible &&
                sourcebookHost.Height >= 60 && sourcebookHost.Bottom <= generalTabs.Top && generalTabs.Height >= 96 &&
                outerTabs.Bottom <= footerHost.Top,
                $"Minimum-size Preferences shares space between expanded sourcebooks and General settings [window={options.ClientSize}, books={sourcebookHost.Bounds}, General={generalTabs.Bounds}]");
            var booksController = Field<Forms.TreeView>(options, "treSourcebook");
            var optionalBook = booksController.Nodes.Cast<Forms.TreeNode>()
                .First(node => node.Tag is string code && code != "SR4");
            var optionalRow = sharedSourcebooks.Items.OfType<TreeViewItem>()
                .Single(item => ReferenceEquals(item.Tag, optionalBook));
            var optionalCheck = (CheckBox)optionalRow.Header!;
            optionalRow.BringIntoView();
            await Idle();
            optionalCheck.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(ReferenceEquals(booksController.SelectedNode, optionalBook),
                "Clicking a sourcebook checkbox selects its book for Sources details");
            bool priorCheck = optionalBook.Checked;
            sharedSourcebooks.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Space });
            check(optionalBook.Checked != priorCheck && optionalCheck.IsChecked == optionalBook.Checked,
                "Space toggles the selected sourcebook and updates its checkbox");
            optionalBook.Checked = priorCheck;
            var bookController = booksController.Nodes[0];
            string originalBookName = bookController.Text;
            double disclosureFont = disclosure.FontSize;
            try
            {
                bookController.Text = "A translated sourcebook name with enough words to wrap inside the narrow tree";
                sourceCaption.Text = string.Join(" ", Enumerable.Repeat("Translated sourcebook selection caption", 16));
                Avalonia.Controls.Documents.TextElement.SetFontSize(disclosure, disclosureFont * 2.25);
                Avalonia.Controls.Documents.TextElement.SetFontSize(sharedSourcebooks, disclosureFont * 2.25);
                await Idle();
                check(disclosure.FontSize == disclosureFont * 2.25 &&
                    Equals(ToolTip.GetTip(disclosure), sourceCaption.Text) && sourcebookHost.Height >= 60 &&
                    sourcebookHost.Bottom <= generalTabs.Top && generalTabs.Height >= 96 &&
                    sharedSourcebooks.IsEffectivelyVisible,
                    $"Minimum-size Preferences keeps the sourcebook tree and General tabs usable at 225% text [caption={disclosureHost.Bounds}, books={sourcebookHost.Bounds}, General={generalTabs.Bounds}]");
                var bookItem = sharedSourcebooks.Items.OfType<TreeViewItem>().Single(item => ReferenceEquals(item.Tag, bookController));
                bookItem.BringIntoView();
                await Idle();
                var bookCheck = (CheckBox)bookItem.Header!;
                var bookText = bookCheck.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == bookController.Text);
                var bookPosition = Avalonia.VisualExtensions.TranslatePoint(bookText, default, sharedSourcebooks);
                check(bookCheck.IsChecked == bookController.Checked &&
                    bookText.FontSize == disclosureFont * 2.25 && bookText.Bounds.Height > bookText.FontSize * 2 &&
                    bookPosition.HasValue && bookPosition.Value.X + bookText.Bounds.Width <= sharedSourcebooks.Bounds.Width + 1,
                    "Narrow Preferences wraps long sourcebook names while retaining their check state at 225% text");
            }
            finally
            {
                bookController.Text = originalBookName;
                sourceCaption.Text = originalSourceCaption;
                disclosure.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                sharedSourcebooks.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                await Idle();
            }

            options.ClientSize = new Size(900, originalSize.Height);
            await Idle();
            check(!disclosureHost.Visible && sourcebookHost.Visible && sourcebookHost.Right <= generalTabs.Left,
                "An expanded sourcebook pane returns to the wide sidebar");
            options.ClientSize = new Size(440, 460);
            await Idle();
            check(disclosure.IsChecked == true && sourcebookHost.Visible && generalTabs.SelectedTab == originalGeneralPage,
                "Returning to a narrow Preferences window restores the expanded pane and selected page");
            disclosure.IsChecked = false;
            options.ClientSize = originalSize;
            await Idle();
            var headers = new List<TabControl>();
            foreach (string name in new[] { "tabControl1", "tabControl2" })
            {
                var source = Field<Forms.TabControl>(options, name);
                Reveal(source);
                await Idle();
                var host = FindHost(options, "Native" + name);
                var tabs = (TabControl)((ScrollViewer)host.NativeControl!).Content!;
                headers.Add(tabs);
                var pages = source.TabPages.Cast<Forms.TabPage>().ToArray();
                var items = tabs.Items.Cast<TabItem>().ToArray();
                check(tabs.IsEffectivelyVisible && tabs.Bounds.Height >= 30 &&
                    items.Select(item => item.Tag).SequenceEqual(pages) &&
                    items.Select(item => item.Header).SequenceEqual(pages.Select(page => page.Text)),
                    name + " displays native Preferences tabs in page order");
                var original = source.SelectedTab;
                var alternate = items.First(item => !ReferenceEquals(item.Tag, original));
                tabs.SelectedItem = alternate;
                await Idle();
                check(ReferenceEquals(source.SelectedTab, alternate.Tag) && source.SelectedTab?.Visible == true,
                    name + " reveals the page selected in the native header");
                source.SelectedTab = original;
                await Idle();
                check(ReferenceEquals((tabs.SelectedItem as TabItem)?.Tag, original),
                    name + " reflects controller selection");
            }
            foreach (string name in new[]
            {
                "cboPDFArgumentStyle", "cboXSLT", "cboLanguage", "cboEssenceDecimals",
                "cboBuildMethod", "cboLimbCount", "cboSetting"
            })
            {
                var source = Field<Forms.ComboBox>(options, name);
                string? pageHost = name switch
                {
                    "cboEssenceDecimals" or "cboBuildMethod" or "cboLimbCount" => "NativeCharacterAndPrinting",
                    "cboPDFArgumentStyle" or "cboXSLT" or "cboLanguage" => "NativeGlobalPreferences",
                    "cboSetting" => "NativePreferenceProfile",
                    _ => null
                };
                var host = FindHost(options, pageHost ?? "Native" + name);
                Reveal(host.Parent);
                await Idle();
                var dropdown = pageHost != null
                    ? ((Control)host.NativeControl!).GetVisualDescendants().OfType<ComboBox>()
                        .Single(control => control.Name == "Native" + name)
                    : (ComboBox)((Border)host.NativeControl!).Child!;
                if (host.Parent is Forms.ScrollableControl { AutoScroll: true } scroll)
                {
                    scroll.AutoScrollPosition = new Point(0, Math.Max(0, -scroll.AutoScrollPosition.Y + host.Bottom - scroll.ClientSize.Height + 20));
                    await Idle();
                }
                if (name == "cboPDFArgumentStyle")
                {
                    check(!source.Visible && !dropdown.IsEffectivelyVisible, "Preferences keeps the obsolete PDF argument choice hidden");
                    continue;
                }
                check(source.Parent == null && dropdown.IsEffectivelyVisible && dropdown.Bounds.Height >= 17 &&
                    dropdown.Items.Cast<string>().SequenceEqual(source.Items.Cast<object>().Select(source.GetItemText)),
                    name + $" displays the native Preferences choices in source order [visible={dropdown.IsEffectivelyVisible}, bounds={dropdown.Bounds}, host={host.Bounds}, parent={host.Parent?.Name}/{host.Parent?.Visible}/{host.Parent?.ClientSize}, hostVisible={host.Visible}, sourceVisible={source.Visible}, native={dropdown.Items.Count}, source={source.Items.Count}]");
                if (source.Items.Count <= 1)
                    continue;

                int original = source.SelectedIndex;
                dropdown.IsDropDownOpen = true;
                await Idle();
                check(dropdown.IsDropDownOpen, name + " opens its native choice popup");
                dropdown.SelectedIndex = original == 0 ? 1 : 0;
                dropdown.IsDropDownOpen = false;
                check(source.SelectedIndex == dropdown.SelectedIndex,
                    name + " sends the native choice to the original Preferences controller");
                source.SelectedIndex = original;
                await Idle();
                check(dropdown.SelectedIndex == original, name + " reflects a restored controller selection");
            }
            var sourceSet = Field<Forms.ComboBox>(options, "cboSourceSet");
            var sourceSetHost = FindHost(options, "NativeSources");
            Reveal(sourceSetHost.Parent);
            await Idle();
            var sourcesView = (ScrollViewer)sourceSetHost.NativeControl!;
            var sourceSetInput = sourcesView.GetVisualDescendants().OfType<ComboBox>().Single();
            sourceSetInput.Text = "Synthetic saved set";
            await Idle();
            check(sourceSetInput.IsEditable && sourceSetInput.IsEffectivelyVisible &&
                sourceSet.Text == "Synthetic saved set", "Native Preferences source set accepts literal names");
            var baseFolder = Field<Forms.TextBox>(options, "txtPdfBaseFolder");
            var baseFolderInput = sourcesView.GetVisualDescendants().OfType<TextBox>()
                .Single(input => input.Name == "NativetxtPdfBaseFolder");
            baseFolderInput.Text = "Synthetic PDF folder";
            check(baseFolder.Text == baseFolderInput.Text, "Native Sources folder edits update the existing controller");
            var report = Field<Forms.TextBox>(options, "txtPdfMatchReport");
            var reportInput = sourcesView.GetVisualDescendants().OfType<TextBox>()
                .Single(input => input.Name == "NativetxtPdfMatchReport");
            report.Text = "Synthetic match report";
            check(reportInput.IsReadOnly && reportInput.AcceptsReturn && reportInput.Text == report.Text,
                "Native Sources displays the existing read-only match report");
            var setsGroup = sourcesView.GetVisualDescendants().OfType<WrapPanel>()
                .Single(group => group.Children.OfType<Button>().Any(button => button.Name == "NativecmdSourceSetSave"));
            var setButtons = setsGroup.Children.OfType<Button>().ToArray();
            var saveCaption = (TextBlock)setButtons[0].Content!;
            string originalSaveCaption = saveCaption.Text!;
            var sourceWindowSize = options.ClientSize;
            try
            {
                options.ClientSize = new Size(440, 460);
                Avalonia.Controls.Documents.TextElement.SetFontSize(sourcesView, setButtons[0].FontSize * 1.5);
                saveCaption.Text = "Save the currently selected sourcebook set to a portable file";
                for (int attempt = 0; attempt < 20 && (sourcesView.Viewport.Height <= 0 ||
                    setButtons[0].Bounds.Bottom > setButtons[1].Bounds.Top); attempt++)
                    await Task.Delay(20);
                setButtons[1].BringIntoView();
                await Idle();
                check(setButtons.Select(button => button.Name).SequenceEqual(new[] { "NativecmdSourceSetSave", "NativecmdSourceSetLoad" }) &&
                    setButtons[0].Bounds.Bottom <= setButtons[1].Bounds.Top &&
                    setButtons.All(button => button.Bounds.Right <= setsGroup.Bounds.Width + 1) &&
                    sourcesView.Offset.Y > 0 && footerHost.Visible,
                    "Narrow Preferences keeps grouped source-set actions and the Save footer reachable at 150% text");
            }
            finally
            {
                saveCaption.Text = originalSaveCaption;
                sourcesView.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                options.ClientSize = sourceWindowSize;
                await Idle();
            }
            var selectedBook = Field<Forms.TreeView>(options, "treSourcebook").Nodes.Cast<Forms.TreeNode>()
                .First(node => node.Tag is string code && code != "SR4");
            selectedBook.Checked = !selectedBook.Checked;
            bool expectedBookState = selectedBook.Checked;
            string expectedBookCode = (string)selectedBook.Tag!;
            options.Close();
            check(headers.All(tabs => tabs.Items.Count == 0), "Preferences releases native tab pages on close");
            using var reopened = new frmOptions();
            reopened.Show();
            await Idle();
            check(Field<Forms.Button>(reopened, "cmdOK").Text == "Save", "Preferences offers a Save action");
            check(Field<Forms.TreeView>(reopened, "treSourcebook").Nodes.Cast<Forms.TreeNode>()
                .Single(node => Equals(node.Tag, expectedBookCode)).Checked == expectedBookState &&
                Field<Forms.TextBox>(reopened, "txtPdfBaseFolder").Text == "Synthetic PDF folder",
                "Closing and reopening Preferences keeps the selected books and PDF folder");
            reopened.Close();
        }

        character.Contacts.Clear();
        character.Contacts.Add(new Contact(character) { Name = "Synthetic contact", Connection = 1, Loyalty = 1 });
        character.Powers.Clear();
        character.Spirits.Clear();
        foreach (bool career in new[] { false, true })
        {
            character.Created = career;
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            editor.MdiParent = main;
            editor.Show();
            character.MAGEnabled = true;
            var tabs = Field<Forms.TabControl>(editor, "tabCharacterTabs");
            var view = tabs.SelectedTab!;
            try
            {
                var initiationHost = FindHost(main, career ? "NativeCareerMetamagic" : "NativeCreationMetamagic");
                var initiationLayout = (Grid)((Grid)initiationHost.NativeControl!).Children[0];
                var initiationRows = (StackPanel)initiationLayout.Children.OfType<ScrollViewer>().Single().Content!;
                var initiation = initiationRows.Children.OfType<ListBox>().Single();
                var source = Field<Forms.ListBox>(editor, "lstInitiation");
                source.Items.Add("Synthetic grade");
                var initiationTab = Field<Forms.TabPage>(editor, "tabInitiation");
                if (!tabs.TabPages.Contains(initiationTab))
                    tabs.TabPages.Add(initiationTab);
                tabs.SelectedTab = initiationTab;
                await Idle();
                check(initiation.IsEffectivelyVisible && initiation.Items.Cast<string>().Last() == "Synthetic grade",
                    editor.GetType().Name + " presents initiation grades after a controller update");
                var choices = new List<(string Name, string Tab)>
                {
                    ("cboSkillFilter", "tabSkills"), ("cboTradition", "tabMagician"),
                    ("cboStream", "tabTechnomancer"), ("cboActiveCommlink", "tabOtherInfo")
                };
                if (career)
                    choices.AddRange(new[]
                    {
                        ("cboWeaponAmmo", "tabWeapons"), ("cboVehicleWeaponAmmo", "tabVehicles"),
                        ("cboComplexFormSkill", "tabTechnomancer"), ("cboComplexFormAttribute", "tabTechnomancer")
                    });
                else
                    choices.Add(("cboCyberwareGrade", "tabCyberware"));
                foreach (var (name, tabName) in choices)
                {
                    var target = Field<Forms.TabPage>(editor, tabName);
                    if (!tabs.TabPages.Contains(target) && target.Parent == null)
                        tabs.TabPages.Add(target);
                    Reveal(target);
                    await Idle();
                    var choice = Field<Forms.ComboBox>(editor, name);
                    string hostName = name switch
                    {
                        "cboSkillFilter" when career => "NativeCareerSkillHeader",
                        "cboActiveCommlink" => "NativeCharacterSummary",
                        "cboTradition" => career ? "NativeCareerSpells" : "NativeCreationSpells",
                        "cboStream" or "cboComplexFormSkill" or "cboComplexFormAttribute" =>
                            career ? "NativeCareerComplexForms" : "NativeCreationComplexForms",
                        "cboVehicleWeaponAmmo" => "NativeCareerVehicles",
                        "cboWeaponAmmo" => "NativeCareerWeapons",
                        "cboCyberwareGrade" => "NativeCreationCyberware",
                        _ => "Native" + name
                    };
                    var choiceHost = FindHost(main, hostName);
                    var native = ((Control)choiceHost.NativeControl!).GetVisualDescendants().OfType<ComboBox>()
                        .Single(input => input.Name == "Native" + name);
                    check(choice.Parent == null && native.IsVisible == choice.Visible &&
                        native.IsEnabled == choice.Enabled && native.SelectedIndex == choice.SelectedIndex &&
                        native.Items.Cast<string>().SequenceEqual(choice.Items.Cast<object>().Select(choice.GetItemText)),
                        editor.GetType().Name + "." + name + " preserves the dropdown choices and selection");
                    if (name == "cboActiveCommlink")
                    {
                        var summary = (ScrollViewer)choiceHost.NativeControl!;
                        var balance = summary.GetVisualDescendants().OfType<TextBlock>().Single(label => label.Name == "NativelblRemainingNuyen");
                        var sourceBalance = Field<Forms.Label>(editor, "lblRemainingNuyen");
                        string balanceText = sourceBalance.Text;
                        sourceBalance.Text = "123,456,789¥";
                        await Idle();
                        check(balance.Text == sourceBalance.Text && balance.Parent is Grid row &&
                            (Grid.GetRow(balance) == 0 ? row.Children[0].Bounds.Right <= balance.Bounds.Left
                                : row.Children[0].Bounds.Bottom <= balance.Bounds.Top) &&
                            summary.Viewport.Width > 0,
                            editor.GetType().Name + " keeps the live Nuyen balance clear of its wrapping caption");
                        sourceBalance.Text = balanceText;
                    }
                    if (name == "cboSkillFilter")
                    {
                        check(native.IsEffectivelyVisible && native.Bounds.Height >= 17 && native.ItemCount > 1,
                            editor.GetType().Name + " renders the native skill filter on the Skills tab");
                        int original = choice.SelectedIndex;
                        native.SelectedIndex = original == 0 ? 1 : 0;
                        check(native.SelectedIndex == choice.SelectedIndex,
                            editor.GetType().Name + " routes native skill filtering to the existing handler");
                        choice.SelectedIndex = original;
                        await Idle();
                        native.IsDropDownOpen = true;
                        await Idle();
                        tabs.SelectedTab = view;
                        await Idle();
                        check(!native.IsDropDownOpen && !native.IsEffectivelyVisible,
                            editor.GetType().Name + " closes the skill dropdown when leaving the tab");
                    }
                }
                Reveal(Field<Forms.TabPage>(editor, "tabCharacterInfo"));
                await Idle();
                check(AppearanceColors.NativeTemplate.Build(null) is TextBlock { Text: "" },
                    "editable appearance palette renders empty content during popup transitions");
                foreach (string name in new[] { "txtHair", "txtEyes", "txtSkin" })
                {
                    var original = Field<Forms.ComboBox>(editor, name);
                    var identityHost = (ScrollViewer)FindHost(main, "NativeCharacterNarrative").NativeControl!;
                    var palette = identityHost.GetVisualDescendants().OfType<ComboBox>().Single(input => input.Name == "Native" + name);
                    check(palette.IsEditable && palette.IsEffectivelyVisible,
                        editor.GetType().Name + "." + name + " renders an editable palette");
                    palette.Text = "Synthetic iridescent";
                    await Idle();
                    check(original.Text == "Synthetic iridescent" && palette.Text == original.Text,
                        editor.GetType().Name + "." + name + " preserves literal appearance text");
                    palette.IsDropDownOpen = true;
                    await Idle();
                    check(palette.IsDropDownOpen && palette.Items.OfType<AppearanceColor>().Any(color => color.IsFamily),
                        editor.GetType().Name + "." + name + " opens the grouped native palette");
                    int option = palette.Items.OfType<AppearanceColor>().ToList().FindIndex(color => !color.IsFamily);
                    string expected = ((AppearanceColor)palette.Items[option]!).Name;
                    palette.SelectedIndex = option;
                    palette.IsDropDownOpen = false;
                    await Idle();
                    check(original.Text == expected, editor.GetType().Name + "." + name + " commits a selected palette color");
                }
                var narrative = (ScrollViewer)FindHost(main, "NativeCharacterNarrative").NativeControl!;
                var pageLayout = (Grid)narrative.Content!;
                var fields = pageLayout.Children.OfType<StackPanel>().Single(panel => panel.Name == "NativeCharacterInfoFields");
                var sidebar = pageLayout.Children.OfType<StackPanel>().Single(panel => panel.Name == "NativeCharacterInfoSidebar");
                var portrait = sidebar.Children.OfType<Image>().Single();
                var portraitController = Field<Forms.PictureBox>(editor, "picMugshot");
                var previousPortrait = character.Mugshot;
                using (var sample = new SkiaSharp.SKBitmap(16, 20))
                using (var png = sample.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                {
                    character.Mugshot = Convert.ToBase64String(png.ToArray());
                    using var stream = new System.IO.MemoryStream(png.ToArray());
                    portraitController.Image = Majorsilence.Forms.Drawing.Image.FromStream(stream, true);
                }
                await Idle();
                check(Grid.GetColumn(sidebar) == 1 && portrait.Source?.Size.Height == 20,
                    editor.GetType().Name + " places the live portrait alongside Character Info in a wide window");
                var identity = fields.Children.OfType<Avalonia.Controls.Primitives.UniformGrid>().Single();
                int wideColumns = identity.Columns;
                var characterNameInput = identity.GetVisualDescendants().OfType<TextBox>().Single(input => input.Name == "NativetxtCharacterName");
                var heightInput = identity.GetVisualDescendants().OfType<TextBox>().Single(input => input.Name == "NativetxtHeight");
                characterNameInput.Text = "Synthetic legal name";
                heightInput.Text = "171 cm";
                check(character.Name == characterNameInput.Text && character.Height == heightInput.Text &&
                    identity.GetVisualDescendants().OfType<TextBlock>().Any(label => label.Text?.Contains(" cm") == true),
                    editor.GetType().Name + " native identity updates the model and retains metatype measurement hints");
                if (!career)
                {
                    var status = fields.Children.OfType<CheckBox>().Single();
                    var controller = Field<Forms.CheckBox>(editor, "chkCharacterCreated");
                    bool wasChecked = controller.Checked;
                    status.IsChecked = !wasChecked;
                    check(controller.Checked != wasChecked, "Native Creation status checkbox updates the original save controller");
                    controller.Checked = wasChecked;
                }
                var narrativeRows = fields.Children.OfType<StackPanel>().ToArray();
                var narrativeInputs = narrativeRows.Select(row => row.Children.OfType<TextBox>().Single()).ToArray();
                var narrativeCaption = Field<Forms.Label>(editor, "lblDescription");
                string captionText = narrativeCaption.Text;
                var workspaceSize = main.ClientSize;
                try
                {
                    main.ClientSize = new Size(480, 380);
                    var header = narrativeRows[0].Children.OfType<Grid>().Single();
                    var label = header.Children.OfType<TextBlock>().Single();
                    double enlarged = label.FontSize * 1.5;
                    Avalonia.Controls.Documents.TextElement.SetFontSize(narrative, enlarged);
                    narrativeCaption.Text = string.Join(" ", Enumerable.Repeat("A translated character description caption", 4));
                    await Idle();
                    check(Grid.GetRow(sidebar) == 1 && Grid.GetColumn(sidebar) == 0 &&
                        fields.Bounds.Bottom <= sidebar.Bounds.Top && sidebar.Bounds.Right <= pageLayout.Bounds.Width + 1,
                        editor.GetType().Name + " stacks the portrait and reputation below the fields in a narrow window");
                    var removePortrait = sidebar.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdDeleteMugshot");
                    var removeController = Field<Forms.Button>(editor, "cmdDeleteMugshot");
                    var removeCaption = (TextBlock)removePortrait.Content!;
                    removeController.Text = string.Join(" ", Enumerable.Repeat("Remove the current character portrait", 6));
                    await Idle();
                    check(removeCaption.FontSize == enlarged && removeCaption.TextLayout.TextLines.Count is > 1 and <= 3 &&
                        Equals(ToolTip.GetTip(removePortrait), removeController.Text) &&
                        removePortrait.Bounds.Right <= ((WrapPanel)removePortrait.Parent!).Bounds.Width + 1,
                        editor.GetType().Name + " wraps the enlarged portrait command within the narrow sidebar");
                    var previousImage = portraitController.Image;
                    removePortrait.BringIntoView();
                    await Idle();
                    removePortrait.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                    previousImage?.Dispose();
                    check(portrait.Source == null && string.IsNullOrEmpty(character.Mugshot),
                        editor.GetType().Name + " native Remove Portrait clears model and preview");
                    character.Mugshot = previousPortrait;
                    check(identity.Columns < wideColumns && characterNameInput.FontSize == enlarged &&
                        identity.Children.All(cell => cell.Bounds.Right <= identity.Bounds.Width + 1),
                        editor.GetType().Name + " identity fields use fewer columns at 150% text in a narrow workspace");
                    var eyes = identity.GetVisualDescendants().OfType<ComboBox>().Single(palette => palette.Name == "NativetxtEyes");
                    var eyesController = Field<Forms.ComboBox>(editor, "txtEyes");
                    int preferredPaletteWidth = eyesController.DropDownWidth;
                    try
                    {
                        eyesController.DropDownWidth = 1200;
                        eyes.BringIntoView();
                        await Idle();
                        eyes.IsDropDownOpen = true;
                        await Idle();
                        var popup = eyes.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().Single();
                        var popupContent = popup.Child!;
                        double availableWidth = TopLevel.GetTopLevel(eyes)!.ClientSize.Width;
                        check(eyes.IsDropDownOpen && eyes.FontSize == enlarged &&
                            popupContent.MinWidth >= eyes.Bounds.Width && popupContent.MaxWidth <= availableWidth,
                            editor.GetType().Name + " enlarged eye palette keeps its popup within the narrow workspace");
                    }
                    finally
                    {
                        eyes.IsDropDownOpen = false;
                        eyesController.DropDownWidth = preferredPaletteWidth;
                        await Idle();
                    }
                    check(narrative.IsEffectivelyVisible && label.FontSize == enlarged && label.Text == narrativeCaption.Text &&
                        header.Bounds.Bottom <= narrativeInputs[0].Bounds.Top &&
                        narrativeInputs.All(input => input.IsEffectivelyEnabled && input.AcceptsReturn && input.Bounds.Width <= narrative.Bounds.Width) &&
                        narrative.Extent.Height > narrative.Viewport.Height,
                        editor.GetType().Name + " wraps long captions above multiline fields at 150% text in a narrow workspace");
                    narrativeInputs.Last().Focus();
                    narrativeInputs.Last().BringIntoView();
                    await Idle();
                    check(narrative.Offset.Y > 0, editor.GetType().Name + " can scroll to the last narrative field");
                    for (int index = 0; index < narrativeInputs.Length; index++)
                        narrativeInputs[index].Text = "Synthetic narrative " + index + "\nLiteral [O'Brien]";
                    var restored = new Character();
                    restored.LoadSnapshot(character.CaptureSnapshot());
                    check(new[] { restored.Description, restored.Background, restored.Concept, restored.Notes }
                            .SequenceEqual(narrativeInputs.Select(input => input.Text)),
                        editor.GetType().Name + " saves all four native narrative edits in character order");
                }
                finally
                {
                    narrativeCaption.Text = captionText;
                    narrative.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                    main.ClientSize = workspaceSize;
                    await Idle();
                }
                tabs.SelectedTab = view;

                if (career)
                {
                    var contact = NativeContactInteractions.Controllers(editor, enemy: false).Single();
                    var contactRow = NativeContactInteractions.Section(editor, enemy: false).Rows[contact];
                    contactRow.Connection.Value = 2;
                    contactRow.Loyalty.Value = 3;
                    check(contact.ContactObject.Connection == 2 && contact.ContactObject.Loyalty == 3,
                        "Native Career connection and loyalty update the original contact");
                }

                using var panel = new Forms.Panel
                {
                    Bounds = new Rectangle(8, 8, 600, 90),
                    AutoScroll = true,
                    AutoScrollMinSize = new Size(0, 300)
                };
                view.Controls.Add(panel);
                panel.BringToFront();
                using var power = new PowerControl { PowerObject = new Power(character) { Source = "SR4", Page = "195" } };
                power.PowerRatingChanged += _ => { };
                using var spirit = new SpiritControl(career) { SpiritObject = new Spirit(character) };
                spirit.ForceChanged += _ => { };
                spirit.ServicesOwedChanged += _ => { };
                power.Top = 10;
                spirit.Top = 180;
                panel.Controls.Add(power);
                panel.Controls.Add(spirit);
                await Idle();

                var spiritActions = (WrapPanel)((Border)FindHost(main, "NativeSpiritActions").NativeControl!).Child!;
                var spiritName = spiritActions.Children.OfType<ComboBox>().Single();
                check(spiritName.IsEditable && !spiritName.IsEffectivelyVisible,
                    editor.GetType().Name + " clips the editable spirit name with its row");
                var powerFields = (Grid)((Border)FindHost(main, "NativeAdeptPowerRow").NativeControl!).Child!;
                var detailsButton = powerFields.Children.OfType<Button>().Single(button => button.Name == "NativePowerDetails");
                var detailsFlyout = (Flyout)detailsButton.Flyout!;
                detailsFlyout.ShowAt(detailsButton);
                await Idle();
                var sourceLink = ((StackPanel)detailsFlyout.Content!).Children.OfType<Button>().Single();
                check(sourceLink.IsEffectivelyVisible && sourceLink.Content is TextBlock { Text: { } citation } &&
                    citation.EndsWith(" 195", StringComparison.Ordinal),
                    editor.GetType().Name + " exposes an installed adept power's source citation");
                detailsFlyout.Hide();
                var rating = powerFields.Children.OfType<WrapPanel>().SelectMany(group => group.Children)
                    .OfType<NumericUpDown>().Single();
                var spiritInputs = spiritActions.Children.OfType<StackPanel>()
                    .SelectMany(group => group.Children).OfType<NumericUpDown>().ToArray();
                var force = spiritInputs.Single(input => input.Name == "nativenudForce");
                var services = spiritInputs.Single(input => input.Name == "nativenudServices");
                check(rating.IsEffectivelyVisible && !force.IsEffectivelyVisible,
                    editor.GetType().Name + " clips shared spinners below the viewport");
                rating.Value = 2;
                check(power.PowerObject.Rating == 2, editor.GetType().Name + " native adept rating updates the power");
                int panelWidth = panel.Width;
                int rowHeight = power.Height;
                panel.Width = 245;
                await Idle();
                check(power.Width <= panel.ClientSize.Width && power.Height > rowHeight,
                    editor.GetType().Name + " wraps adept power controls as the viewport narrows");
                panel.Width = 1024;
                await Idle();
                check(powerFields.ColumnDefinitions.Count == 4 &&
                    powerFields.Children.Select(Grid.GetColumn).SequenceEqual(new[] { 0, 1, 2, 3 }),
                    editor.GetType().Name + " aligns name, rating, discounts and actions in columns at wide widths");
                panel.Width = panelWidth;
                panel.AutoScrollPosition = new Point(0, 160);
                await Idle();
                check(!rating.IsEffectivelyVisible && force.IsEffectivelyVisible && services.IsEffectivelyVisible,
                    editor.GetType().Name + " scrolls native inputs into and out of the viewport");
                check(force.IsEnabled == career, editor.GetType().Name + " preserves the mode-specific initial Force availability");
                Field<Forms.NumericUpDown>(spirit, "nudForce").Enabled = true;
                spiritName.Text = "Synthetic spirit type";
                check(spiritName.IsEffectivelyVisible && spirit.SpiritObject.Name == "Synthetic spirit type",
                    editor.GetType().Name + " accepts literal spirit or sprite types in the visible row");
                force.Value = 3;
                services.Value = 4;
                check(spirit.SpiritObject.Force == 3 && spirit.SpiritObject.ServicesOwed == 4,
                    editor.GetType().Name + " native Force and Services update the spirit");
                panel.Controls.Remove(spirit);
                await Idle();
                check(TopLevel.GetTopLevel(force) == null && TopLevel.GetTopLevel(services) == null && TopLevel.GetTopLevel(spiritName) == null,
                    editor.GetType().Name + " detaches native inputs when the row is removed");
            }
            finally
            {
                FieldInfo(editor, "_blnIsDirty").SetValue(editor, false);
                editor.Close();
            }
        }
    }

    internal static async Task CheckContactOptionsAsync(frmMain main, Action<bool, string> check)
    {
        using (var contact = new frmSelectContactConnection())
        {
            var pending = contact.ShowDialogAsync(main);
            await Idle();
            contact.ClientSize = new Size(280, 280);
            await Idle();
            var layout = (Grid)((Border)FindHost(contact, "NativeContactConnection").NativeControl!).Child!;
            var scroll = (ScrollViewer)layout.Children[0];
            for (int frame = 0; frame < 10 && layout.Bounds.Width > contact.ClientSize.Width; frame++)
            {
                await Task.Delay(20);
                await Idle();
            }
            check(scroll.Extent.Height > scroll.Viewport.Height && scroll.Extent.Width <= scroll.Viewport.Width &&
                scroll.Bounds.Bottom <= layout.Children[1].Bounds.Top,
                $"Narrow advanced contact options scroll independently of OK and Cancel [client={contact.ClientSize}, extent={scroll.Extent}, viewport={scroll.Viewport}, scroll={scroll.Bounds}, actions={layout.Children[1].Bounds}]");
            var confirm = layout.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOK");
            var color = layout.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdChangeColour");
            var colorContent = (Grid)color.Content!;
            var colorCaption = colorContent.Children.OfType<TextBlock>().Single();
            var confirmCaption = (TextBlock)confirm.Content!;
            Field<Forms.Button>(contact, "cmdChangeColour").Text = string.Join(" ", Enumerable.Repeat("Change group colour", 4));
            Field<Forms.Button>(contact, "cmdOK").Text = string.Join(" ", Enumerable.Repeat("Confirm contact", 4));
            colorCaption.FontSize *= 1.5;
            confirmCaption.FontSize *= 1.5;
            await Idle();
            check(colorCaption.Bounds.Height > colorCaption.FontSize * 2 &&
                colorContent.Children[0].Bounds.Right <= colorCaption.Bounds.Left &&
                colorCaption.Bounds.Right <= colorContent.Bounds.Width + 1 &&
                confirmCaption.Bounds.Height > confirmCaption.FontSize * 2 &&
                confirm.Bounds.Right <= layout.Children[1].Bounds.Width + 1 &&
                scroll.Bounds.Height > 0 && scroll.Bounds.Bottom <= layout.Children[1].Bounds.Top,
                "Advanced contact options wrap enlarged colour and confirmation captions without clipping the swatch or form");
            layout.GetVisualDescendants().OfType<TextBox>().Single(input => input.Name == "NativeGroupName").Text = "Synthetic group";
            layout.GetVisualDescendants().OfType<ComboBox>().Single(input => input.Name == "NativecboMembership").SelectedIndex = 2;
            layout.GetVisualDescendants().OfType<CheckBox>().Single(input => input.Name == "NativeFreeContact").IsChecked = true;
            var total = layout.GetVisualDescendants().OfType<TextBlock>().Single(label => label.Name == "NativeConnectionModifier");
            check(total.Text!.EndsWith(" 2", StringComparison.Ordinal), "Native contact choices update the total connection modifier");
            confirm.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK && contact.GroupName == "Synthetic group" &&
                contact.Membership == 2 && contact.Free,
                "The native contact confirmation returns the existing group, connection, and free-contact values");
        }
    }

    private static void Reveal(Forms.Control? control)
    {
        if (control == null)
            return;

        Reveal(control.Parent);
        if (control is Forms.TabPage page && page.Parent is Forms.TabControl tabs)
            tabs.SelectedTab = page;
    }

    private static Task Idle() => Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).GetTask();

    private static FieldInfo FieldInfo(object owner, string name) =>
        owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static T Field<T>(object owner, string name) => (T)FieldInfo(owner, name).GetValue(owner)!;

    private static Forms.NativeControlHost FindHost(Forms.Form owner, string name) =>
        Descendants(owner.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>().Single(host => host.Name == name);

    private static IEnumerable<Forms.Control> Descendants(IEnumerable<Forms.Control> controls) =>
        controls.SelectMany(control => Descendants(control.Controls.Cast<Forms.Control>()).Prepend(control));
}
