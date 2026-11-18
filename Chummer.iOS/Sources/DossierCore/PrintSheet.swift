import Foundation

/// Plain current-sheet values shared by printing and readable snapshot export.
/// No catalogs, sourcebook text, or private dossier metadata are embedded.
public struct PrintableSheet {
    public struct Section {
        public let title: String
        public let columns: [String]
        public let weights: [Double]
        public let rows: [[String]]
        public init(title: String, columns: [String], weights: [Double], rows: [[String]]) {
            self.title = title; self.columns = columns; self.weights = weights; self.rows = rows
        }
    }
    public struct Monitor {
        public let title: String
        public let damage: Int
        public let capacity: Int
        public let threshold: Int
        public let offset: Int
        public let ignored: Bool
    }
    public let title: String
    public let sections: [Section]
    public let monitors: [Monitor]
    public init(title: String, sections: [Section], monitors: [Monitor] = []) {
        self.title = title; self.sections = sections; self.monitors = monitors
    }
    public var text: String {
        ([title, "SR 4 Dossier - Character Sheet"] + sections.flatMap { section in
            ["", section.title.uppercased(), section.columns.joined(separator: " | ")] + section.rows.map { $0.joined(separator: " | ") }
        }).joined(separator: "\n")
    }
}

extension DossierCharacter {
    public var printableSheet: PrintableSheet {
        var sections: [PrintableSheet.Section] = []
        func table(_ title: String, _ columns: [String], _ weights: [Double], _ rows: [[String]]) {
            guard !rows.isEmpty else { return }
            sections.append(.init(title: title, columns: columns, weights: weights, rows: rows))
        }
        func citation(_ fields: [String: String]) -> String {
            let source = fields["source"] ?? "", page = fields["page"] ?? ""
            return source.isEmpty ? "" : source + (page.isEmpty ? "" : " p. " + page)
        }
        func summary(_ fields: [String: String], keys: [(String, String)]) -> String {
            keys.compactMap { key, label in
                guard let value = fields[key], !value.isEmpty else { return nil }
                return label.isEmpty ? value : label + " " + value
            }.joined(separator: " · ")
        }
        table("Personal data", ["Field", "Value", "Field", "Value"], [1, 2, 1, 2], [
            ["Name", value("name"), "Alias", value("alias")],
            ["Metatype", value("metatype"), "Ethnicity", dossierField("ethnicity")],
            ["Age / Sex", [value("age"), value("sex")].filter { !$0.isEmpty }.joined(separator: " / "), "Nuyen", value("nuyen")],
            ["Karma", value("karma"), "Total karma", value("totalkarma")],
            ["Street cred", value("streetcred"), "Notoriety", value("notoriety")]
        ])
        let attributeValues = attributes.filter { !["ESS", "INI"].contains($0.id) && ($0.id != "MAG" || isMagician || isAdept) && ($0.id != "RES" || isTechnomancer) }.map {
            [$0.id, "\($0.base) / \($0.total)"]
        } + [["ESS", essenceMaximum + " / " + essence]]
        table("Attributes", ["Attribute", "Base / Modified", "Attribute", "Base / Modified"], [1, 1, 1, 1], stride(from: 0, to: attributeValues.count, by: 2).map { index in
            attributeValues[index] + (index + 1 < attributeValues.count ? attributeValues[index + 1] : ["", ""])
        })
        var initiativeRows = [["Physical", String(initiative), String(initiativePasses)], ["Matrix", matrixInitiative.map(String.init) ?? "Manual", String(matrixInitiativePasses)]]
        if isMagician { initiativeRows.append(["Astral", String(astralInitiative), "3"]) }
        table("Initiative", ["Type", "Initiative", "Passes"], [3, 1, 1], initiativeRows)
        table("Condition monitor", ["Track", "Damage / Boxes", "Wound modifier"], [2, 2, 2], [
            ["Physical", "\(damage(true)) / \(trackCapacity(true))", ignoresWounds(physical: true) ? "Ignored" : "Every \(woundThreshold) boxes; offset \(woundOffset)"],
            ["Stun", "\(damage(false)) / \(trackCapacity(false))", ignoresWounds(physical: false) ? "Ignored" : "Every \(woundThreshold) boxes; offset \(woundOffset)"],
            ["Combined penalty", String(woundModifier), "Included in pools and initiative"]
        ])
        let skillRows = rows(collection: "skills", item: "skill")
        func isNativeLanguage(_ skill: SheetSkill) -> Bool {
            guard let row = skillRows.first(where: { $0.id == skill.id }) else { return false }
            return row["skillcategory"] == "Language" && row["knowledge"].lowercased() == "true" && skill.rating == 0
        }
        let printableSkills = skills.filter { $0.rating > 0 || $0.pool > 0 || isNativeLanguage($0) }.sorted { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }.map { skill in
            let original = skillRows.first { $0.id == skill.id }
            let specialization = original?["spec"] ?? ""
            return [skill.name + (isNativeLanguage(skill) ? " (Native)" : "") + (specialization.isEmpty ? "" : " (" + specialization + ")"), isNativeLanguage(skill) ? "-" : String(skill.rating), isNativeLanguage(skill) ? "-" : String(skill.pool), citation(original?.fields ?? [:])]
        }
        let half = (printableSkills.count + 1) / 2
        table("Skills", ["Skill / Specialization", "Rtg.", "Pool", "Source", "Skill / Specialization", "Rtg.", "Pool", "Source"], [3, 0.65, 0.65, 1.5, 3, 0.65, 0.65, 1.5], (0..<half).map { index in
            printableSkills[index] + (index + half < printableSkills.count ? printableSkills[index + half] : ["", "", "", ""])
        })
        let weapons = rows(collection: "weapons", item: "weapon")
        table("Weapons", ["Weapon", "Mode", "DV", "AP", "Ammo", "Source"], [3, 0.7, 0.8, 0.7, 2, 1.2], weapons.map { weapon in
            let grenades = grenades(forWeapon: weapon)
            let stock = grenades.isEmpty ? "" : "Quantity " + NSDecimalNumber(decimal: grenades.reduce(Decimal.zero) { $0 + (Decimal(string: $1["qty"]) ?? 0) }).stringValue
            let magazines = (1...4).compactMap { slot -> String? in
                let remaining = weapon["ammoremaining" + (slot == 1 ? "" : String(slot))]
                guard (Int(remaining) ?? 0) > 0 else { return nil }
                return "M\(slot): \(remaining) " + (loadedAmmunition(for: weapon, slot: slot)?["name"] ?? "rounds")
            }.joined(separator: "; ")
            return [weapon["name"], weapon["mode"], weaponDamage(weapon), weaponAP(weapon), [stock, magazines].filter { !$0.isEmpty }.joined(separator: "; "), citation(weapon.fields)]
        })
        for (title, collection, item, keys) in [
            ("Armor", "armors", "armor", [("b", "Ballistic"), ("i", "Impact"), ("notes", "")]),
            ("Qualities", "qualities", "quality", [("extra", ""), ("notes", "")]),
            ("Lifestyles", "lifestyles", "lifestyle", [("baselifestyle", ""), ("notes", "")])
        ] {
            table(title, ["Name", "Details", "Source"], [3, 4, 1.4], rows(collection: collection, item: item).map { row in
                [row["name"], summary(row.fields, keys: keys.filter { $0.0 != "rating" || (Int(row["rating"]) ?? 0) > 0 }), citation(row.fields)]
            })
        }
        var augmentations: [[String]] = []
        func collectAugmentation(_ element: CharacterElement, depth: Int) {
            var fields: [String: String] = [:]
            for case .element(let child) in element.content where !child.content.contains(where: { if case .element = $0 { return true }; return false }) {
                fields[child.name] = child.text
            }
            let keys: [(String, String)] = [("extra", ""), ("rating", "Rating"), ("ess", "Essence"), ("notes", "")]
            augmentations.append([String(repeating: "  ", count: depth) + element.value("name"), summary(fields, keys: keys.filter { $0.0 != "rating" || (Int(fields["rating"] ?? "") ?? 0) > 0 }), citation(fields)])
            for child in element.child("children")?.children("cyberware") ?? [] { collectAugmentation(child, depth: depth + 1) }
        }
        for row in rows(collection: "cyberwares", item: "cyberware") {
            if let element = equipmentElement(collection: "cyberwares", item: "cyberware", index: row.id) { collectAugmentation(element, depth: 0) }
        }
        table("Augmentations", ["Name", "Details", "Source"], [3, 4, 1.4], augmentations)
        // Keep programs separate from the equipment tree, as in the app.
        let grenadePaths = Set(weapons.flatMap { grenades(forWeapon: $0).map(\.path) })
        var equipment: [[String]] = []
        func collect(_ entry: SheetGear, depth: Int) {
            guard !entry.isProgram, entry["category"] != "Program Options", !grenadePaths.contains(entry.path) else { return }
            equipment.append([String(repeating: "  ", count: depth) + entry["name"], summary(entry.fields, keys: [("extra", ""), ("rating", "Rating"), ("qty", "Qty"), ("response", "Response"), ("signal", "Signal"), ("system", "System"), ("firewall", "Firewall"), ("notes", "")].filter { !["rating", "response", "signal", "system", "firewall"].contains($0.0) || (Int(entry[$0.0]) ?? 0) > 0 }), citation(entry.fields)])
            for child in entry.children { collect(child, depth: depth + 1) }
        }
        for entry in gear where !grenadePaths.contains(entry.path) { collect(entry, depth: 0) }
        table("Equipment / Commlinks", ["Name", "Details", "Source"], [3, 4, 1.4], equipment)
        table("Programs", ["Program / Options", "Rating", "Loaded", "Source"], [5, 1, 1, 1.4], programs.map { program in
            let options = program.children.filter { $0["category"] == "Program Options" }.map { $0["name"] + ((Int($0["rating"]) ?? 0) > 0 ? " " + $0["rating"] : "") }
            return [[program["name"], program["extra"]].filter { !$0.isEmpty }.joined(separator: " · ") + (options.isEmpty ? "" : " · " + options.joined(separator: ", ")), program["rating"], program["equipped"].lowercased() == "true" ? "Yes" : "No", citation(program.fields)]
        })
        if isMagician {
            table("Spells", ["Spell", "Type / Range / Duration / DV", "Source"], [3, 4, 1.4], rows(collection: "spells", item: "spell").map { [$0["name"], summary($0.fields, keys: [("type", ""), ("range", ""), ("duration", ""), ("dv", "DV")]), citation($0.fields)] })
        }
        if isAdept {
            table("Adept powers", ["Power", "Source"], [7, 1.4], rows(collection: "powers", item: "power").map { [powerSummary($0), citation($0.fields)] })
        }
        if isTechnomancer {
            table("Complex forms", ["Form", "Rating", "Source"], [6, 1, 1.4], rows(collection: "complexforms", item: "complexform").map { [$0["name"], $0["rating"], citation($0.fields)] })
        }
        if isMagician || isTechnomancer {
            table(isTechnomancer ? "Sprites" : "Spirits", ["Name", "Force", "Services", "Bound"], [5, 1, 1, 1], rows(collection: "spirits", item: "spirit").map { [$0["name"], $0["force"], $0["services"], $0["bound"]] })
        }
        table("Vehicles / Drones", ["Vehicle", "Handling / Speed / Body / Armor / Pilot / Sensor", "Damage", "Source"], [2.5, 4, 1, 1.4], rows(collection: "vehicles", item: "vehicle").map { [$0["name"], summary($0.fields, keys: [("handling", "Handling"), ("speed", "Speed"), ("body", "Body"), ("armor", "Armor"), ("pilot", "Pilot"), ("sensor", "Sensor")]), "\(vehicleDamage($0))/\(vehicleCapacity($0))", citation($0.fields)] })
        table("Contacts", ["Contact", "Connection", "Loyalty", "Notes"], [2, 1, 1, 4], rows(collection: "contacts", item: "contact").map { contact in [contact["name"], contact["connection"], contact["loyalty"], (["BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "EDG", "ESS", "MAG", "RES", "initiative", "skills"].compactMap { key -> String? in
            let detail = contactDetail(contact.id, key: key)
            return detail.isEmpty ? nil : key + ": " + detail
        } + [contactNotes(contact.id)]).filter { !$0.isEmpty }.joined(separator: "\n")] })
        table("Personal history", ["Field", "Value"], [2, 5], notes.fields.keys.filter { $0.hasPrefix("history.") && !(notes.fields[$0] ?? "").isEmpty }.sorted().map { [String($0.dropFirst(8)), notes.fields[$0] ?? ""] })
        table("Unlisted equipment notes", ["Name", "Details"], [2, 5], notes.pendingEquipment.map { entry in [entry.name, ([entry.category] + entry.stats.keys.sorted().map { key in key + ": " + (entry.stats[key] ?? "") } + entry.adjustments.keys.sorted().map { key in key + ": " + (entry.adjustments[key] ?? "") }).joined(separator: " · ")] })
        for (title, content) in [("IDs / Licenses", dossierField("identities")), ("Description", value("description")), ("Background", value("background")), ("Concept", value("concept")), ("Notes", plainNotes)] where !content.isEmpty {
            table(title, [""], [1], [[content]])
        }
        return PrintableSheet(title: title, sections: sections, monitors: [true, false].map { physical in
            .init(title: physical ? "Physical" : "Stun", damage: damage(physical), capacity: trackCapacity(physical), threshold: woundThreshold, offset: woundOffset, ignored: ignoresWounds(physical: physical))
        })
    }
}
