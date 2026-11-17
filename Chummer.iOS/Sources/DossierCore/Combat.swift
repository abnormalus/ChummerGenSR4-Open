import Foundation

extension DossierCharacter {
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
    /// Ammo, limb-specific strength and optional house-rule adjustments remain desktop rules.
    public func weaponDamage(_ weapon: SheetRow) -> String {
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
        return String(Int(ceil(result)) + bonus) + type + raw[split.upperBound...]
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

