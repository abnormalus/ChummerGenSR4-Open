using System;
using System.Linq;
using System.Reflection;
using Avalonia.Interactivity;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CreationContactViewportChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);
    private static void Clean(frmCreate editor) => typeof(frmCreate).GetField("_blnIsDirty", Flags).SetValue(editor, false);
    private static bool Dirty(frmCreate editor) => Field<bool>(editor, "_blnIsDirty");
    internal static void Run(Character source, Action<bool, string> check)
    {
        foreach (var method in new[] { CharacterBuildMethod.BP, CharacterBuildMethod.Karma })
        {
            var baseline = new Character(); baseline.LoadSnapshot(source.CaptureSnapshot());
            baseline.Created = false; baseline.IgnoreRules = true; baseline.BuildMethod = method; baseline.Contacts.Clear();
            foreach (bool enemy in new[] { false, true })
                baseline.Contacts.Add(new Contact(baseline) { Name = "Synthetic native contact " + enemy,
                    EntityType = enemy ? ContactType.Enemy : ContactType.Contact, Connection = 2, Loyalty = 1, Membership = 1 });
            // Both owners must start from the same deserialization generation.
            // A second load normalizes existing skill-group maximum metadata.
            byte[] fixture = baseline.CaptureSnapshot();
            baseline = new Character(); baseline.LoadSnapshot(fixture);
            var adapted = new Character(); adapted.LoadSnapshot(fixture);
            using var original = new frmCreate(baseline) { Loading = true };
            using var editor = new frmCreate(adapted) { Loading = true };
            original.Show(); editor.Show(); original.Loading = editor.Loading = false;
            try
            {
                Clean(original); Clean(editor); byte[] before = adapted.CaptureSnapshot();
                editor.InitializeCreationContactPresentation(attachPresenter: false);
                check(adapted.CaptureSnapshot().SequenceEqual(before) && !Dirty(editor)
                    && new[] { false, true }.All(enemy => editor.ContactPresentation(enemy).Rows.Keys.SequenceEqual(
                        enemy ? editor.CreationEnemyControllers : editor.CreationContactControllers)),
                    "Native-adapted contact viewport retains clean canonical ordered ownership in " + method);
                foreach (bool enemy in new[] { false, true })
                {
                    var controller = (enemy ? editor.CreationEnemyControllers : editor.CreationContactControllers).Single();
                    var legacy = (enemy ? original.CreationEnemyControllers : original.CreationContactControllers).Single();
                    var row = editor.ContactPresentation(enemy).Rows[controller];
                    check(controller.Parent == null && ReferenceEquals(controller.CreationViewportOwner, editor)
                        && row.Connection.Minimum == (enemy ? 1 : 0) && row.Connection.Maximum == 6
                        && row.Loyalty.Minimum == 1 && row.Loyalty.Maximum == 6,
                        "Native-adapted contact and enemy inputs retain detached modal owner and original ranges enemy=" + enemy + " " + method);
                    string literal = "Synthetic literal [O'Brien] " + enemy;
                    legacy.CreationNameInput.Text = literal; row.Name.Text = literal;
                    legacy.CreationConnectionInput.Value = enemy ? 4 : 0; row.Connection.Value = enemy ? 4 : 0;
                    legacy.CreationLoyaltyInput.Value = 3; row.Loyalty.Value = 3;
                    check(baseline.CaptureSnapshot().SequenceEqual(adapted.CaptureSnapshot()) && Dirty(editor),
                        "Native-adapted contact literal and rating edits match original owner snapshot enemy=" + enemy + " " + method
                        + " [nativeModel=" + controller.ContactObject.Name + "/" + controller.ContactObject.Connection + "/" + controller.ContactObject.Loyalty
                        + "; legacyModel=" + legacy.ContactObject.Name + "/" + legacy.ContactObject.Connection + "/" + legacy.ContactObject.Loyalty
                        + "; owner=" + editor.Visible + "/" + editor.Enabled + "; panel=" + Field<Forms.Panel>(editor, enemy ? "panEnemies" : "panContacts").Visible
                        + "/" + Field<Forms.Panel>(editor, enemy ? "panEnemies" : "panContacts").Enabled + "; row=" + controller.Visible + "/" + controller.Enabled + "]");
                    original.UpdateCharacterInfo(); editor.UpdateCharacterInfo();
                    check(Field<Forms.Label>(original, "lblContactsBP").Text == Field<Forms.Label>(editor, "lblContactsBP").Text
                        && Field<Forms.Label>(original, "lblEnemiesBP").Text == Field<Forms.Label>(editor, "lblEnemiesBP").Text,
                        "Native-adapted contact and enemy accounting matches legacy owner in " + method);
                    byte[] valid = adapted.CaptureSnapshot(); row.Connection.Value = 2.5m;
                    check(adapted.CaptureSnapshot().SequenceEqual(valid) && row.Connection.Value == controller.CreationConnectionInput.Value,
                        "Native-adapted contact fractional rating rejects and restores original input enemy=" + enemy + " " + method);
                    controller.Enabled = false; row.Name.Text = "Blocked draft"; row.Connection.Value = 5;
                    row.Delete.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
                    check(adapted.CaptureSnapshot().SequenceEqual(valid),
                        "Native-adapted disabled contact rejects queued text numeric and delete edits enemy=" + enemy + " " + method);
                    controller.Enabled = true;
                    byte[] beforeGroup = adapted.CaptureSnapshot();
                    controller.ContactObject.Free = true; legacy.ContactObject.Free = true;
                    controller.ContactObject.Membership = 2; legacy.ContactObject.Membership = 2;
                    controller.CreationGroupValue.Text = controller.GroupRating.ToString(); legacy.CreationGroupValue.Text = legacy.GroupRating.ToString();
                    typeof(frmCreate).GetMethod(enemy ? "objEnemy_ConnectionGroupRatingChanged" : "objContact_ConnectionGroupRatingChanged", Flags)
                        .Invoke(editor, new object[] { controller });
                    typeof(frmCreate).GetMethod(enemy ? "objEnemy_ConnectionGroupRatingChanged" : "objContact_ConnectionGroupRatingChanged", Flags)
                        .Invoke(original, new object[] { legacy });
                    check(baseline.CaptureSnapshot().SequenceEqual(adapted.CaptureSnapshot())
                        && row.Group.Content.ToString().Contains(controller.GroupRating.ToString()) && !beforeGroup.SequenceEqual(adapted.CaptureSnapshot()),
                        "Native-adapted contact source group Free refresh retains canonical data enemy=" + enemy + " " + method);
                    // The existing public headless picker response drives the real
                    // OpenFileDialog path without adding a production dialog hook.
                    var pickerResponse = Forms.Headless.HeadlessRenderer.OpenFileResponse;
                    string linkedPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chummer-synthetic-contact-link-" + Guid.NewGuid().ToString("N") + ".chum");
                    try
                    {
                        var attach = row.LinkMenu.ItemsSource.Cast<Avalonia.Controls.MenuItem>().First();
                        Clean(editor); byte[] beforeLink = adapted.CaptureSnapshot();
                        Forms.Headless.HeadlessRenderer.OpenFileResponse = null;
                        attach.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.MenuItem.ClickEvent));
                        check(beforeLink.SequenceEqual(adapted.CaptureSnapshot()) && !Dirty(editor),
                            "Native-adapted contact file picker cancellation retains exact snapshot and clean state enemy=" + enemy + " " + method);
                        System.IO.File.WriteAllBytes(linkedPath, source.CaptureSnapshot());
                        Forms.Headless.HeadlessRenderer.OpenFileResponse = _ => new[] { linkedPath };
                        attach.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.MenuItem.ClickEvent));
                        check(controller.ContactObject.FileName == linkedPath && controller.ContactObject.RelativeFileName.StartsWith("../") && Dirty(editor),
                            "Native-adapted contact file attachment uses the actual picker and original path notification enemy=" + enemy + " " + method);
                        var saved = new Character(); saved.LoadSnapshot(adapted.CaptureSnapshot());
                        check(saved.Contacts.Single(c => c.Name == controller.ContactObject.Name).FileName == linkedPath,
                            "Native-adapted contact association survives unchanged save format enemy=" + enemy + " " + method);
                        // Restore the paired fixture through the same original handler.
                        Forms.Headless.HeadlessRenderer.OpenFileResponse = _ => new[] { linkedPath };
                        legacy.AttachCreationCharacter();
                    }
                    finally { Forms.Headless.HeadlessRenderer.OpenFileResponse = pickerResponse; System.IO.File.Delete(linkedPath); }
                    Field<Forms.Button>(original, enemy ? "cmdAddEnemy" : "cmdAddContact").PerformClick();
                    Field<Forms.Button>(editor, enemy ? "cmdAddEnemy" : "cmdAddContact").PerformClick();
                    var added = (enemy ? editor.CreationEnemyControllers : editor.CreationContactControllers).Last();
                    var addedLegacy = (enemy ? original.CreationEnemyControllers : original.CreationContactControllers).Last();
                    var addedRow = editor.ContactPresentation(enemy).Rows[added];
                    addedRow.Name.Text = "Synthetic dynamic"; addedLegacy.CreationNameInput.Text = "Synthetic dynamic";
                    check(added.Parent == null && editor.ContactPresentation(enemy).Rows.Count == 2
                        && baseline.CaptureSnapshot().SequenceEqual(adapted.CaptureSnapshot()),
                        "Native-adapted dynamically added contact joins the same owned viewport enemy=" + enemy + " " + method);
                    bool confirm = adapted.Options.ConfirmDelete, originalConfirm = baseline.Options.ConfirmDelete;
                    try { baseline.Options.ConfirmDelete = adapted.Options.ConfirmDelete = false; row.Delete.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent)); legacy.CreationDeleteButton.PerformClick(); }
                    finally { adapted.Options.ConfirmDelete = confirm; baseline.Options.ConfirmDelete = originalConfirm; }
                    Clean(editor); byte[] afterDelete = adapted.CaptureSnapshot();
                    row.Name.Text = "Stale name"; row.Connection.Value = 6;
                    row.Delete.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
                    check(!editor.ContactPresentation(enemy).Rows.ContainsKey(controller) && controller.IsDisposed && !Dirty(editor)
                        && afterDelete.SequenceEqual(adapted.CaptureSnapshot()) && baseline.CaptureSnapshot().SequenceEqual(adapted.CaptureSnapshot()),
                        "Native-adapted accepted contact removal releases bindings and retains other models enemy=" + enemy + " " + method);
                }
            }
            finally { Clean(original); Clean(editor); original.Close(); editor.Close(); }
        }
    }
}
