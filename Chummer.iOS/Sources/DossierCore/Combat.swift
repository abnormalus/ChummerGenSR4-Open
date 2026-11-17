import Foundation

extension DossierCharacter {
    public var woundThreshold: Int { max(1, 3 + savedImprovement("CMThreshold")) }
    public var woundOffset: Int { savedImprovement("CMThresholdOffset") }
    public func ignoresWounds(physical: Bool) -> Bool {
        rows(collection: "improvements", item: "improvement").contains {
            $0["improvementttype"] == (physical ? "IgnoreCMPenaltyPhysical" : "IgnoreCMPenaltyStun") && $0["enabled"].lowercased() != "false"
        }
    }
    public var woundModifier: Int {
        [true, false].reduce(0) { result, physical in
            guard !ignoresWounds(physical: physical) else { return result }
            let marked = min(damage(physical), trackCapacity(physical))
            return result - max(0, marked - woundOffset) / woundThreshold
        }
    }
    /// Imported pool snapshots already include this saved desktop penalty. Replace
    /// its contribution with the current monitor penalty instead of adding twice.
    var importedWoundModifier: Int {
        rows(collection: "improvements", item: "improvement").filter {
            $0["improvementsource"] == "ConditionMonitor" && $0["enabled"].lowercased() != "false"
        }.reduce(0) { $0 + (Int($1["val"]) ?? 0) }
    }

    public var isMagician: Bool { value("magician").lowercased() == "true" }
    public var isAdept: Bool { value("adept").lowercased() == "true" }
    /// An optional manual total remains available for temporary table effects.
    public var initiativePasses: Int {
        Int(dossierField("initiativePasses")) ?? (1 + savedImprovement("InitiativePass") + savedImprovement("InitiativePassAdd"))
    }
    public func powerSummary(_ power: SheetRow) -> String {
        let extra = power["extra"].isEmpty ? "" : " (" + power["extra"] + ")"
        let rating = power["levels"].lowercased() == "true" ? " " + power["rating"] : ""
        return power["name"] + extra + rating
    }
    /// Vehicle uses the same persisted field and Body formula as desktop Vehicle.
    public func vehicleCapacity(_ vehicle: SheetRow) -> Int {
        8 + Int(ceil(Double(Int(vehicle["body"]) ?? 0) / 2))
    }
    public func vehicleDamage(_ vehicle: SheetRow) -> Int { max(0, Int(vehicle["physicalcmfilled"]) ?? 0) }
    public mutating func setVehicleDamage(_ count: Int, index: Int) {
        setRowValue(collection: "vehicles", item: "vehicle", index: index, field: "physicalcmfilled", value: String(max(0, count)))
    }

    /// Resolve the base expression, retaining suffixes and the original saved formula.
    /// Unsupported expressions stay visible rather than displaying a guessed number.
    /// Saved loaded-ammo and installed-modifier bonuses are included. Limb-specific
    /// strength and optional house rules can be entered as explicit sheet overrides.
    public func weaponDamage(_ weapon: SheetRow) -> String {
        if !weaponAdjustment(weapon, field: "damage").isEmpty { return weaponAdjustment(weapon, field: "damage") }
        let raw = weapon["damage"]
        let suffixes = ["P or S", "P", "S"]
        guard let suffix = suffixes.first(where: { raw.range(of: $0, options: .backwards) != nil }),
              let split = raw.range(of: suffix, options: .backwards) else { return raw }
        var expression = String(raw[..<split.lowerBound])
        for attribute in ["STR", "AGI", "BOD", "REA", "INT", "LOG", "WIL", "CHA", "MAG", "RES"] {
            var score = total(attribute)
            if attribute == "STR" && (weapon["category"] == "Throwing Weapons" || weapon["useskill"] == "Throwing Weapons") {
                score += savedImprovement("ThrowSTR")
            }
            expression = expression.replacingOccurrences(of: attribute, with: String(score))
        }
        var parser = DamageExpression(expression)
        guard let result = parser.evaluate(), result.isFinite, abs(result) < 1_000_000 else { return raw }
        // Desktop CalculatedDamage rounds the evaluated DV up.
        var type = suffix
        var bonus = 0
        let improvements = rows(collection: "improvements", item: "improvement").filter { $0["enabled"].lowercased() != "false" }
        for improvement in improvements {
            if improvement["improvementttype"] == "WeaponCategoryDV" {
                let name = improvement["improvedname"]
                if name == weapon["category"] || "Cyberware " + name == weapon["category"] { bonus += Int(improvement["val"]) ?? 0 }
                if !weapon["useskill"].isEmpty && name == weapon["useskill"] { bonus += Int(improvement["val"]) ?? 0 }
            }
            if weapon["name"] == "Unarmed Attack" {
                if improvement["improvementttype"] == "UnarmedDV" { bonus += Int(improvement["val"]) ?? 0 }
                if improvement["improvementttype"] == "UnarmedDVPhysical" { type = "P" }
            }
        }
        bonus += installedWeaponMods(weapon).reduce(0) { $0 + (Int($1.value("dvbonus")) ?? 0) }
        var extra = String(raw[split.upperBound...])
        var replacement: String?
        for adjustment in loadedWeaponBonuses(weapon) {
            if let damageType = adjustment.child("damagetype") { type = ""; extra = damageType.text }
            bonus += Int(adjustment.value("damage")) ?? 0
            if let replace = adjustment.child("damagereplace") { replacement = replace.text }
        }
        if let replacement {
            // Replacement DV ignores base/mod DV, as in desktop CalculatedDamage.
            return evaluatedReplacementDamage(replacement) ?? raw
        }
        let multiplier = weapon["name"] == "Unarmed Attack (Smashing Blow)" ? 2 : 1
        return String((Int(ceil(result)) + bonus) * multiplier) + type + extra
    }
}

/// A bounded arithmetic grammar, not a script evaluator. All input must be consumed.
private struct DamageExpression {
    var input: [Character]
    var index = 0
    init(_ text: String) { input = Array(text.filter { !$0.isWhitespace }) }
    mutating func evaluate() -> Double? {
        guard input.count <= 256, let value = expression(), index == input.count else { return nil }
        return value
    }
    mutating func take(_ c: Character) -> Bool {
        guard index < input.count, input[index] == c else { return false }
        index += 1; return true
    }
    mutating func expression() -> Double? {
        guard var value = term() else { return nil }
        while index < input.count {
            if take("+") { guard let rhs = term() else { return nil }; value += rhs }
            else if take("-") { guard let rhs = term() else { return nil }; value -= rhs }
            else { break }
        }
        return value
    }
    mutating func term() -> Double? {
        guard var value = factor() else { return nil }
        while index < input.count {
            if take("*") { guard let rhs = factor() else { return nil }; value *= rhs }
            else if take("/") { guard let rhs = factor(), rhs != 0 else { return nil }; value /= rhs }
            else { break }
        }
        return value
    }
    mutating func factor() -> Double? {
        if take("-") { return factor().map { -$0 } }
        if take("+") { return factor() }
        if take("(") { guard let value = expression(), take(")") else { return nil }; return value }
        if index + 3 <= input.count && String(input[index..<index+3]) == "min" {
            index += 3
            guard take("("), let lhs = expression(), take(","), let rhs = expression(), take(")") else { return nil }
            return min(lhs, rhs)
        }
        let start = index
        while index < input.count && (input[index].isNumber || input[index] == ".") { index += 1 }
        return start == index ? nil : Double(String(input[start..<index]))
    }
}


extension DossierCharacter {
    /// Manual table adjustments use supported notes and never change catalog base stats.
    private func weaponAdjustmentKey(_ weapon: SheetRow, field: String) -> String {
        let identity = weapon["guid"].isEmpty ? "row-\(weapon.id)" : weapon["guid"]
        return "weapon." + identity + "." + field
    }
    public func weaponAdjustment(_ weapon: SheetRow, field: String) -> String {
        dossierField(weaponAdjustmentKey(weapon, field: field))
    }
    public mutating func setWeaponAdjustment(_ weapon: SheetRow, field: String, value: String) throws {
        try setDossierField(weaponAdjustmentKey(weapon, field: field), value: value.trimmingCharacters(in: .whitespacesAndNewlines))
    }
    public func activeAmmoSlot(for weapon: SheetRow) -> Int {
        let slot = Int(weaponAdjustment(weapon, field: "ammoSlot")) ?? 1
        return (1...4).contains(slot) ? slot : 1
    }
    public func loadedAmmunition(for weapon: SheetRow, slot: Int? = nil) -> SheetGear? {
        let selected = slot ?? activeAmmoSlot(for: weapon)
        let key = "ammoloaded" + (selected == 1 ? "" : String(selected))
        let guid = weapon[key]
        guard !guid.isEmpty, guid != "00000000-0000-0000-0000-000000000000" else { return nil }
        return gear.flatMap { [$0] + $0.descendants }.first { $0["guid"].caseInsensitiveCompare(guid) == .orderedSame }
    }
    private func installedWeaponMods(_ weapon: SheetRow) -> [CharacterElement] {
        (equipmentElement(collection: "weapons", item: "weapon", index: weapon.id)?.child("weaponmods")?.children("weaponmod") ?? [])
            .filter { $0.value("installed").lowercased() == "true" }
    }
    private func loadedWeaponBonuses(_ weapon: SheetRow) -> [CharacterElement] {
        guard let ammo = loadedAmmunition(for: weapon), let node = gearElement(at: ammo.path) else { return [] }
        let child = node.child("children")?.children("gear").compactMap { $0.child("weaponbonus") }.first
        return [node.child("weaponbonus"), child].compactMap { $0 }.filter {
            !(weapon["damage"].contains("(f)") && $0.xml.contains("(f)"))
        }
    }
    private func evaluatedReplacementDamage(_ damage: String) -> String? {
        let tags = ["P or S", "P", "S"]
        guard let tag = tags.first(where: { damage.range(of: $0, options: .backwards) != nil }),
              let range = damage.range(of: tag, options: .backwards) else { return nil }
        var parser = DamageExpression(String(damage[..<range.lowerBound]))
        guard let number = parser.evaluate(), number.isFinite, abs(number) < 1_000_000 else { return nil }
        return String(Int(ceil(number))) + tag + damage[range.upperBound...]
    }
    public func weaponAP(_ weapon: SheetRow) -> String {
        if !weaponAdjustment(weapon, field: "ap").isEmpty { return weaponAdjustment(weapon, field: "ap") }
        let base = weapon["ap"]
        guard base == "-" || Int(base) != nil else { return base }
        var total = Int(base) ?? 0
        var replacement: String?
        // Desktop AP uses the loaded parent ammunition bonus; child damage plugins
        // are handled separately by its damage routine.
        if let ammo = loadedAmmunition(for: weapon), let bonus = gearElement(at: ammo.path)?.child("weaponbonus"),
           !(weapon["damage"].contains("(f)") && bonus.xml.contains("(f)")) {
            if weapon["damage"].contains("(f)") && ammo["name"].contains("Flechette") { total -= 5 }
            else {
                total += Int(bonus.value("ap")) ?? 0
                replacement = bonus.child("apreplace")?.text
            }
        }
        if weapon["name"] == "Unarmed Attack" || weapon["category"] == "Cyberware" {
            total += savedImprovement("UnarmedAP")
        }
        total += installedWeaponMods(weapon).reduce(0) { $0 + (Int($1.value("apbonus")) ?? 0) }
        return replacement ?? (total == 0 ? "-" : (total > 0 ? "+" : "") + String(total))
    }
}
