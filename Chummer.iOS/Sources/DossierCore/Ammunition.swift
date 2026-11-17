import Foundation

public enum AmmunitionError: LocalizedError {
    case invalidSelection, insufficientRounds
    public var errorDescription: String? {
        switch self {
        case .invalidSelection: return "That ammunition or weapon is no longer available."
        case .insufficientRounds: return "There aren't enough rounds for that shot."
        }
    }
}

extension DossierCharacter {
    public func ammunition(forWeapon weapon: SheetRow) -> [SheetGear] {
        guard weapon["type"].lowercased() == "ranged", weapon["requireammo"].lowercased() != "false", weapon["ammo"] != "0" else { return [] }
        let category = (weapon["ammocategory"].isEmpty ? weapon["category"].replacingOccurrences(of: "Cyberware", with: "").trimmingCharacters(in: .whitespaces) : weapon["ammocategory"])
        return gear.flatMap { [$0] + $0.descendants }.filter {
            $0["category"] == "Ammunition" && !$0["extra"].isEmpty
                && $0["extra"].caseInsensitiveCompare(category) == .orderedSame
        }
    }

    /// Quick round totals, not an action-economy or attack-legality engine.
    public func suggestedFireCounts(for weapon: SheetRow) -> [Int] {
        let modes = Set(weapon["mode"].uppercased().components(separatedBy: CharacterSet.alphanumerics.inverted).filter { !$0.isEmpty })
        var counts: Set<Int> = []
        if modes.contains("SS") { counts.insert(1) }
        if modes.contains("SA") { counts.formUnion([1, 2]) }
        if modes.contains("BF") { counts.formUnion([3, 6]) }
        if modes.contains("FA") { counts.formUnion([3, 6, Int(weapon["fullburst"]) ?? 10, Int(weapon["suppressive"]) ?? 20]) }
        return counts.filter { $0 > 0 }.sorted()
    }

    public mutating func spendAmmunition(weaponIndex: Int, path: [Int], rounds: Int) throws {
        guard rounds > 0, let weapon = rows(collection: "weapons", item: "weapon").first(where: { $0.id == weaponIndex }),
              let ammo = ammunition(forWeapon: weapon).first(where: { $0.path == path }),
              let quantity = Decimal(string: ammo["qty"], locale: Locale(identifier: "en_US_POSIX")) else { throw AmmunitionError.invalidSelection }
        guard quantity >= Decimal(rounds) else { throw AmmunitionError.insufficientRounds }
        let remaining = NSDecimalNumber(decimal: quantity - Decimal(rounds)).stringValue
        setGearValue(path: path, field: "qty", value: remaining)
    }

    /// Desktop Chummer removes rounds from reserves when reloading. Firing a loaded
    /// magazine therefore changes only that magazine, never subtracts reserves again.
    public mutating func spendLoadedAmmunition(weaponIndex: Int, slot: Int, rounds: Int) throws {
        guard (1...4).contains(slot), rounds > 0,
              let weapon = rows(collection: "weapons", item: "weapon").first(where: { $0.id == weaponIndex }),
              weapon["type"].lowercased() == "ranged" else { throw AmmunitionError.invalidSelection }
        let key = "ammoremaining" + (slot == 1 ? "" : String(slot))
        guard let remaining = Int(weapon[key]), remaining >= rounds else { throw AmmunitionError.insufficientRounds }
        var next = self
        next.setRowValue(collection: "weapons", item: "weapon", index: weaponIndex, field: key, value: String(remaining - rounds))
        try next.setWeaponAdjustment(weapon, field: "ammoSlot", value: String(slot))
        self = next
    }
}

extension DossierCharacter {
    /// Exchange rounds between an existing reserve and magazine atomically. Loading
    /// sets the desired magazine count; any old rounds return to their original lot.
    /// The count is a manual sheet entry, not a catalog-capacity or action-cost rule.
    public mutating func reloadAmmunition(weaponIndex: Int, path: [Int], slot: Int, rounds: Int) throws {
        guard (1...4).contains(slot), (1...Int(Int32.max)).contains(rounds),
              let weapon = rows(collection: "weapons", item: "weapon").first(where: { $0.id == weaponIndex }),
              let reserve = ammunition(forWeapon: weapon).first(where: { $0.path == path }),
              UUID(uuidString: reserve["guid"]) != nil, reserve["guid"] != "00000000-0000-0000-0000-000000000000" else { throw AmmunitionError.invalidSelection }
        var next = self
        try next.unloadAmmunition(weaponIndex: weaponIndex, slot: slot)
        guard let available = next.gear(at: path), let quantity = Decimal(string: available["qty"], locale: Locale(identifier: "en_US_POSIX")) else {
            throw AmmunitionError.invalidSelection
        }
        guard quantity >= Decimal(rounds) else { throw AmmunitionError.insufficientRounds }
        next.setGearValue(path: path, field: "qty", value: NSDecimalNumber(decimal: quantity - Decimal(rounds)).stringValue)
        let suffix = slot == 1 ? "" : String(slot)
        next.setRowValue(collection: "weapons", item: "weapon", index: weaponIndex, field: "ammoloaded" + suffix, value: reserve["guid"])
        next.setRowValue(collection: "weapons", item: "weapon", index: weaponIndex, field: "ammoremaining" + suffix, value: String(rounds))
        try next.setWeaponAdjustment(weapon, field: "ammoSlot", value: String(slot))
        self = next
    }
    public mutating func unloadAmmunition(weaponIndex: Int, slot: Int) throws {
        guard (1...4).contains(slot),
              let weapon = rows(collection: "weapons", item: "weapon").first(where: { $0.id == weaponIndex }),
              weapon["type"].lowercased() == "ranged" else { throw AmmunitionError.invalidSelection }
        let suffix = slot == 1 ? "" : String(slot)
        let remaining = Int(weapon["ammoremaining" + suffix]) ?? 0
        if remaining > 0 {
            guard let original = loadedAmmunition(for: weapon, slot: slot),
                  let quantity = Decimal(string: original["qty"], locale: Locale(identifier: "en_US_POSIX")) else {
                throw AmmunitionError.invalidSelection
            }
            setGearValue(path: original.path, field: "qty", value: NSDecimalNumber(decimal: quantity + Decimal(remaining)).stringValue)
        }
        setRowValue(collection: "weapons", item: "weapon", index: weaponIndex, field: "ammoremaining" + suffix, value: "0")
        setRowValue(collection: "weapons", item: "weapon", index: weaponIndex, field: "ammoloaded" + suffix, value: "00000000-0000-0000-0000-000000000000")
    }
}
