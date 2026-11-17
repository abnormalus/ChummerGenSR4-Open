import Foundation

public struct SheetAttribute: Identifiable, Equatable {
    public var id: String
    public var base: Int
    public var total: Int
}
public struct SheetSkill: Identifiable, Equatable {
    public var id: Int
    public var name: String
    public var attribute: String
    public var rating: Int
    public var pool: Int
}
public struct SheetRow: Identifiable, Equatable {
    public var id: Int
    public var fields: [String: String]
    public subscript(_ key: String) -> String { fields[key] ?? "" }
}

/// A gear instance retains its source hierarchy; paths distinguish repeated names.
public struct SheetGear: Identifiable {
    public let path: [Int]
    public let fields: [String: String]
    public let children: [SheetGear]
    public var id: [Int] { path }
    public subscript(_ key: String) -> String { fields[key] ?? "" }
    public var isProgram: Bool { self["category"] == "Matrix Programs" || self["category"] == "Sensor Software" }
    public var isMatrixEquipment: Bool {
        ["Commlink", "Matrix Programs", "Sensor Software", "Commlink Operating System"].contains(self["category"])
    }
    public var descendants: [SheetGear] { children.flatMap { [$0] + $0.descendants } }
}

/// Sheet arithmetic only. Imported totals supply the modifier offset; no catalogs,
/// cyberlimb averaging, purchase accounting, or advancement rules are executed here.
public struct DossierCharacter {
    // Each value snapshot owns a fresh cache after an edit. Sharing an unchanged
    // cache is safe; old snapshots retain both their XML and projections.
    private final class Cache {
        var data: Data?
        var rows: [String: [SheetRow]] = [:]
        var attributes: [SheetAttribute]?
        var skills: [SheetSkill]?
        var gear: [SheetGear]?
        var programs: [SheetGear]?
        var notes: (String, DossierNotes)?
    }
    private var cache = Cache()
    public private(set) var revision = UUID()
    private var xml: CharacterXML {
        didSet { cache = Cache(); revision = UUID() }
    }
    public init(data: Data) throws {
        xml = try CharacterXML(data: data)
        _ = try DossierNotes.split(xml.root.value("notes"))
        for contact in xml.root.child("contacts")?.children("contact") ?? [] {
            _ = try DossierNotes.split(contact.value("notes"))
        }
    }
    public func data() -> Data {
        if let data = cache.data { return data }
        let data = xml.data(); cache.data = data; return data
    }
    /// Prevent text fields from producing numeric data the desktop cannot load.
    public func exportData() throws -> Data {
        func integer(_ value: String, _ label: String) throws {
            guard Int32(value) != nil else { throw DossierError.invalidXML("\(label) must be a whole number") }
        }
        for key in ["karma", "totalkarma", "streetcred", "notoriety", "nuyen", "physicalcmfilled", "stuncmfilled"] {
            if let node = xml.root.child(key) { try integer(node.text, key) }
        }
        for attribute in xml.root.child("attributes")?.children("attribute") ?? [] {
            try integer(attribute.value("value"), attribute.value("name"))
        }
        for skill in rows(collection: "skills", item: "skill") { try integer(skill["rating"], skill["name"] + " rating") }
        for contact in rows(collection: "contacts", item: "contact") {
            try integer(contact["connection"], contact["name"] + " connection")
            try integer(contact["loyalty"], contact["name"] + " loyalty")
        }
        for (collection, item) in [("gears", "gear"), ("cyberwares", "cyberware"), ("powers", "power"), ("complexforms", "complexform")] {
            for row in rows(collection: collection, item: item) {
                if row.fields["rating"] != nil { try integer(row["rating"], row["name"] + " rating") }
                if let qty = row.fields["qty"], Decimal(string: qty, locale: Locale(identifier: "en_US_POSIX")) == nil {
                    throw DossierError.invalidXML(row["name"] + " quantity must be a number")
                }
            }
        }
        for row in rows(collection: "vehicles", item: "vehicle") {
            if let damage = row.fields["physicalcmfilled"] { try integer(damage, row["name"] + " damage") }
        }
        for row in rows(collection: "spirits", item: "spirit") {
            for key in ["force", "services"] where row.fields[key] != nil { try integer(row[key], row["name"] + " " + key) }
            if let bound = row.fields["bound"], !["true", "false"].contains(bound.lowercased()) {
                throw DossierError.invalidXML(row["name"] + " bound must be True or False")
            }
        }
        return data()
    }
    public func value(_ field: String) -> String { xml.root.value(field) }
    public mutating func setValue(_ field: String, _ value: String) { guard self.value(field) != value else { return }; xml.root.set(field, value) }
    public var title: String {
        let alias = value("alias")
        return alias.isEmpty ? (value("name").isEmpty ? "Character Dossier" : value("name")) : alias
    }
    public var attributes: [SheetAttribute] {
        if let attributes = cache.attributes { return attributes }
        let attributes = (xml.root.child("attributes")?.children("attribute") ?? []).map {
            let base = Int($0.value("value")) ?? 0
            return SheetAttribute(id: $0.value("name"), base: base, total: Int($0.value("totalvalue")) ?? base)
        }
        cache.attributes = attributes; return attributes
    }
    public func total(_ attribute: String) -> Int { attributes.first { $0.id == attribute }?.total ?? 0 }
    public mutating func setAttribute(_ id: String, base: Int) {
        guard let old = attributes.first(where: { $0.id == id }) else { return }
        changeAttribute(id, base: base, total: old.total + base - old.base, delta: base - old.base)
    }
    public mutating func setAttributeTotal(_ id: String, total: Int) {
        guard let old = attributes.first(where: { $0.id == id }) else { return }
        changeAttribute(id, base: old.base, total: total, delta: total - old.total)
    }
    private mutating func changeAttribute(_ id: String, base: Int, total: Int, delta: Int) {
        let priorSkills = skills
        xml.root.update("attributes") { attributes in
            attributes.update("attribute", matching: { $0.value("name") == id }) {
                $0.set("value", String(base)); $0.set("totalvalue", String(total))
            }
        }
        for skill in priorSkills where skill.attribute == id {
            setRowValue(collection: "skills", item: "skill", index: skill.id, field: "totalvalue", value: String(skill.pool + delta))
        }
    }
    public var skills: [SheetSkill] {
        if let skills = cache.skills { return skills }
        let skills = rows(collection: "skills", item: "skill").map { row in
            let rating = Int(row["rating"]) ?? 0
            let attr = total(row["attribute"])
            let defaultPool = rating == 0 && row["default"].lowercased() == "true" ? attr - 1 : attr + rating
            return SheetSkill(id: row.id, name: row["name"], attribute: row["attribute"], rating: rating,
                              pool: Int(row["totalvalue"]) ?? defaultPool)
        }
        cache.skills = skills; return skills
    }
    public mutating func setSkillRating(_ index: Int, rating: Int) {
        guard let prior = skills.first(where: { $0.id == index }) else { return }
        setRowValue(collection: "skills", item: "skill", index: index, field: "rating", value: String(rating))
        setSkillPool(index, pool: prior.pool + rating - prior.rating)
    }
    public mutating func setSkillPool(_ index: Int, pool: Int) {
        setRowValue(collection: "skills", item: "skill", index: index, field: "totalvalue", value: String(pool))
    }
    public var initiative: Int { total("REA") + total("INT") + (Int(dossierField("initiativeModifier")) ?? 0) }
    public var astralInitiative: Int { 2 * total("INT") + (Int(dossierField("astralModifier")) ?? 0) }
    /// Provisional sheet formulas use saved enabled improvements. Desktop settings can
    /// additionally reduce Response for program load; an explicit override covers that.
    public var matrixResponse: Int? {
        if let override = Int(dossierField("matrixResponse")) { return override }
        guard let link = gear.last(where: { $0["category"] == "Commlink" && $0["active"].lowercased() == "true" }) else { return nil }
        let upgrades = link.children.filter { $0["category"] == "Commlink Upgrade" }.compactMap { Int($0["response"]) }
        var response = max(Int(link["response"]) ?? 0, upgrades.max() ?? 0)
        if link["homenode"].lowercased() == "true" { response += Int(ceil(Double(total("INT")) / 2)) }
        return response
    }
    public var isTechnomancer: Bool {
        value("technomancerenabled").lowercased() == "true" || value("resenabled").lowercased() == "true"
    }
    func savedImprovement(_ type: String) -> Int {
        let entries = rows(collection: "improvements", item: "improvement").filter {
            $0["improvementttype"] == type && $0["enabled"].lowercased() != "false"
                && $0["addtorating"].lowercased() != "true"
                && !(isTechnomancer && type == "MatrixInitiativePass" && $0["improvementsource"] == "Gear")
        }
        func sum(_ rows: [SheetRow]) -> Int {
            var result = 0
            var unique: [String: [Int]] = [:]
            for row in rows {
                let value = Int(row["val"]) ?? 0
                if row["unique"].isEmpty { result += value }
                else { unique[row["unique"], default: []].append(value) }
            }
            if let values = unique["precedence0"] { return values.max() ?? 0 }
            if let values = unique["precedence1"] { return values.reduce(0, +) }
            return result + unique.values.reduce(0) { $0 + ($1.max() ?? 0) }
        }
        return sum(entries.filter { $0["custom"].lowercased() != "true" })
            + sum(entries.filter { $0["custom"].lowercased() == "true" })
    }
    public var matrixInitiative: Int? {
        let metatype = value("metatype")
        // Special personas need a desktop snapshot or explicit sheet input.
        if metatype.hasSuffix("Sprite") || metatype.hasSuffix("A.I.") || ["Technocritters", "Protosapients"].contains(value("metatypecategory")) {
            return Int(dossierField("matrixInitiativeOverride"))
        }
        let base: Int
        if isTechnomancer { base = 2 * total("INT") + 1 + savedImprovement("LivingPersonaResponse") }
        else {
            guard let response = matrixResponse else { return nil }
            base = total("INT") + response + savedImprovement("MatrixInitiative")
        }
        return base + (Int(dossierField("matrixInitiativeModifier")) ?? 0)
    }
    public var matrixInitiativePasses: Int {
        (isTechnomancer ? 3 : 1) + savedImprovement("MatrixInitiativePass")
            + savedImprovement("MatrixInitiativePassAdd") + (Int(dossierField("matrixPassesModifier")) ?? 0)
    }
    public func damage(_ physical: Bool) -> Int { max(0, Int(value(physical ? "physicalcmfilled" : "stuncmfilled")) ?? 0) }
    public mutating func setDamage(_ count: Int, physical: Bool) {
        setValue(physical ? "physicalcmfilled" : "stuncmfilled", String(max(0, count)))
    }
    public func trackCapacity(_ physical: Bool) -> Int { max(0, 8 + Int(ceil(Double(total(physical ? "BOD" : "WIL")) / 2)) + savedImprovement(physical ? "PhysicalCM" : "StunCM")) }

    public var gear: [SheetGear] {
        if let gear = cache.gear { return gear }
        func entries(_ elements: [CharacterElement], parent: [Int]) -> [SheetGear] {
            elements.enumerated().map { index, element in
                let path = parent + [index]
                var fields: [String: String] = [:]
                for case .element(let child) in element.content where child.name != "children" {
                    // Only expose scalar fields. Never flatten nested XML into display text.
                    if !child.content.contains(where: { if case .element = $0 { return true }; return false }) {
                        fields[child.name] = child.text
                    }
                }
                return SheetGear(path: path, fields: fields,
                                 children: entries(element.child("children")?.children("gear") ?? [], parent: path))
            }
        }
        let gear = entries(xml.root.child("gears")?.children("gear") ?? [], parent: [])
        cache.gear = gear; return gear
    }

    public mutating func setGearValue(path: [Int], field: String, value: String) {
        func contains(_ entries: [SheetGear], _ path: ArraySlice<Int>) -> Bool {
            guard let index = path.first, entries.indices.contains(index) else { return false }
            return path.count == 1 || contains(entries[index].children, path.dropFirst())
        }
        guard contains(gear, path[...]) else { return }
        func update(_ collection: inout CharacterElement, path: ArraySlice<Int>) {
            guard let index = path.first else { return }
            collection.updateRow("gear", index: index) { entry in
                if path.count == 1 { entry.set(field, value) }
                else { entry.update("children") { update(&$0, path: path.dropFirst()) } }
            }
        }
        xml.root.update("gears") { update(&$0, path: path[...]) }
    }

    public var programs: [SheetGear] {
        if let programs = cache.programs { return programs }
        let programs = gear.flatMap { [$0] + $0.descendants }.filter { $0.isProgram }.sorted {
            let order = $0["name"].localizedCaseInsensitiveCompare($1["name"])
            return order == .orderedSame ? $0.path.lexicographicallyPrecedes($1.path) : order == .orderedAscending
        }
        cache.programs = programs; return programs
    }

    public func rows(collection: String, item: String) -> [SheetRow] {
        let key = collection + "/" + item
        if let rows = cache.rows[key] { return rows }
        let rows = (xml.root.child(collection)?.children(item) ?? []).enumerated().map { index, element in
            var fields: [String: String] = [:]
            for entry in element.content {
                if case .element(let child) = entry, !child.content.contains(where: { if case .element = $0 { return true }; return false }) { fields[child.name] = child.text }
            }
            return SheetRow(id: index, fields: fields)
        }
        cache.rows[key] = rows; return rows
    }
    public mutating func setRowValue(collection: String, item: String, index: Int, field: String, value: String) {
        guard rows(collection: collection, item: item).first(where: { $0.id == index })?[field] != value else { return }
        xml.root.update(collection) { $0.updateRow(item, index: index) { $0.set(field, value) } }
    }

    private var splitNotes: (String, DossierNotes) {
        if let notes = cache.notes { return notes }
        let notes = (try? DossierNotes.split(value("notes"))) ?? (value("notes"), DossierNotes())
        cache.notes = notes; return notes
    }
    public var notes: DossierNotes { splitNotes.1 }
    public var plainNotes: String { splitNotes.0 }
    public mutating func setPlainNotes(_ text: String) throws { setValue("notes", try notes.merged(with: text)) }
    public func dossierField(_ key: String) -> String { notes.fields[key] ?? "" }
    public mutating func setDossierField(_ key: String, value: String) throws {
        try updateNotes { $0.fields[key] = value }
    }
    private mutating func updateNotes(_ body: (inout DossierNotes) -> Void) throws {
        var (plain, metadata) = try DossierNotes.split(value("notes"))
        body(&metadata)
        setValue("notes", try metadata.merged(with: plain))
    }
    public mutating func addEquipment(_ equipment: PendingEquipment) throws {
        try updateNotes { $0.pendingEquipment.append(equipment) }
    }
    public mutating func removePendingEquipment(_ id: UUID) throws {
        try updateNotes { $0.pendingEquipment.removeAll { $0.id == id } }
    }
    public func contactDetail(_ index: Int, key: String) -> String {
        let contacts = rows(collection: "contacts", item: "contact")
        guard contacts.indices.contains(index) else { return "" }
        return (try? DossierNotes.split(contacts[index]["notes"]).1.fields[key]) ?? ""
    }
    public mutating func setContactDetail(_ index: Int, key: String, value: String) throws {
        let contacts = rows(collection: "contacts", item: "contact")
        guard contacts.indices.contains(index) else { return }
        var (plain, metadata) = try DossierNotes.split(contacts[index]["notes"])
        metadata.fields[key] = value
        setRowValue(collection: "contacts", item: "contact", index: index, field: "notes", value: try metadata.merged(with: plain))
    }
    public func contactNotes(_ index: Int) -> String {
        let contacts = rows(collection: "contacts", item: "contact")
        guard contacts.indices.contains(index) else { return "" }
        return (try? DossierNotes.split(contacts[index]["notes"]).0) ?? contacts[index]["notes"]
    }
    public mutating func setContactNotes(_ index: Int, text: String) throws {
        let contacts = rows(collection: "contacts", item: "contact")
        guard contacts.indices.contains(index) else { return }
        let metadata = try DossierNotes.split(contacts[index]["notes"]).1
        setRowValue(collection: "contacts", item: "contact", index: index, field: "notes", value: try metadata.merged(with: text))
    }
    public mutating func addContact() {
        var contact = CharacterElement(name: "contact")
        for (key, value) in ["name": "New contact", "connection": "1", "loyalty": "1", "membership": "0",
                             "areaofinfluence": "0", "magicalresources": "0", "matrixresources": "0",
                             "type": "Contact", "file": "", "relative": "", "notes": "", "groupname": "",
                             "colour": "-16777216", "free": "False"] { contact.set(key, value) }
        xml.root.update("contacts") { $0.content.append(.element(contact)) }
    }
}
