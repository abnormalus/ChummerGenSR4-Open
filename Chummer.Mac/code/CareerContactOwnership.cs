#nullable enable
using System.Collections.Generic;
using System.Linq;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private readonly List<ContactControl> careerContacts = new();
    private readonly List<ContactControl> careerEnemies = new();
    private CreationContactSection? careerContactsView, careerEnemiesView;

    internal IReadOnlyList<ContactControl> CareerContactControllers(bool enemy) => enemy ? careerEnemies : careerContacts;
    internal CreationContactSection? CareerContactPresentation(bool enemy) => enemy ? careerEnemiesView : careerContactsView;

    private void InitializeCareerContactPresentation()
    {
        careerContactsView = new CreationContactSection(this, panContacts, careerContacts, "NativeCareerContacts");
        careerEnemiesView = new CreationContactSection(this, panEnemies, careerEnemies, "NativeCareerEnemies");
    }

    private void DisposeCareerContacts()
    {
        careerContactsView?.Dispose();
        careerEnemiesView?.Dispose();
        foreach (var controller in careerContacts.Concat(careerEnemies))
            if (!controller.IsDisposed) controller.Dispose();
        careerContacts.Clear();
        careerEnemies.Clear();
    }

    private void AddCareerContact(ContactControl controller, bool enemy, bool reveal = false)
    {
        var controllers = enemy ? careerEnemies : careerContacts;
        controllers.Add(controller);
        if (CareerContactPresentation(enemy) is { } presentation)
            presentation.Add(controller, reveal);
        else
            (enemy ? panEnemies : panContacts).Controls.Add(controller);
    }

    private void RemoveCareerContact(ContactControl controller, bool enemy)
    {
        var controllers = enemy ? careerEnemies : careerContacts;
        if (!controllers.Remove(controller))
            return;

        _objCharacter.Contacts.Remove(controller.ContactObject);
        if (CareerContactPresentation(enemy) is { } presentation)
            presentation.Remove(controller);
        else
        {
            Forms.Panel panel = enemy ? panEnemies : panContacts;
            panel.Controls.Remove(controller);
            for (int index = 0; index < controllers.Count; index++)
                controllers[index].Top = index * controllers[index].Height + panel.AutoScrollPosition.Y;
        }
    }
}
