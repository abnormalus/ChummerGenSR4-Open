namespace Chummer;

// Shared synthetic boundaries keep native and headless owner checks comparable.
internal static class CreationContactRuleFixture
{
    internal static byte[] Create(Character source, CharacterBuildMethod method, bool qualityLimit)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        character.Created = false; character.IgnoreRules = false; character.BuildMethod = method;
        character.Contacts.Clear(); character.Qualities.Clear(); character.Improvements.Clear();
        for (int i = 0; i < (qualityLimit ? 3 : 5); i++) character.Contacts.Add(new Contact(character)
        { Name = "Synthetic enemy boundary " + i, EntityType = ContactType.Enemy, Connection = 4, Loyalty = 1 });
        if (qualityLimit) character.Qualities.Add(new Quality(character)
        { Name = "Synthetic negative boundary", Type = QualityType.Negative, BP = -20, ContributeToLimit = true });
        return character.CaptureSnapshot();
    }
}
