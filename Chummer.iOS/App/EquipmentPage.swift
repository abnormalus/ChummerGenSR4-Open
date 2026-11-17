import SwiftUI
import UIKit
import DossierCore

private struct EquipmentSection: Identifiable {
    let title: String, collection: String, item: String
    let fields: [String]
    var id: String { collection }
}

struct EquipmentPage: View {
    @Binding var character: DossierCharacter
    @State private var adding = false
    @State private var editingGear: SheetGear?
    @State private var errorMessage: String?
    private let sections: [EquipmentSection] = [
        .init(title: "Weapons", collection: "weapons", item: "weapon", fields: ["name", "damage", "ap", "mode", "rc", "ammo"]),
        .init(title: "Armor", collection: "armors", item: "armor", fields: ["name", "b", "i", "notes"]),
        .init(title: "Gear", collection: "gears", item: "gear", fields: ["name", "rating", "qty", "notes"]),
        .init(title: "Augmentations", collection: "cyberwares", item: "cyberware", fields: ["name", "rating", "ess", "notes"]),
        .init(title: "Spells", collection: "spells", item: "spell", fields: ["name", "type", "range", "duration", "dv"]),
        .init(title: "Adept powers", collection: "powers", item: "power", fields: ["name", "rating", "extra"]),
        .init(title: "Complex forms", collection: "complexforms", item: "complexform", fields: ["name", "rating", "extra"]),
        .init(title: "Spirits / sprites", collection: "spirits", item: "spirit", fields: ["name", "force", "services", "bound"]),
        .init(title: "Vehicles", collection: "vehicles", item: "vehicle", fields: ["name", "handling", "accel", "speed", "body", "armor", "pilot", "sensor"])
    ]
    private var weapons: [SheetRow] { character.rows(collection: "weapons", item: "weapon") }
    private var assignedAmmoPaths: Set<[Int]> { Set(weapons.flatMap { character.ammunition(forWeapon: $0).map(\.path) }) }
    private var weaponSection: some View {
        SheetCard(title: "Weapons", rowSpacing: 0) {
            ForEach(weapons) { weapon in
                VStack(alignment: .leading, spacing: 0) {
                    DisclosureGroup {
                        ForEach(["name", "damage", "ap", "mode", "rc", "ammo"], id: \.self) { key in
                            SheetField(title: key.uppercased(), value: Binding(get: {
                                weapons.first { $0.id == weapon.id }?[key] ?? ""
                            }, set: { character.setRowValue(collection: "weapons", item: "weapon", index: weapon.id, field: key, value: $0) }))
                        }
                    } label: {
                        HStack {
                            Text(weapon["name"]).bold()
                            Text([weapon["mode"], weapon["damage"], weapon["ap"].isEmpty ? "" : "AP " + weapon["ap"]].filter { !$0.isEmpty && $0 != "0" }.joined(separator: " · ")).font(.caption).foregroundStyle(.secondary)
                            Spacer()
                            SourceReference(source: weapon["source"], page: weapon["page"])
                        }
                    }.sheetRow(weapon.id)
                    if weapon["type"].lowercased() == "ranged" {
                        ForEach(1...4, id: \.self) { slot in
                            let suffix = slot == 1 ? "" : String(slot)
                            if (Int(weapon["ammoremaining" + suffix]) ?? 0) > 0 {
                                fireMenu(weapon: weapon, title: "Loaded magazine \(slot) · \(weapon["ammoremaining" + suffix]) rounds") { rounds in
                                    try character.spendLoadedAmmunition(weaponIndex: weapon.id, slot: slot, rounds: rounds)
                                }.sheetRow(slot).padding(.leading, 12)
                            }
                        }
                        ForEach(Array(character.ammunition(forWeapon: weapon).enumerated()), id: \.element.id) { index, ammo in
                            HStack {
                                fireMenu(weapon: weapon, title: ammo["name"] + " · Reserve " + ammo["qty"]) { rounds in
                                    try character.spendAmmunition(weaponIndex: weapon.id, path: ammo.path, rounds: rounds)
                                }
                                Spacer()
                                SourceReference(source: ammo["source"], page: ammo["page"])
                            }.sheetRow(index).padding(.leading, 12)
                        }
                    }
                }
            }
            if weapons.isEmpty { Text("No entries").font(.caption).foregroundStyle(.secondary) }
        }
    }
    private func fireMenu(weapon: SheetRow, title: String, spend: @escaping (Int) throws -> Void) -> some View {
        Menu {
            let suggested = character.suggestedFireCounts(for: weapon)
            ForEach(suggested, id: \.self) { count in
                Button("Fire \(count)") { do { try spend(count) } catch { errorMessage = error.localizedDescription } }
            }
            Menu("Other round counts") {
                ForEach([1, 2, 3, 6, 9, 15].filter { !suggested.contains($0) }, id: \.self) { count in
                    Button("Fire \(count)") { do { try spend(count) } catch { errorMessage = error.localizedDescription } }
                }
            }
        } label: {
            HStack { Text(title); Image(systemName: "chevron.down").font(.caption2) }
                .font(.subheadline).frame(minHeight: 44)
        }
        .accessibilityHint("Choose rounds to spend from this ammunition supply")
    }
    private var matrixGear: [SheetGear] { character.gear.filter { $0.isMatrixEquipment } }
    @ViewBuilder private var matrixSection: some View {
        let hardware = matrixGear.filter { !$0.isProgram }
        if !hardware.isEmpty {
            SheetCard(title: "Commlinks", rowSpacing: 0) {
                ForEach(Array(hardware.enumerated()), id: \.element.id) { index, gear in
                    gearEntry(gear, index: index)
                }
            }
        }
        if !character.programs.isEmpty {
            SheetCard(title: "Programs", rowSpacing: 0) {
                HStack {
                    Text("Program / options").frame(maxWidth: .infinity, alignment: .leading)
                    Text("Loaded").frame(width: 60)
                }.font(.caption).foregroundStyle(.secondary).padding(.horizontal, 8).padding(.vertical, 4)
                ForEach(Array(character.programs.enumerated()), id: \.element.id) { index, program in
                    HStack(spacing: 8) {
                        GearTree(gear: program, showChildren: false, banded: false)
                        Toggle(isOn: Binding(get: {
                            character.programs.first { $0.id == program.id }?["equipped"].lowercased() == "true"
                        }, set: { loaded in
                            character.setGearValue(path: program.path, field: "equipped", value: loaded ? "True" : "False")
                        })) {
                            Image(systemName: program["equipped"].lowercased() == "true" ? "checkmark.square.fill" : "square")
                                .font(.title3)
                        }
                        .toggleStyle(.button).buttonStyle(.plain).frame(width: 60).frame(minHeight: 44)
                        .accessibilityLabel("Load " + program["name"])
                    }.sheetRow(index)
                }
            }
        }
    }
    private func gearEntry(_ gear: SheetGear, index: Int) -> some View {
        GearTree(gear: gear, shaded: index.isMultiple(of: 2), hiddenPaths: assignedAmmoPaths)
            .contextMenu { Button("Edit Gear", systemImage: "pencil") { editingGear = gear } }
            .accessibilityAction(named: "Edit Gear") { editingGear = gear }
    }

    var body: some View {
        VStack(spacing: 16) {
            Button("Add Equipment", systemImage: "plus.circle") { adding = true }.buttonStyle(.borderedProminent)
            if !character.notes.pendingEquipment.isEmpty {
                SheetCard(title: "New equipment awaiting desktop review", rowSpacing: 0) {
                    Text("These entries stay in your dossier. Matching them to desktop Chummer equipment is the next development stage.")
                        .font(.caption).foregroundStyle(.secondary)
                    ForEach(Array(character.notes.pendingEquipment.enumerated()), id: \.element.id) { index, gear in
                        VStack(alignment: .leading, spacing: 4) {
                        HStack {
                            VStack(alignment: .leading) { Text(gear.name).bold(); Text(gear.category).font(.caption) }
                            Spacer()
                            Button("Remove", role: .destructive) {
                                do { try character.removePendingEquipment(gear.id) } catch { errorMessage = error.localizedDescription }
                            }
                        }
                        if !gear.stats.isEmpty { Text(gear.stats.keys.sorted().map { "\($0): \(gear.stats[$0] ?? "")" }.joined(separator: " · ")).font(.caption) }
                        if !gear.adjustments.isEmpty { Text("Adjustments: " + gear.adjustments.keys.sorted().map { "\($0): \(gear.adjustments[$0] ?? "")" }.joined(separator: " · ")).font(.caption) }
                        }.sheetRow(index)
                    }
                }
            }
            weaponSection
            matrixSection
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 310), alignment: .top)], alignment: .leading, spacing: 16) {
                ForEach(sections.filter { $0.collection != "weapons" }) { section in
                    SheetCard(title: section.title, rowSpacing: 0) {
                        let rows = character.rows(collection: section.collection, item: section.item)
                        if section.collection == "gears" {
                            let gear = character.gear.filter { !$0.isMatrixEquipment && !assignedAmmoPaths.contains($0.path) }
                            if gear.isEmpty { Text("No entries").font(.caption).foregroundStyle(.secondary) }
                            ForEach(Array(gear.enumerated()), id: \.element.id) { index, item in
                                gearEntry(item, index: index)
                            }
                        } else {
                            if rows.isEmpty { Text("No entries").font(.caption).foregroundStyle(.secondary) }
                            ForEach(rows) { row in
                                DisclosureGroup {
                                    ForEach(section.fields, id: \.self) { key in
                                        SheetField(title: key.uppercased(), value: Binding(get: {
                                            character.rows(collection: section.collection, item: section.item).first { $0.id == row.id }?[key] ?? ""
                                        }, set: { character.setRowValue(collection: section.collection, item: section.item, index: row.id, field: key, value: $0) }))
                                    }
                                } label: {
                                    HStack {
                                        Text(row["name"])
                                        Spacer()
                                        SourceReference(source: row["source"], page: row["page"])
                                    }
                                }.sheetRow(row.id)
                            }
                        }
                    }
                }
            }
        }
        .sheet(item: $editingGear) { gear in
            NavigationStack {
                Form {
                    ForEach(["name", "rating", "qty", "notes"], id: \.self) { key in
                        SheetField(title: key.uppercased(), value: Binding(get: {
                            character.rows(collection: "gears", item: "gear").first { $0.id == gear.path[0] }?[key] ?? ""
                        }, set: { character.setRowValue(collection: "gears", item: "gear", index: gear.path[0], field: key, value: $0) }), multiline: key == "notes")
                    }
                }
                .navigationTitle("Edit Gear").navigationBarTitleDisplayMode(.inline)
                .toolbar { Button("Done") { editingGear = nil } }
            }
        }
        .sheet(isPresented: $adding) { AddEquipmentView { gear in
            do { try character.addEquipment(gear); adding = false } catch { errorMessage = error.localizedDescription }
        } }
        .alert("Couldn't save equipment", isPresented: Binding(get: { errorMessage != nil }, set: { if !$0 { errorMessage = nil } })) {
            Button("OK", role: .cancel) { errorMessage = nil }
        } message: { Text(errorMessage ?? "") }
    }
}

private struct AddEquipmentView: View {
    @Environment(\.dismiss) private var dismiss
    @State private var name = ""
    @State private var category = "Ranged weapon"
    @State private var damage = ""
    @State private var ap = ""
    @State private var recoilAdjustment = ""
    @State private var notes = ""
    let save: (PendingEquipment) -> Void
    var body: some View {
        NavigationStack {
            Form {
                Section("Equipment") {
                    TextField("Name, e.g. ares alp", text: $name)
                    Picker("Category", selection: $category) {
                        ForEach(["Ranged weapon", "Melee weapon", "Armor", "Gear", "Commlink", "Augmentation", "Vehicle"], id: \.self) { Text($0) }
                    }
                    TextField("Notes", text: $notes, axis: .vertical)
                }
                if category.contains("weapon") {
                    Section("Base stats (optional)") {
                        TextField("Damage", text: $damage)
                        TextField("Armor penetration", text: $ap)
                    }
                    Section("Keep these adjustments") {
                        TextField("Recoil adjustment", text: $recoilAdjustment)
                    }
                }
                Section { Text("You can enter an incomplete name. Desktop equipment matching will be added in the next development stage.") }
            }
            .navigationTitle("Add Equipment").navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Cancel") { dismiss() } }
                ToolbarItem(placement: .confirmationAction) { Button("Add") {
                    var stats = ["notes": notes]
                    if !damage.isEmpty { stats["damage"] = damage }
                    if !ap.isEmpty { stats["ap"] = ap }
                    save(PendingEquipment(name: name.trimmingCharacters(in: .whitespacesAndNewlines), category: category,
                                          stats: stats, adjustments: recoilAdjustment.isEmpty ? [:] : ["rc": recoilAdjustment]))
                }.disabled(name.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty) }
            }
        }
    }
}

/// Nested gear is displayed without manufacturing or moving equipment instances.
private struct GearTree: View {
    let gear: SheetGear
    var shaded = false
    var showChildren = true
    var banded = true
    var hiddenPaths: Set<[Int]> = []
    private var options: String {
        gear.children.filter { $0["category"] == "Program Options" }.map {
            $0["name"] + ((Int($0["rating"]) ?? 0) > 0 ? " \($0["rating"])" : "")
        }.joined(separator: " · ")
    }
    private var inlineOptions: String {
        [gear["extra"], options].filter { !$0.isEmpty }.joined(separator: " · ")
    }
    private var details: String {
        var values: [String] = []
        if let rating = Int(gear["rating"]), rating > 0 { values.append("Rating \(rating)") }
        for (key, label) in [("response", "Response"), ("signal", "Signal"), ("system", "System"), ("firewall", "Firewall")] {
            if let value = Int(gear[key]), value > 0 { values.append("\(label) \(value)") }
        }
        return values.joined(separator: " · ")
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            if banded { summary.sheetRow(shaded ? 0 : 1) } else { summary }
            if showChildren {
                ForEach(Array(gear.children.filter { $0["category"] != "Program Options" && !$0.isProgram && !hiddenPaths.contains($0.path) }.enumerated()), id: \.element.id) { index, child in
                    GearTree(gear: child, shaded: index.isMultiple(of: 2), hiddenPaths: hiddenPaths).padding(.leading, 12)
                }
            }
        }
        .accessibilityElement(children: .contain)
    }
    @ViewBuilder private var summary: some View {
        if UIDevice.current.userInterfaceIdiom == .pad {
            HStack(alignment: .firstTextBaseline, spacing: 12) {
                // Keep long option lists readable without truncation or added row height.
                ScrollView(.horizontal, showsIndicators: false) {
                    HStack(alignment: .firstTextBaseline, spacing: 8) {
                        Text(gear["name"]).font(.subheadline.bold())
                        if !inlineOptions.isEmpty {
                            Text("· " + inlineOptions).font(.caption).foregroundStyle(.secondary)
                        }
                    }.fixedSize(horizontal: true, vertical: false)
                }
                if !details.isEmpty {
                    Text(details).font(.caption).foregroundStyle(.secondary).fixedSize()
                }
                SourceReference(source: gear["source"], page: gear["page"])
            }
        } else {
            VStack(alignment: .leading, spacing: 4) {
                HStack(alignment: .firstTextBaseline) {
                    Text(gear["name"]).font(.subheadline.bold())
                    Spacer(minLength: 8)
                    if !details.isEmpty { Text(details).font(.caption).foregroundStyle(.secondary) }
                    SourceReference(source: gear["source"], page: gear["page"])
                }
                if !inlineOptions.isEmpty { Text(inlineOptions).font(.caption).foregroundStyle(.secondary) }
            }
        }
    }
}

private struct SourceReference: View {
    let source: String
    let page: String
    var body: some View {
        if !source.isEmpty {
            Text(source + (page.isEmpty ? "" : " p. " + page))
                .font(.caption2).foregroundStyle(.secondary).fixedSize()
                .accessibilityLabel("Source: " + source + (page.isEmpty ? "" : ", page " + page))
        }
    }
}
