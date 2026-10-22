#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Chummer;

public partial class frmCreate
{
    // Presentation panels must not define which contacts participate in rules.
    // These ordered controllers retain the original event and accounting paths
    // when their native rows are moved into a coherent scrolling viewport.
    private readonly List<ContactControl> creationContactControllers = new();
    private readonly List<ContactControl> creationEnemyControllers = new();
    internal IReadOnlyList<ContactControl> CreationContactControllers => creationContactControllers;
    internal IReadOnlyList<ContactControl> CreationEnemyControllers => creationEnemyControllers;

    private void InitializeCreationContactControllerLifetime()
    {
        Disposed += (_, _) =>
        {
            DisposeCreationContactPresentation();
            foreach (var controller in creationContactControllers.Concat(creationEnemyControllers).ToArray())
                if (!controller.IsDisposed) controller.Dispose();
            creationContactControllers.Clear(); creationEnemyControllers.Clear();
        };
    }
    private void AddCreationContactController(ContactControl controller, bool enemy)
    {
        (enemy ? creationEnemyControllers : creationContactControllers).Add(controller);
        var presentation = ContactPresentation(enemy);
        if (presentation == null) (enemy ? panEnemies : panContacts).Controls.Add(controller);
        else presentation.Add(controller, reveal: true);
    }
    private void RemoveCreationContactController(ContactControl controller, bool enemy)
    {
        (enemy ? creationEnemyControllers : creationContactControllers).Remove(controller);
        (enemy ? panEnemies : panContacts).Controls.Remove(controller);
        ContactPresentation(enemy)?.Remove(controller);
    }
}
