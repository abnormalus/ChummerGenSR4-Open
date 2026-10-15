#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Chummer;

// Field mappings follow Shadowrun4-FoundryVTT's MIT-licensed importer; see docs/FOUNDRY_EXPORT.md.
// Export reads the live model (including unsaved changes), never rewrites a .chum file.
internal sealed class FoundryActorExporter
{
    internal enum Target { Version16, Version126 }
    private readonly Character _character;
    private readonly Target _target;
    private readonly JsonArray _items = new();
    private readonly Dictionary<object, string> _ids = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Cyberware, JsonObject> _implants = new();
    private readonly List<string> _warnings = new();
    internal static readonly IReadOnlyDictionary<string, string> Attributes = new Dictionary<string, string> {
        ["BOD"]="BODY", ["AGI"]="AGILITY", ["REA"]="REACTION", ["STR"]="STRENGTH", ["CHA"]="CHARISMA",
        ["INT"]="INTUITION", ["LOG"]="LOGIC", ["WIL"]="WILLPOWER", ["EDG"]="EDGE", ["MAG"]="MAGIC", ["RES"]="RESONANCE" };

    private FoundryActorExporter(Character character, Target target) { _character = character; _target = target; }
    internal static JsonObject Export(Character character, Target target = Target.Version16) => new FoundryActorExporter(character, target).Build();
    internal static string Serialize(Character character, Target target = Target.Version16) => Export(character, target).ToJsonString(new JsonSerializerOptions { WriteIndented = true });

    internal static JsonObject Obj(params (string Key, object? Value)[] values)
    {
        var result = new JsonObject();
        foreach (var (key, value) in values) result[key] = value is JsonNode node ? node.DeepClone() : JsonSerializer.SerializeToNode(value);
        return result;
    }
    internal static XElement Snapshot(Action<XmlTextWriter> save)
    {
        using var text = new StringWriter(CultureInfo.InvariantCulture);
        using (var writer = new XmlTextWriter(text)) { save(writer); writer.Flush(); }
        return XElement.Parse(text.ToString());
    }
    internal static string Text(XElement element, string field) => (string?)element.Element(field) ?? "";
    internal static decimal Number(string value, decimal fallback = 0)
    {
        if (decimal.TryParse(value, NumberStyles.Number, GlobalOptions.Instance.CultureInfo, out decimal number)) return number;
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out number) ? number : fallback;
    }
    internal static int Integer(string value, int fallback = 0) => (int)Number(value, fallback);
    private static int Capacity(string value) => value.TrimStart().StartsWith('[') ? 0 : Integer(Regex.Match(value, @"^\s*\d+").Value);
    internal static int Effective(string value)
    {
        var matches = Regex.Matches(value, @"-?\d+");
        return matches.Count == 0 ? 0 : int.Parse(matches[^1].Value, CultureInfo.InvariantCulture);
    }
    private string Id(object item)
    {
        if (!_ids.TryGetValue(item, out string? id)) _ids[item] = id = Guid.NewGuid().ToString("N")[..16];
        return id;
    }
    private static string Source(XElement raw) => (Text(raw, "source") + " " + Text(raw, "page")).Trim();
    private static string SkillKey(string name) => Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]", "");
    internal static string AddNotes(string original, IEnumerable<string> notices)
    {
        var lines = notices.Where(line => !string.IsNullOrWhiteSpace(line)).Distinct(StringComparer.Ordinal).ToArray();
        lines = lines.Where(line => !original.Split('\n').Contains("• " + line, StringComparer.Ordinal)).ToArray();
        if (lines.Length == 0) return original;
        const string header = "Chummer export — manual handling required";
        return original + (original.Length == 0 ? "" : "\n\n") + (original.Contains(header, StringComparison.Ordinal) ? "" : header + "\n") + string.Join("\n", lines.Select(line => "• " + line));
    }
    private static void Note(JsonObject item, params string[] notices)
    {
        var system = (JsonObject)item["system"]!;
        system["notes"] = AddNotes(system["notes"]?.GetValue<string>() ?? "", notices);
    }
    internal static void Set(JsonObject root, string path, int value)
    {
        var keys = path.Split('.');
        var node = root;
        foreach (string key in keys[..^1]) { node[key] ??= new JsonObject(); node = (JsonObject)node[key]!; }
        node[keys[^1]] = value;
    }

    // These are Foundry's input fields, not its recalculated derivedStats cache.
    internal static Dictionary<string, int> Capture(Character character)
    {
        var result = Attributes.ToDictionary(pair => "sheetStats." + pair.Value, pair =>
            pair.Key == "MAG" && !character.MAGEnabled || pair.Key == "RES" && !character.RESEnabled ? 0 : character.GetAttribute(pair.Key).TotalValue);
        int uninjuredInitiative = character.INT.TotalValue + character.REA.TotalValue + character.GetAttribute("INI").AttributeModifiers + new ImprovementManager(character).ValueOf(Improvement.ImprovementType.Initiative);
        uninjuredInitiative = Math.Clamp(uninjuredInitiative, 0, Math.Max(0, character.GetAttribute("INI").MetatypeAugmentedMaximum));
        result["modifiers.initiative.bonuses.physical"] = uninjuredInitiative - character.INT.TotalValue - character.REA.TotalValue;
        result["modifiers.initiative.bonuses.astral"] = 0;
        result["modifiers.initiative.bonuses.matrix"] = Integer(Regex.Match(character.MatrixInitiative, @"^-?\d+").Value) - character.INT.TotalValue;
        result["modifiers.initiative.passes.physical"] = Math.Max(0, Effective(character.InitiativePasses) - 1);
        result["modifiers.initiative.passes.astral"] = character.MagicianEnabled ? Math.Max(0, Effective(character.AstralInitiativePasses) - 1) : 0;
        result["modifiers.initiative.passes.matrix"] = Math.Max(0, Effective(character.MatrixInitiativePasses));
        return result;
    }

    private JsonObject Build()
    {
        foreach (var skill in _character.Skills) AddSkill(skill);
        foreach (var quality in _character.Qualities) {
            var raw = Snapshot(quality.Save);
            var item = Add(quality, raw, raw, "Quality", quality.DisplayName);
            var system = (JsonObject)item["system"]!;
            system["category"] = quality.Type.ToString(); system["bp"] = Math.Abs(quality.BP);
        }
        foreach (var armor in _character.Armor) AddArmor(armor);
        foreach (var weapon in _character.Weapons) AddWeapon(weapon);
        foreach (var gear in _character.Gear) AddGear(gear);
        foreach (var ware in _character.Cyberware) AddImplant(ware);
        foreach (var spell in _character.Spells) AddSpell(spell);
        foreach (var power in _character.Powers) {
            var raw = Snapshot(power.Save);
            var item = Add(power, raw, raw, "Power", power.DisplayNameShort);
            var system = (JsonObject)item["system"]!;
            system["rating"] = Math.Max(1, power.Rating); system["cost"] = power.PointsPerLevel;
            system["ratingMode"] = power.LevelsEnabled ? "rated" : "none";
            Note(item, "Power rules and conditional bonuses are not automated. Current attribute/skill bonuses are included in the exported totals; do not add them again.");
        }
        foreach (var vehicle in _character.Vehicles) {
            var raw = Snapshot(vehicle.Save);
            var item = Add(vehicle, raw, Snapshot(vehicle.Print), "Gear", vehicle.DisplayNameShort);
            Note(item, "Vehicle retained as an inventory record. Vehicle actor creation, vehicle statistics and mounted weapons require manual setup in Foundry.");
            foreach (var gear in vehicle.Gear) AddGear(gear, Id(vehicle));
            foreach (var weapon in vehicle.Weapons) AddWeapon(weapon, Id(vehicle));
        }
        foreach (var program in _character.TechPrograms) {
            var raw = Snapshot(program.Save);
            var item = Add(program, raw, raw, "Program", program.DisplayName);
            Note(item, "Complex form retained as a Program record. Technomancer threading, fading and form options require manual setup; original form data is retained in export metadata.");
        }
        var systemData = new JsonObject();
        foreach (var pair in Capture(_character)) Set(systemData, pair.Key, pair.Value);
        FoundryWareEffects.Apply(_character, systemData, _implants);
        Set(systemData, "sheetStats.CURRENTEDGE", _character.EDG.TotalValue);
        decimal foundryEssenceLoss = 0;
        if (_target == Target.Version126) {
            decimal cyber = _implants.Where(p => p.Key.SourceType == Improvement.ImprovementSource.Cyberware).Sum(p => p.Value["system"]!["essenceActual"]!.GetValue<decimal>());
            decimal bio = _implants.Where(p => p.Key.SourceType == Improvement.ImprovementSource.Bioware).Sum(p => p.Value["system"]!["essenceActual"]!.GetValue<decimal>());
            foundryEssenceLoss = Math.Max(cyber, bio) + Math.Min(cyber, bio) / 2;
        }
        // 1.26 subtracts installed implant loss when displaying Essence; 1.6 displays ESSENCE directly.
        ((JsonObject)systemData["sheetStats"]!)["ESSENCE"] = _character.Essence + foundryEssenceLoss;
        systemData["metaData"] = Obj(("nuyen", _character.Nuyen), ("karma", _character.Karma), ("totalKarma", _character.CareerKarma),
            ("streetCred", _character.TotalStreetCred), ("notoriety", _character.TotalNotoriety), ("publicAwareness", _character.TotalPublicAwareness), ("age", int.TryParse(_character.Age, out int age) ? age : 0));
        if (!string.IsNullOrWhiteSpace(_character.Age) && !int.TryParse(_character.Age, out _)) _warnings.Add("Age was not numeric; Foundry age is 0. Original age: " + _character.Age);
        if (_character.Contacts.Count > 0) _warnings.Add("Contacts are retained below as notes; set up Foundry connections manually.\n" + string.Join("\n", _character.Contacts.Select(c => c.Name)));
        string identityNotes = string.Join("\n\n", new[] { _character.Description, _character.Background, _character.Notes, _character.GameNotes }.Where(s => !string.IsNullOrWhiteSpace(s)));
        systemData["descriptionAndNotes"] = Obj(("metatype", _character.Metatype), ("gender", _character.Sex), ("height", _character.Height),
            ("weight", _character.Weight), ("eyes", _character.Eyes), ("hair", _character.Hair), ("skin", _character.Skin),
            ("bio", _character.Description), ("notes", AddNotes(identityNotes, _warnings)));
        string tradition = _character.MagicTradition.ToLowerInvariant();
        string drainFormula = XmlManager.Instance.Load("traditions.xml").SelectNodes("/chummer/traditions/tradition")!.Cast<XmlNode>()
            .FirstOrDefault(node => node["name"]?.InnerText == _character.MagicTradition)?["drain"]?.InnerText ?? "LOG";
        systemData["magic"] = Obj(("magician", _character.MagicianEnabled), ("adept", _character.AdeptEnabled),
            ("tradition", tradition.Contains("hermet") ? "HERMETIC" : tradition.Contains("shaman") ? "SHAMAN" : tradition.Contains("chaos") ? "CHAOS" : tradition.Contains("wicca") ? "WICCA" : ""), ("drainAttribute", drainFormula.Contains("CHA", StringComparison.Ordinal) ? "CHARISMA" : drainFormula.Contains("INT", StringComparison.Ordinal) ? "INTUITION" : "LOGIC"));
        if (_target == Target.Version126) {
            ((JsonObject)systemData["magic"]!)["spellpower"] = _character.MAGMagician;
            ((JsonObject)systemData["magic"]!)["powerPoints"] = _character.MAGAdept;
            systemData["technomancy"] = Obj(("technomancer", _character.TechnomancerEnabled));
        }
        systemData["technomancer"] = _character.TechnomancerEnabled;
        systemData["conditionMonitor"] = Obj(("physical", Obj(("value", _character.PhysicalCMFilled), ("max", _character.PhysicalCM))),
            ("stun", Obj(("value", _character.StunCMFilled), ("max", _character.StunCM))));
        // Foundry adds equipped armor to these actor-level bonus fields. Avoid counting the items twice.
        int equippedBallistic = _character.Armor.Where(a => a.Equipped).Sum(a => a.TotalBallistic);
        int equippedImpact = _character.Armor.Where(a => a.Equipped).Sum(a => a.TotalImpact);
        if (_target == Target.Version126) {
            var equipped = _character.Armor.Where(a => a.Equipped).ToArray();
            equippedBallistic = ArmorStack(equipped, true); equippedImpact = ArmorStack(equipped, false);
        }
        systemData["armor"] = Obj(("ballistic", _character.TotalBallisticArmorRating - equippedBallistic), ("impact", _character.TotalImpactArmorRating - equippedImpact));
        string name = !string.IsNullOrWhiteSpace(_character.Alias) ? _character.Alias.Trim() : !string.IsNullOrWhiteSpace(_character.Name) ? _character.Name.Trim() : "Chummer character";
        string image = "icons/svg/mystery-man.svg";
        if (!string.IsNullOrEmpty(_character.Mugshot)) {
            using var pixels = PortraitImage.Decode(Convert.FromBase64String(_character.Mugshot));
            using var portrait = SkiaSharp.SKImage.FromBitmap(pixels);
            using var png = portrait.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            image = "data:image/png;base64," + Convert.ToBase64String(png.ToArray());
        }
        return Obj(("name", name), ("type", "character"), ("img", image), ("system", systemData), ("items", _items),
            ("effects", new JsonArray()), ("folder", null), ("ownership", Obj(("default", 0))),
            ("flags", Obj(("shadowrun4e", Obj(("chummerExport", Obj(("formatVersion", 1), ("targetSystemVersion", Version), ("originalAge", _character.Age))))))),
            ("_stats", Obj(("systemId", "shadowrun4e"), ("systemVersion", Version), ("coreVersion", "14.368"))));
    }
    private string Version => _target == Target.Version16 ? "1.6.0" : "1.26.1";
    private static string Stacking(Armor armor) => armor.Category is "Helmets" or "Shields" ? "accessory" : armor.Name.Contains("Form-Fitting", StringComparison.OrdinalIgnoreCase) ? "formFitting" : "standard";
    private static int ArmorStack(IEnumerable<Armor> armor, bool ballistic) => armor.Where(a => Stacking(a) == "standard").Select(a => ballistic ? a.TotalBallistic : a.TotalImpact).DefaultIfEmpty().Max() +
        armor.Where(a => Stacking(a) != "standard").Sum(a => ballistic ? a.TotalBallistic : a.TotalImpact);

    private JsonObject Add(object identity, XElement raw, XElement printed, string type, string? name = null, string? parent = null)
    {
        string avail = Text(printed, "avail_english"); if (avail == "") avail = Text(printed, "avail");
        var system = Obj(("description", ""), ("notes", Text(raw, "notes")), ("cost", Integer(Text(printed, "owncost"), Integer(Text(printed, "cost")))),
            ("avail", Effective(Regex.Match(avail, @"^\d+").Value)), ("availability", avail), ("legality", avail.EndsWith('F') ? "F" : avail.EndsWith('R') ? "R" : ""),
            ("quantity", Math.Max(1, Integer(Text(raw, "qty"), 1))), ("rating", Integer(Text(raw, "rating"))), ("capacity", Capacity(Text(printed, "capacity"))),
            ("source", Source(raw)), ("equipped", bool.TryParse(Text(raw, "equipped"), out bool equipped) && equipped));
        var result = Obj(("_id", Id(identity)), ("name", name ?? Text(raw, "name")), ("type", type), ("img", "icons/svg/item-bag.svg"),
            ("system", system), ("effects", new JsonArray()), ("flags", Obj(("shadowrun4e", Obj(("chummerExport", Obj(("parentId", parent), ("originalXml", raw.ToString(SaveOptions.DisableFormatting)))))))));
        _items.Add(result);
        return result;
    }
    internal static int EffectiveSkillRating(Skill skill, Character character)
    {
        var printed = Snapshot(skill.Print);
        int rating = Integer(Text(printed, "rating"));
        return rating == 0 ? 0 : Math.Max(0, Integer(Text(printed, "total")) - Integer(Text(printed, "attributemod")) - character.WoundModifiers);
    }
    private void AddSkill(Skill skill)
    {
        var raw = Snapshot(skill.Save); var printed = Snapshot(skill.Print);
        // 1.6 getSkill() compares names without normalizing spaces/hyphens. Its UI uses the label for display.
        string internalName = _target == Target.Version16 && !skill.KnowledgeSkill && !skill.ExoticSkill ? SkillKey(skill.Name) : skill.Name;
        var item = Add(skill, raw, printed, "Skill", internalName);
        string category = skill.SkillCategory.Replace(" Active", "", StringComparison.Ordinal).ToLowerInvariant();
        var system = (JsonObject)item["system"]!;
        system["attribute"] = Attributes.GetValueOrDefault(skill.Attribute, "LOGIC");
        system["category"] = category == "magical" ? "magic" : category == "interest" ? "hobby" : category;
        system["type"] = skill.KnowledgeSkill ? "knowledge" : "active"; system["group"] = skill.SkillGroup;
        system["specialization"] = skill.Specialization; system["label"] = skill.KnowledgeSkill || skill.ExoticSkill ? "" : "sr4.skills." + SkillKey(skill.Name);
        int rating = Integer(Text(printed, "rating"));
        system["rating"] = EffectiveSkillRating(skill, _character);
        if (skill.RatingModifiers != 0 || skill.DicePoolModifiers != 0) system["description"] = "Chummer effective skill rating includes current augmentation and pool bonuses. Base rating: " + rating + ".";
    }
    private void AddGear(Gear gear, string? parent = null)
    {
        var raw = gear is Commlink commlink ? Snapshot(commlink.Save) : gear is OperatingSystem os ? Snapshot(os.Save) : Snapshot(gear.Save);
        var printed = gear is Commlink commlink2 ? Snapshot(commlink2.Print) : gear is OperatingSystem os2 ? Snapshot(os2.Print) : Snapshot(gear.Print);
        string type = gear is Commlink ? "Commlink" : gear.IsProgram || gear is OperatingSystem ? "Program" : gear.Category == "Ammunition" ? "Ammo" : gear.Category == "Foci" ? "Focus" : gear.Category == "Fetishes" ? "Fetish" : "Gear";
        var item = Add(gear, raw, printed, type, gear.DisplayName, parent);
        ((JsonObject)item["system"]!)["capacity"] = Capacity(gear.CalculatedCapacity);
        if (gear is Commlink link) {
            var system = (JsonObject)item["system"]!;
            system["response"] = link.TotalResponse; system["signal"] = link.TotalSignal;
            system["firewall"] = link.TotalFirewall; system["os"] = link.TotalSystem;
        }
        if (type == "Ammo") Note(item, "Loaded ammunition bonuses are already included in the current exported weapon values. Recalculate the weapon when changing ammunition.");
        if (parent != null) Note(item, "Contained by another Chummer item (parent ID " + parent + "). Foundry displays this as a separate inventory entry.");
        foreach (var child in gear.Children) AddGear(child, Id(gear));
    }
    private void AddArmor(Armor armor)
    {
        var raw = Snapshot(armor.Save); var item = Add(armor, raw, Snapshot(armor.Print), "Armor", armor.DisplayName);
        var system = (JsonObject)item["system"]!;
        system["ballisticarmor"] = armor.TotalBallistic; system["impactarmor"] = armor.TotalImpact;
        system["capacity"] = Capacity(armor.CalculatedCapacity);
        if (_target == Target.Version126) system["stackingType"] = Stacking(armor);
        var mods = new JsonArray();
        foreach (var mod in armor.ArmorMods) {
            var modRaw = Snapshot(mod.Save); var child = Add(mod, modRaw, Snapshot(mod.Print), "Armor Mod", mod.DisplayName, Id(armor));
            // Parent armor contains calculated modification bonuses; linked child records must not add them again.
            var childSystem = (JsonObject)child["system"]!;
            childSystem["ballisticBonus"] = 0; childSystem["impactBonus"] = 0;
            Note(child, "Armor totals already include this modification. Recalculate the parent armor if changing or removing it; additional effects require manual handling.");
            mods.Add(Id(mod));
        }
        system["installedModIds"] = mods;
        foreach (var gear in armor.Gear) AddGear(gear, Id(armor));
    }
    private void AddWeapon(Weapon weapon, string? parent = null)
    {
        var raw = Snapshot(weapon.Save); var printed = Snapshot(weapon.Print);
        bool melee = weapon.WeaponType == "Melee";
        var item = Add(weapon, raw, printed, melee ? "Melee Weapon" : "Ranged Weapon", weapon.DisplayName, parent);
        var system = (JsonObject)item["system"]!;
        string damage = weapon.CalculatedDamage(0, true);
        var match = Regex.Match(damage, @"^\s*(\d+)([PS])");
        system["damage"] = match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        system["damageType"] = damage.Contains("(e)", StringComparison.OrdinalIgnoreCase) ? "ELECTRICITY" : damage.Contains('S') ? "STUN" : "PHYSICAL";
        system["ap"] = Integer(weapon.TotalAP); system["armorType"] = melee ? "impact" : "ballistic";
        system["attackSkill"] = AttackSkill(weapon.Category); system["noStrengthBonus"] = true;
        system["reach"] = weapon.TotalReach;
        system["rc"] = Effective(weapon.TotalRC);
        string mode = weapon.CalculatedMode;
        system["mode"] = mode.Contains("FA", StringComparison.Ordinal) ? "SEMI_BURST_FULL_AUTO" : mode.Contains("BF", StringComparison.Ordinal) ? "SEMI_BURST" : mode.Contains("SA", StringComparison.Ordinal) ? "SEMI_AUTOMATIC" : "SINGLE_SHOT";
        var ammo = Regex.Match(weapon.CalculatedAmmo(true), @"^(\d+)\(([^)]+)\)");
        system["maxAmmo"] = ammo.Success ? int.Parse(ammo.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        system["ammoFeed"] = ammo.Success ? ammo.Groups[2].Value : "";
        system["currentAmmo"] = weapon.AmmoRemaining;
        system["range"] = weapon.RangeShort + "/" + weapon.RangeMedium + "/" + weapon.RangeLong + "/" + weapon.RangeExtreme;
        var mods = new JsonArray();
        foreach (var accessory in weapon.WeaponAccessories) {
            var child = Add(accessory, Snapshot(accessory.Save), Snapshot(accessory.Print), "Weapon Mod", accessory.DisplayName, Id(weapon));
            WeaponModRecord(child, Snapshot(accessory.Save)); mods.Add(Id(accessory));
            foreach (var gear in accessory.Gear) AddGear(gear, Id(accessory));
        }
        foreach (var mod in weapon.WeaponMods) {
            var child = Add(mod, Snapshot(mod.Save), Snapshot(mod.Print), "Weapon Mod", mod.DisplayName, Id(weapon));
            WeaponModRecord(child, Snapshot(mod.Save)); mods.Add(Id(mod));
        }
        system["installedModIds"] = mods;
        system["smartlink"] = weapon.WeaponAccessories.Any(a => a.Name.Contains("Smart", StringComparison.OrdinalIgnoreCase)) || weapon.WeaponMods.Any(m => m.Name.Contains("Smart", StringComparison.OrdinalIgnoreCase));
        if (melee || mods.Count != 0 || !match.Success || weapon.TotalAP.Contains("half", StringComparison.OrdinalIgnoreCase))
            Note(item, "Weapon values include Chummer's current Strength, ammunition and attachment bonuses. Recalculate damage/AP/RC manually after changing these; attachment records do not add the same bonuses again. Original damage: " + weapon.Damage + "; current damage: " + damage + "; AP: " + weapon.TotalAP + ".");
    }
    private static void WeaponModRecord(JsonObject item, XElement raw)
    {
        var system = (JsonObject)item["system"]!;
        system["damageBonus"] = 0; system["apBonus"] = 0; system["rcBonus"] = 0;
        system["grantsSmartlink"] = false; system["modeOverride"] = "";
        string mount = Text(raw, "mount").Split('/')[0].ToLowerInvariant();
        system["mount"] = mount is "top" or "barrel" or "under" ? mount : "internal";
        Note(item, "This attachment's current bonuses are already included in the parent weapon. Changing/removing it requires manually recalculating the parent weapon.");
    }
    private void AddImplant(Cyberware ware, string? parent = null)
    {
        var raw = Snapshot(ware.Save); var item = Add(ware, raw, Snapshot(ware.Print), "Implant", ware.DisplayName, parent);
        _implants.Add(ware, item);
        var system = (JsonObject)item["system"]!;
        string grade = ware.Grade.Name.ToLowerInvariant().Replace("ware", "", StringComparison.Ordinal);
        string targetGrade = grade.Contains("alpha", StringComparison.Ordinal) ? "ALPHA" : grade.Contains("beta", StringComparison.Ordinal) ? "BETA" : grade.Contains("delta", StringComparison.Ordinal) ? "DELTA" : grade.Contains("used", StringComparison.Ordinal) || grade.Contains("second", StringComparison.Ordinal) ? "SECOND_HAND" : "STANDARD";
        decimal gradeFactor = targetGrade switch { "ALPHA" => .8m, "BETA" => .7m, "DELTA" => .5m, "SECOND_HAND" => 1.25m, _ => 1m };
        system["grade"] = targetGrade;
        system["type"] = ware.SourceType == Improvement.ImprovementSource.Bioware ? "BIOWARE" : "CYBERWARE";
        // Chummer's Essence calculation charges only root implants. Children consume their parent's capacity.
        decimal essence = parent == null ? ware.CalculatedESS : 0;
        system["essence"] = _target == Target.Version126 ? essence / gradeFactor : essence;
        system["essenceActual"] = essence;
        system["capacity"] = Capacity(ware.CalculatedCapacity);
        if (parent != null) Note(item, "Installed inside implant " + parent + ". Containment/capacity is recorded in export metadata; Foundry lists this component separately.");
        foreach (var child in ware.Children) AddImplant(child, Id(ware));
        foreach (var gear in ware.Gear) AddGear(gear, Id(ware));
    }
    private void AddSpell(Spell spell)
    {
        var raw = Snapshot(spell.Save); var item = Add(spell, raw, raw, "Spell", spell.DisplayNameShort);
        var system = (JsonObject)item["system"]!;
        system["category"] = spell.Category.ToUpperInvariant(); system["type"] = spell.Type == "M" ? "MANA" : "PHYSICAL";
        system["combatType"] = spell.Descriptors.Contains("Indirect", StringComparison.OrdinalIgnoreCase) ? "INDIRECT" : "DIRECT";
        system["range"] = spell.Range.StartsWith('T') ? "TOUCH" : spell.Range == "Self" ? "SELF" : "LOS";
        system["area"] = spell.Range.Contains("(A)", StringComparison.OrdinalIgnoreCase);
        system["duration"] = spell.Duration switch { "I" => "INSTANT", "S" => "SUSTAINED", _ => "PERMANENT" };
        var drain = Regex.Match(spell.DV, @"([+-]\s*\d+)\s*$");
        system["dv"] = drain.Success ? Integer(drain.Groups[1].Value.Replace(" ", "", StringComparison.Ordinal)) : 0;
        system["damageType"] = spell.Damage == "S" ? "STUN" : "PHYSICAL";
        Note(item, "Original drain: " + spell.DV + ". Check special spell rules, elemental effects and conditional drain modifiers in Foundry.");
    }
    private static string AttackSkill(string category) => category switch {
        "Blades" => "BLADES", "Clubs" => "CLUBS", "Unarmed" => "UNARMED", "Exotic Melee Weapons" => "EXOTIC_MELEE", "Exotic Ranged Weapons" => "EXOTIC_RANGED",
        "Bows" or "Crossbows" => "ARCHERY", "Throwing Weapons" => "THROWING", "Tasers" or "Holdouts" or "Light Pistols" or "Heavy Pistols" => "PISTOLS",
        "Machine Pistols" or "Submachine Guns" or "Assault Rifles" => "AUTOMATICS", "Battle Rifles" or "Sports Rifles" or "Sniper Rifles" or "Shotguns" => "LONGARMS", _ => "HEAVY_WEAPONS" };
}
