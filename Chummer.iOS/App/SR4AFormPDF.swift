import UIKit
import PDFKit
import DossierCore

/// Coordinates are in points from the top-left of the embedded 612 x 792 form.
/// The original pages are painted unchanged before adding character values.
struct SR4AFormPDF {
    static func data(for character: DossierCharacter) throws -> Data {
        guard let url = Bundle.main.url(forResource: "SR4A-Character-Sheet", withExtension: "pdf"),
              let template = PDFDocument(url: url), template.pageCount == 2 else { throw CocoaError(.fileReadCorruptFile) }
        let snapshot = character.printableSheet
        var extraFields: [[String]] = []
        var selectedWeapons = Set<Int>()
        let weapons = character.rows(collection: "weapons", item: "weapon")
        let ranged = weapons.filter { $0["type"].lowercased() == "ranged" }
        let melee = weapons.filter { $0["type"].lowercased() != "ranged" }
        let rawSkills = character.rows(collection: "skills", item: "skill")
        func native(_ skill: SheetSkill) -> Bool {
            let raw = rawSkills.first { $0.id == skill.id }
            return skill.rating == 0 && raw?["skillcategory"] == "Language" && raw?["knowledge"].lowercased() == "true"
        }
        let skills = character.skills.filter { $0.rating > 0 || $0.pool > 0 || native($0) }.sorted {
            let first = $0.rating > 0 || native($0), second = $1.rating > 0 || native($1)
            if first != second { return first }
            return $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending
        }
        func ammoLabel(_ weapon: SheetRow) -> String {
            let grenades = character.grenades(forWeapon: weapon)
            if !grenades.isEmpty { return "Qty " + NSDecimalNumber(decimal: grenades.reduce(Decimal.zero) { $0 + (Decimal(string: $1["qty"]) ?? 0) }).stringValue }
            let slot = character.activeAmmoSlot(for: weapon)
            let remaining = weapon["ammoremaining" + (slot == 1 ? "" : String(slot))]
            if Int(remaining) != nil, weapon["ammo"] != "0", weapon["requireammo"].lowercased() != "false" { return remaining + "/" + weapon["ammo"] }
            return weapon["ammo"]
        }
        let renderer = UIGraphicsPDFRenderer(bounds: CGRect(x: 0, y: 0, width: 612, height: 792))
        let firstPages = renderer.pdfData { context in
            func put(_ rawValue: String, _ x: CGFloat, _ y: CGFloat, _ width: CGFloat, label: String, size: CGFloat = 8, forceFit: Bool = false) {
                let value = forceFit ? rawValue.components(separatedBy: .whitespacesAndNewlines).filter { !$0.isEmpty }.joined(separator: " ") : rawValue
                guard !value.isEmpty else { return }
                var font = UIFont.systemFont(ofSize: size)
                while (value as NSString).size(withAttributes: [.font: font]).width > width && font.pointSize > (forceFit ? 4 : 6) {
                    font = UIFont.systemFont(ofSize: font.pointSize - 0.25)
                }
                var shown = value
                var scale = min(1, width / max(1, (value as NSString).size(withAttributes: [.font: font]).width))
                if !forceFit && (value.contains("\n") || scale < 0.7) {
                    let marker = "*" + String(extraFields.count + 1)
                    extraFields.append([marker + " " + label, value]); shown = marker
                    font = UIFont.systemFont(ofSize: 6); scale = 1
                }
                context.cgContext.saveGState()
                context.cgContext.translateBy(x: x, y: y); context.cgContext.scaleBy(x: scale, y: 1)
                (shown as NSString).draw(at: .zero, withAttributes: [.font: font, .foregroundColor: UIColor.black])
                context.cgContext.restoreGState()
            }
            func begin(_ page: Int) {
                context.beginPage()
                guard let original = template.page(at: page)?.pageRef else { return }
                context.cgContext.saveGState()
                context.cgContext.translateBy(x: 0, y: 792); context.cgContext.scaleBy(x: 1, y: -1)
                context.cgContext.drawPDFPage(original)
                context.cgContext.restoreGState()
                ("* Expanded entries and additional details follow on continuation pages." as NSString).draw(at: CGPoint(x: 57, y: 764), withAttributes: [.font: UIFont.systemFont(ofSize: 6), .foregroundColor: UIColor.darkGray])
            }
            func table(_ rows: [[String]], x: [CGFloat], y: CGFloat, widths: [CGFloat], limit: Int, title: String) {
                for (i, row) in rows.prefix(limit).enumerated() {
                    for j in row.indices where j < x.count {
                        put(row[j], x[j], y + CGFloat(i) * 14, widths[j], label: "\(title): \(row.first ?? "") - column \(j + 1)", size: 7)
                    }
                }
            }
            func section(_ title: String) -> [[String]] { snapshot.sections.first { $0.title == title }?.rows ?? [] }
            begin(0)
            let name = [character.value("name"), character.value("alias")].filter { !$0.isEmpty }.reduce(into: [String]()) { if !$0.contains($1) { $0.append($1) } }.joined(separator: " / ")
            put(name, 124, 78, 191, label: "Name / Alias")
            put(character.value("metatype"), 87, 94, 72, label: "Metatype")
            put(character.dossierField("ethnicity"), 199, 94, 116, label: "Ethnicity")
            for (key, x, width) in [("age", 63.0, 37.0), ("sex", 123, 50), ("height", 204, 31), ("weight", 266, 49), ("totalkarma", 91, 23), ("karma", 164, 27), ("streetcred", 235, 24), ("notoriety", 297, 18)] {
                put(character.value(key), x, ["age", "sex", "height", "weight"].contains(key) ? 110 : 126, width, label: key)
            }
            for (i, key) in ["BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL"].enumerated() {
                let attribute = character.attributes.first { $0.id == key }
                let value = attribute.map { $0.base == $0.total ? "\($0.total)" : "\($0.base) (\($0.total))" } ?? "0"
                put(value, 104, 195 + CGFloat(i) * 14, 39, label: key)
            }
            let right = [String(character.total("EDG")), String(max(0, character.total("EDG") - (Int(character.value("edgeused")) ?? 0))), character.essence,
                         character.isTechnomancer ? String(character.total("RES")) : character.isMagician || character.isAdept ? String(character.total("MAG")) : "-",
                         String(character.initiative), String(character.initiativePasses), character.matrixInitiative.map { "\($0) / \(character.matrixInitiativePasses) IP" } ?? "-", character.isMagician ? String(character.astralInitiative) : "-"]
            for (i, value) in right.enumerated() { put(value, 227, 195 + CGFloat(i) * 14, 38, label: "Attribute / Initiative \(i + 1)") }
            let armor = character.rows(collection: "armors", item: "armor").first
            put(armor?["name"] ?? "", 416, 78, 86, label: "Primary armor")
            put(armor?["b"] ?? "", 514, 74, 19, label: "Ballistic armor")
            put(armor?["i"] ?? "", 552, 74, 18, label: "Impact armor")
            if let weapon = ranged.first {
                put(weapon["name"], 447, 94, 122, label: "Primary ranged weapon")
                for (value, x, width) in [(character.weaponDamage(weapon), 384.0, 25.0), (character.weaponAP(weapon), 427, 17), (weapon["mode"], 466, 21), (weapon["rc"], 501, 17), (ammoLabel(weapon), 548, 22)] {
                    put(value, x, 109, width, label: "Primary ranged weapon details", size: 7)
                }
            }
            if let weapon = character.primaryMeleeWeapon {
                put(weapon["name"], 447, 126, 122, label: "Primary melee weapon")
                put(weapon["reach"], 389, 141, 48, label: "Reach"); put(character.weaponDamage(weapon), 468, 141, 47, label: "Melee DV"); put(character.weaponAP(weapon), 545, 141, 23, label: "Melee AP")
            }
            for monitor in snapshot.monitors {
                let physical = monitor.title == "Physical", count = physical ? 18 : 12
                let originX: CGFloat = physical ? 365.5 : 481.2
                for index in 0..<count {
                    let rect = CGRect(x: originX + CGFloat(index % 3) * 31.4 + 1, y: 221.8 + CGFloat(index / 3) * 30.2 + 1, width: 29.4, height: 28.2)
                    if index >= max(monitor.capacity, monitor.damage) {
                        context.cgContext.setFillColor(UIColor(white: 0.22, alpha: 1).cgColor); context.cgContext.fill(rect)
                    } else if index < monitor.damage {
                        ("X" as NSString).draw(at: CGPoint(x: rect.minX + 7, y: rect.minY + 3), withAttributes: [.font: UIFont.boldSystemFont(ofSize: 19), .foregroundColor: UIColor.black])
                    }
                }
                if max(monitor.capacity, monitor.damage) > count { extraFields.append([monitor.title + " monitor", "Damage \(monitor.damage) / \(monitor.capacity) boxes"]) }
            }
            for (i, skill) in skills.prefix(34).enumerated() {
                let raw = rawSkills.first { $0.id == skill.id }
                let spec = raw?["spec"] ?? ""
                let label = skill.name + (spec.isEmpty ? "" : " (" + spec + ")") + (native(skill) ? " (Native)" : "")
                let x: CGFloat = i < 17 ? 51.4 : 188.7, y = 389 + CGFloat(i % 17) * 14
                put(label, x, y, 60, label: "Skill", size: 7)
                put(native(skill) ? "N" : String(skill.rating), x + 63, y, 13, label: "Skill rating", size: 7)
                put(String(character.total(skill.attribute)), x + 79, y, 12, label: "Skill attribute", size: 7)
                put(native(skill) ? "N" : String(skill.pool), x + 94, y, 15, label: "Skill pool", size: 7)
                let knowledge = raw?["knowledge"].lowercased() == "true"
                context.cgContext.setStrokeColor(UIColor.black.cgColor)
                context.cgContext.strokeEllipse(in: CGRect(x: x + (knowledge ? 121 : 111), y: y - 1, width: 8, height: 11))
            }
            let qualities = character.rows(collection: "qualities", item: "quality")
            table(qualities.map { [$0["name"], [$0["extra"], $0["notes"]].filter { !$0.isEmpty }.joined(separator: "; ")] }, x: [361.5, 469.5], y: 492, widths: [104, 86], limit: 9, title: "Quality")
            for (i, quality) in qualities.prefix(9).enumerated() {
                context.cgContext.strokeEllipse(in: CGRect(x: quality["qualitytype"].lowercased().contains("negative") ? 568 : 558, y: 492 + CGFloat(i) * 14 - 1, width: 8, height: 11))
            }
            let lifestyle = character.rows(collection: "lifestyles", item: "lifestyle").first
            put(lifestyle?["name"] ?? lifestyle?["baselifestyle"] ?? "", 117, 676, 449, label: "Primary lifestyle")
            put(character.value("nuyen"), 77, 692, 123, label: "Nuyen")
            let idGear = character.gear.flatMap { [$0] + $0.descendants }.filter { $0["category"] == "ID/Credsticks" }
            func identityName(_ gear: SheetGear) -> String { [gear["name"], gear["extra"], (Int(gear["rating"]) ?? 0) > 0 ? "Rating " + gear["rating"] : ""].filter { !$0.isEmpty }.joined(separator: " ") }
            let licenses = idGear.filter { $0["name"].contains("License") }.map(identityName).joined(separator: "; ")
            put(licenses, 248, 692, 317, label: "Licenses", size: 7)
            let identities = ([character.dossierField("identities")] + idGear.filter { $0["name"].contains("SIN") }.map(identityName)).filter { !$0.isEmpty }.joined(separator: "; ")
            if !identities.isEmpty { put(identities, 48, 725, 518, label: "IDs / Licenses") }

            begin(1)
            table(ranged.map { [ $0["name"], character.weaponDamage($0), character.weaponAP($0), $0["mode"], $0["rc"], ammoLabel($0) ] }, x: [57.4, 122.7, 162.5, 200.4, 252.6, 290.9], y: 89, widths: [62, 37, 35, 49, 35, 28], limit: 6, title: "Ranged weapon")
            table(melee.map { [$0["name"], character.weaponDamage($0), character.weaponAP($0), $0["reach"]] }, x: [57.4, 159.7, 224.5, 291.7], y: 230, widths: [99, 61, 64, 27], limit: 7, title: "Melee weapon")
            selectedWeapons.formUnion(ranged.prefix(6).map(\.id)); selectedWeapons.formUnion(melee.prefix(7).map(\.id))
            table(section("Augmentations").map { row in
                let parts = row[1].components(separatedBy: " · ")
                let rating = parts.first { $0.hasPrefix("Rating ") }.map { String($0.dropFirst(7)) } ?? ""
                let essence = parts.first { $0.hasPrefix("Essence ") }.map { String($0.dropFirst(8)) } ?? ""
                return [row[0], rating, parts.filter { !$0.hasPrefix("Rating ") && !$0.hasPrefix("Essence ") }.joined(separator: "; "), essence]
            }, x: [57.4, 156, 193, 289], y: 382, widths: [96, 34, 92, 30], limit: 8, title: "Augmentation")
            let spells = character.rows(collection: "spells", item: "spell")
            if character.isMagician { table(spells.map { [$0["name"], $0["type"], $0["range"], $0["duration"], $0["dv"]] }, x: [57.1, 131.7, 174.1, 224.6, 278.3], y: 553, widths: [71, 39, 47, 50, 40], limit: 6, title: "Spell") }
            if character.isMagician || character.isTechnomancer {
                table(character.rows(collection: "spirits", item: "spirit").map { [$0["name"], $0["force"], $0["services"], $0["bound"].lowercased() == "true" ? (character.isTechnomancer ? "Registered" : "Bound") : (character.isTechnomancer ? "Unregistered" : "Unbound")] }, x: [57.6, 130.9, 172, 232], y: 688, widths: [70, 37, 56, 87], limit: 5, title: "Spirit / Sprite")
            }
            table(character.rows(collection: "armors", item: "armor").map { [$0["name"], $0["b"] + " / " + $0["i"], $0["notes"]] }, x: [365.6, 437.6, 537.2], y: 90, widths: [69, 96, 43], limit: 5, title: "Armor")
            if let link = character.gear.last(where: { $0["category"] == "Commlink" && $0["active"].lowercased() == "true" }) ?? character.gear.first(where: { $0["category"] == "Commlink" }) {
                let os = link.children.first { $0["category"] == "Commlink Operating System" }
                put(link["name"], 408, 204, 72, label: "Commlink"); put(os?["name"] ?? "", 499, 204, 80, label: "Commlink OS")
                put(character.matrixResponse.map(String.init) ?? link["response"], 409, 218, 57, label: "Response")
                put(os?["system"] ?? link["system"], 505, 218, 74, label: "System")
                put(String(([Int(link["signal"]) ?? 0] + link.children.filter { $0["category"] == "Commlink Upgrade" }.map { Int($0["signal"]) ?? 0 }).max() ?? 0), 392, 232, 73, label: "Signal"); put(os?["firewall"] ?? link["firewall"], 507, 232, 72, label: "Firewall")
            }
            let programNames = character.programs.map { $0["name"] + " " + $0["rating"] + ($0["equipped"].lowercased() == "true" ? " [L]" : "") }.joined(separator: "; ")
            put(programNames, 409, 246, 170, label: "Programs")
            if let vehicle = character.rows(collection: "vehicles", item: "vehicle").first {
                for (key, x, y, width) in [("name", 401.0, 350.0, 62.0), ("handling", 507, 350, 72), ("accel", 394, 364, 78), ("speed", 504, 364, 75), ("pilot", 387, 378, 84), ("body", 503, 378, 76), ("armor", 397, 392, 74), ("sensor", 507, 392, 72)] { put(vehicle[key], x, y, width, label: "Vehicle " + key, forceFit: key == "name") }
                put("Damage \(character.vehicleDamage(vehicle))/\(character.vehicleCapacity(vehicle)) " + vehicle["notes"], 393, 406, 186, label: "Vehicle notes")
            }
            table(character.rows(collection: "contacts", item: "contact").map { [$0["name"], $0["loyalty"], $0["connection"]] }, x: [365.6, 490.3, 530.6], y: 488, widths: [121, 37, 49], limit: 9, title: "Contact")
            var abilities: [[String]] = []
            if character.isAdept { abilities += character.rows(collection: "powers", item: "power").map { [$0["name"] + ($0["extra"].isEmpty ? "" : " (" + $0["extra"] + ")"), $0["levels"].lowercased() == "true" ? $0["rating"] : ""] } }
            if character.isTechnomancer { abilities += character.rows(collection: "complexforms", item: "complexform").map { [$0["name"], $0["rating"]] } }
            table(abilities, x: [365.6, 544.7], y: 669, widths: [176, 35], limit: 6, title: "Power / Form")
        }
        guard let output = PDFDocument(data: firstPages) else { throw CocoaError(.fileWriteUnknown) }
        var continuation: [PrintableSheet.Section] = []
        let limits = ["Qualities": 9, "Armor": 5, "Augmentations": 8, "Spells": 6, "Spirits": 5, "Sprites": 5, "Adept powers": 6, "Complex forms": 6, "Vehicles / Drones": 1]
        var references: [[String]] = []
        for section in snapshot.sections {
            if section.title == "Skills" {
                references += section.rows.flatMap { row in [Array(row.prefix(4)), Array(row.suffix(4))].filter { $0.count == 4 && !$0[3].isEmpty }.map { ["Skills", $0[0], $0[3]] } }
            } else if let sourceIndex = section.columns.firstIndex(of: "Source") {
                references += section.rows.compactMap { row in
                    guard row.indices.contains(sourceIndex), !row[sourceIndex].isEmpty else { return nil }
                    return [section.title, row.first ?? "", row[sourceIndex]]
                }
            }
            if ["Personal data", "Attributes", "Initiative", "Condition monitor", "Skills"].contains(section.title) { continue }
            let rows: [[String]]
            if section.title == "Weapons" { rows = section.rows.enumerated().filter { !selectedWeapons.contains($0.offset) }.map(\.element) }
            else if section.title == "Contacts" { rows = section.rows.enumerated().filter { $0.offset >= 9 || !($0.element.last ?? "").isEmpty }.map(\.element) }
            else if let limit = limits[section.title] { rows = Array(section.rows.dropFirst(limit)) }
            else { rows = section.rows }
            if !rows.isEmpty { continuation.append(.init(title: section.title, columns: section.columns, weights: section.weights, rows: rows)) }
        }
        if skills.count > 34 {
            continuation.insert(.init(title: "Additional skills", columns: ["Skill", "Rating", "Attribute", "Pool"], weights: [5, 1, 1, 1], rows: skills.dropFirst(34).map { skill in
                let spec = rawSkills.first { $0.id == skill.id }?["spec"] ?? ""
                return [skill.name + (spec.isEmpty ? "" : " (" + spec + ")") + (native(skill) ? " (Native)" : ""), native(skill) ? "N" : String(skill.rating), skill.attribute, native(skill) ? "N" : String(skill.pool)]
            }), at: 0)
        }
        if let weaponRows = snapshot.sections.first(where: { $0.title == "Weapons" })?.rows {
            let ammo = weaponRows.filter { !$0[4].isEmpty }.map { [$0[0], $0[4]] }
            if !ammo.isEmpty { continuation.append(.init(title: "Ammunition details", columns: ["Weapon", "Ammunition"], weights: [2, 5], rows: ammo)) }
        }
        if !extraFields.isEmpty { continuation.insert(.init(title: "Expanded form entries", columns: ["Field", "Full value"], weights: [2, 5], rows: extraFields), at: 0) }
        if !references.isEmpty { continuation.append(.init(title: "Source references", columns: ["Section", "Entry", "Source"], weights: [2, 4, 1.5], rows: references)) }
        if character.woundThreshold != 3 || character.woundOffset != 0 || character.ignoresWounds(physical: true) || character.ignoresWounds(physical: false) {
            if let condition = snapshot.sections.first(where: { $0.title == "Condition monitor" }) { continuation.insert(condition, at: 0) }
        }
        if !continuation.isEmpty, let appendix = PDFDocument(data: CharacterSheetPDF.data(for: PrintableSheet(title: character.title + " - Continuation", sections: continuation), startingPage: 3)) {
            for index in 0..<appendix.pageCount { if let page = appendix.page(at: index) { output.insert(page, at: output.pageCount) } }
        }
        guard let bytes = output.dataRepresentation() else { throw CocoaError(.fileWriteUnknown) }
        return bytes
    }
}
