#nullable enable
namespace Chummer;

public partial class frmCreate
{
    private CreationContactSection? creationContactSection, creationEnemySection;
    internal CreationContactSection? ContactPresentation(bool enemy) => enemy ? creationEnemySection : creationContactSection;
    internal void InitializeCreationContactPresentation(bool attachPresenter = true)
    {
        if ((attachPresenter && Avalonia.Application.Current == null) || creationContactSection != null || IsDisposed) return;
        creationContactSection = new CreationContactSection(this, panContacts, creationContactControllers, "nativeCreationContacts", attachPresenter);
        creationEnemySection = new CreationContactSection(this, panEnemies, creationEnemyControllers, "nativeCreationEnemies", attachPresenter);
    }
    private void DisposeCreationContactPresentation()
    {
        creationContactSection?.Dispose(); creationEnemySection?.Dispose();
    }
}
