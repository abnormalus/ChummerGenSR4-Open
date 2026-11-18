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
    var combatOnly = false
    @State private var editingGear: SheetGear?
    @State private var reloadingWeapon: SheetRow?
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
    private var visibleSections: [EquipmentSection] {
        sections.filter { section in
            if section.collection == "weapons" { return false }
            if combatOnly && !["spells", "powers", "complexforms", "spirits", "vehicles"].contains(section.collection) { return false }
            switch section.collection {
            case "spells": return character.isMagician
            case "powers": return character.isAdept
            case "complexforms": return character.isTechnomancer
            case "spirits": return character.isMagician || character.isTechnomancer
            default: return true
            }
        }
    }
    private var weapons: [SheetRow] { character.rows(collection: "weapons", item: "weapon") }
    private var assignedAmmoPaths: Set<[Int]> { Set(weapons.flatMap { (character.ammunition(forWeapon: $0) + character.grenades(forWeapon: $0)).map(\.path) }) }
    private var weaponSection: some View {
        SheetCard(title: "Weapons", rowSpacing: 0) {
            ForEach(weapons) { weapon in
                VStack(alignment: .leading, spacing: 0) {
                    HStack(spacing: 4) {
                        let grenades = character.grenades(forWeapon: weapon)
                        if !grenades.isEmpty {
                            Button("Use") {
                                if let grenade = grenades.first(where: { (Decimal(string: $0["qty"]) ?? 0) >= 1 }) {
                                    do { try character.useGrenade(path: grenade.path) } catch { errorMessage = error.localizedDescription }
                                }
                            }.font(.caption).buttonStyle(.borderless).frame(minWidth: 44, minHeight: 44)
                                .disabled(!grenades.contains { (Decimal(string: $0["qty"]) ?? 0) >= 1 })
                                .accessibilityLabel("Use " + weapon["name"])
                        }
                    DisclosureGroup {
                        ForEach(["name", "damage", "ap", "mode", "rc", "ammo"], id: \.self) { key in
                            SheetField(title: key.uppercased(), value: Binding(get: {
                                weapons.first { $0.id == weapon.id }?[key] ?? ""
                            }, set: { character.setRowValue(collection: "weapons", item: "weapon", index: weapon.id, field: key, value: $0) }))
                        }
                        SheetField(title: "Shown damage override (optional)", value: weaponAdjustment(weapon, field: "damage"))
                        SheetField(title: "Shown AP override (optional)", value: weaponAdjustment(weapon, field: "ap"))
                        Text("Leave overrides blank to calculate from the saved weapon, loaded ammunition and modifiers.")
                            .font(.caption).foregroundStyle(SheetTheme.secondary)
                        if weapon["type"].lowercased() == "ranged" {
                            Button("Reload / change ammo…", systemImage: "arrow.triangle.2.circlepath") { reloadingWeapon = weapon }
                                .frame(minHeight: 44)
                            Menu("Active magazine: \(character.activeAmmoSlot(for: weapon))") {
                                ForEach(1...4, id: \.self) { slot in
                                    Button("Use magazine \(slot)") {
                                        do { try character.setWeaponAdjustment(weapon, field: "ammoSlot", value: String(slot)) }
                                        catch { errorMessage = error.localizedDescription }
                                    }
                                }
                            }.frame(minHeight: 44)
                        }
                    } label: {
                        HStack {
                            Text(weapon["name"]).bold().foregroundStyle(.primary)
                            if !grenades.isEmpty {
                                Text("×" + NSDecimalNumber(decimal: grenades.reduce(Decimal.zero) { $0 + (Decimal(string: $1["qty"]) ?? 0) }).stringValue)
                                    .font(.caption).accessibilityIdentifier("grenade-count-" + weapon["guid"])
                            }
                            Text([weapon["mode"], character.weaponDamage(weapon), character.weaponAP(weapon).isEmpty ? "" : "AP " + character.weaponAP(weapon)].filter { !$0.isEmpty && $0 != "0" }.joined(separator: " · ")).font(.caption).foregroundStyle(SheetTheme.secondary)
                            Spacer()
                            SourceReference(source: weapon["source"], page: weapon["page"])
                        }
                    }
                    }.sheetRow(weapon.id)
                    if weapon["type"].lowercased() == "ranged" {
                        ForEach(1...4, id: \.self) { slot in
                            let suffix = slot == 1 ? "" : String(slot)
                            if (Int(weapon["ammoremaining" + suffix]) ?? 0) > 0 {
                                fireMenu(weapon: weapon, title: "Magazine \(slot) · \(character.loadedAmmunition(for: weapon, slot: slot)?["name"] ?? "Ammunition") · \(weapon["ammoremaining" + suffix]) rounds") { rounds in
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
            if weapons.isEmpty { Text("No entries").font(.caption).foregroundStyle(SheetTheme.secondary) }
        }
    }
    private func weaponAdjustment(_ weapon: SheetRow, field: String) -> Binding<String> {
        Binding(get: { character.weaponAdjustment(weapon, field: field) }, set: {
            do { try character.setWeaponAdjustment(weapon, field: field, value: $0) }
            catch { errorMessage = error.localizedDescription }
        })
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
            Divider()
            Button("Reload / change ammo…", systemImage: "arrow.triangle.2.circlepath") { reloadingWeapon = weapon }
        } label: {
            HStack { Text(title).foregroundStyle(.primary); Image(systemName: "chevron.down").font(.caption2) }
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
                }.font(.caption).foregroundStyle(SheetTheme.secondary).padding(.horizontal, 8).padding(.vertical, 4)
                ForEach(Array(character.programs.enumerated()), id: \.element.id) { index, program in
                    HStack(spacing: 8) {
                        GearTree(gear: program, showChildren: false, banded: false, edit: { editingGear = $0 })
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
    private func augmentationSummary(_ row: SheetRow) -> String {
        [row["name"], row["extra"].isEmpty ? "" : "(" + row["extra"] + ")", (Int(row["rating"]) ?? 0) > 0 ? "Rating " + row["rating"] : ""].filter { !$0.isEmpty }.joined(separator: " ")
    }
    private func gearEntry(_ gear: SheetGear, index: Int) -> some View {
        GearTree(gear: gear, shaded: index.isMultiple(of: 2), hiddenPaths: assignedAmmoPaths, edit: { editingGear = $0 }, use: { grenade in
            do { try character.useGrenade(path: grenade.path) } catch { errorMessage = error.localizedDescription }
        })
    }

    var body: some View {
        VStack(spacing: 16) {
            if !combatOnly && !character.notes.pendingEquipment.isEmpty {
                SheetCard(title: "Unlisted equipment notes", rowSpacing: 0) {
                    ForEach(Array(character.notes.pendingEquipment.enumerated()), id: \.element.id) { index, entry in
                        VStack(alignment: .leading, spacing: 4) {
                            Text(entry.name).bold()
                            Text(([entry.category] + entry.stats.keys.sorted().map { $0 + ": " + (entry.stats[$0] ?? "") }
                                + entry.adjustments.keys.sorted().map { $0 + ": " + (entry.adjustments[$0] ?? "") }).joined(separator: " · "))
                                .font(.caption).foregroundStyle(SheetTheme.secondary)
                        }.sheetRow(index)
                    }
                }
            }
            weaponSection
            if !combatOnly { matrixSection }
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 310), alignment: .top)], alignment: .leading, spacing: 16) {
                ForEach(visibleSections) { section in
                    SheetCard(title: section.collection == "spirits" ? (character.isTechnomancer ? "Sprites" : "Spirits") : section.title, rowSpacing: 0) {
                        let rows = character.rows(collection: section.collection, item: section.item)
                        if section.collection == "gears" {
                            let gear = character.gear.filter { !$0.isMatrixEquipment && !assignedAmmoPaths.contains($0.path) }
                            if gear.isEmpty { Text("No entries").font(.caption).foregroundStyle(SheetTheme.secondary) }
                            ForEach(Array(gear.enumerated()), id: \.element.id) { index, item in
                                gearEntry(item, index: index)
                            }
                        } else {
                            if rows.isEmpty { Text("No entries").font(.caption).foregroundStyle(SheetTheme.secondary) }
                            ForEach(rows) { row in
                                DisclosureGroup {
                                    if section.collection == "vehicles" {
                                        DamageTrackView(title: "Vehicle damage", damage: Binding(get: {
                                            character.vehicleDamage(character.rows(collection: "vehicles", item: "vehicle").first { $0.id == row.id } ?? row)
                                        }, set: { character.setVehicleDamage($0, index: row.id) }), capacity: character.vehicleCapacity(row))
                                    }
                                    ForEach(section.fields, id: \.self) { key in
                                        SheetField(title: key.uppercased(), value: Binding(get: {
                                            character.rows(collection: section.collection, item: section.item).first { $0.id == row.id }?[key] ?? ""
                                        }, set: { character.setRowValue(collection: section.collection, item: section.item, index: row.id, field: key, value: $0) }))
                                    }
                                } label: {
                                    HStack {
                                        Text(section.collection == "powers" ? character.powerSummary(row) : section.collection == "cyberwares" ? augmentationSummary(row) : row["name"]).foregroundStyle(.primary)
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
        .sheet(item: $reloadingWeapon) { weapon in
            ReloadMagazineView(character: $character, weaponIndex: weapon.id)
        }
        .sheet(item: $editingGear) { gear in
            NavigationStack {
                Form {
                    ForEach(["name", "rating", "qty", "notes"], id: \.self) { key in
                        SheetField(title: key.uppercased(), value: Binding(get: {
                            character.gear(at: gear.path)?[key] ?? ""
                        }, set: { character.setGearValue(path: gear.path, field: key, value: $0) }), multiline: key == "notes")
                    }
                }
                .navigationTitle("Edit Gear").navigationBarTitleDisplayMode(.inline)
                .toolbar { Button("Done") { editingGear = nil } }
            }
        }
        .alert("Couldn't save equipment", isPresented: Binding(get: { errorMessage != nil }, set: { if !$0 { errorMessage = nil } })) {
            Button("OK", role: .cancel) { errorMessage = nil }
        } message: { Text(errorMessage ?? "") }
    }
}

/// Loading is a manual sheet operation on existing ammunition, never a purchase.
private struct ReloadMagazineView: View {
    @Binding var character: DossierCharacter
    let weaponIndex: Int
    @Environment(\.dismiss) private var dismiss
    @State private var path: [Int] = []
    @State private var slot = 1
    @State private var rounds = 1
    @State private var errorMessage: String?
    private var weapon: SheetRow? { character.rows(collection: "weapons", item: "weapon").first { $0.id == weaponIndex } }
    var body: some View {
        NavigationStack {
            Form {
                if let weapon {
                    Section("Magazine") {
                        Picker("Slot", selection: $slot) {
                            ForEach(1...4, id: \.self) { Text("Magazine \($0)").tag($0) }
                        }
                        Picker("Ammunition", selection: $path) {
                            ForEach(character.ammunition(forWeapon: weapon)) { ammo in
                                Text(ammo["name"] + " · " + ammo["qty"] + " reserve").tag(ammo.path)
                            }
                        }
                        TextField("Rounds to load", value: $rounds, format: .number).keyboardType(.numberPad)
                        Text("Enter the desired magazine count. Existing rounds return to their original reserve. Check capacity and actions at the table.")
                            .font(.caption)
                    }
                    Section {
                        Button("Load Magazine") {
                            do { try character.reloadAmmunition(weaponIndex: weaponIndex, path: path, slot: slot, rounds: rounds); dismiss() }
                            catch { errorMessage = error.localizedDescription }
                        }.disabled(path.isEmpty || rounds < 1)
                        Button("Unload Magazine") {
                            do { try character.unloadAmmunition(weaponIndex: weaponIndex, slot: slot); dismiss() }
                            catch { errorMessage = error.localizedDescription }
                        }
                    }
                }
            }
            .navigationTitle("Reload").navigationBarTitleDisplayMode(.inline)
            .toolbar { Button("Cancel") { dismiss() } }
        }
        .onAppear {
            if let weapon {
                slot = character.activeAmmoSlot(for: weapon)
                path = character.loadedAmmunition(for: weapon)?.path ?? character.ammunition(forWeapon: weapon).first?.path ?? []
                rounds = max(1, Int(String(weapon["ammo"].prefix(while: { $0.isNumber }))) ?? 1)
            }
        }
        .alert("Couldn't change ammunition", isPresented: Binding(get: { errorMessage != nil }, set: { if !$0 { errorMessage = nil } })) {
            Button("OK", role: .cancel) { errorMessage = nil }
        } message: { Text(errorMessage ?? "") }
    }
}

/// Nested gear is displayed without manufacturing or moving equipment instances.
private struct GearTree: View {
    let gear: SheetGear
    var shaded = false
    var showChildren = true
    var banded = true
    var hiddenPaths: Set<[Int]> = []
    var edit: ((SheetGear) -> Void)?
    var use: ((SheetGear) -> Void)?
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
            Group {
                if banded { entry.sheetRow(shaded ? 0 : 1) } else { entry }
            }
            .contextMenu { if let edit { Button("Edit Gear", systemImage: "pencil") { edit(gear) } } }
            .accessibilityAction(named: "Edit Gear") { edit?(gear) }
            if showChildren {
                ForEach(Array(gear.children.filter { $0["category"] != "Program Options" && !$0.isProgram && !hiddenPaths.contains($0.path) }.enumerated()), id: \.element.id) { index, child in
                    GearTree(gear: child, shaded: index.isMultiple(of: 2), hiddenPaths: hiddenPaths, edit: edit, use: use).padding(.leading, 12)
                }
            }
        }
        .accessibilityElement(children: .contain)
    }
    private var entry: some View {
        HStack(spacing: 4) {
            if gear.isGrenade, let use {
                Button("Use") { use(gear) }.font(.caption).buttonStyle(.borderless).frame(minWidth: 44, minHeight: 44)
                    .disabled((Decimal(string: gear["qty"]) ?? 0) < 1)
                    .accessibilityLabel("Use " + gear["name"])
            }
            summary
            if gear.isGrenade { Text("×" + gear["qty"]).font(.caption) }
        }
    }
    @ViewBuilder private var summary: some View {
        if UIDevice.current.userInterfaceIdiom == .pad {
            HStack(alignment: .firstTextBaseline, spacing: 12) {
                // Keep long option lists readable without truncation or added row height.
                ScrollView(.horizontal, showsIndicators: false) {
                    HStack(alignment: .firstTextBaseline, spacing: 8) {
                        Text(gear["name"]).font(.subheadline.bold())
                        if !inlineOptions.isEmpty {
                            Text("· " + inlineOptions).font(.caption).foregroundStyle(SheetTheme.secondary)
                        }
                    }.fixedSize(horizontal: true, vertical: false)
                }
                if !details.isEmpty {
                    Text(details).font(.caption).foregroundStyle(SheetTheme.secondary).fixedSize()
                }
                SourceReference(source: gear["source"], page: gear["page"])
            }
        } else {
            VStack(alignment: .leading, spacing: 4) {
                HStack(alignment: .firstTextBaseline) {
                    Text(gear["name"]).font(.subheadline.bold())
                    Spacer(minLength: 8)
                    if !details.isEmpty { Text(details).font(.caption).foregroundStyle(SheetTheme.secondary) }
                    SourceReference(source: gear["source"], page: gear["page"])
                }
                if !inlineOptions.isEmpty { Text(inlineOptions).font(.caption).foregroundStyle(SheetTheme.secondary) }
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
                .font(.caption2).foregroundStyle(SheetTheme.secondary).fixedSize()
                .accessibilityLabel("Source: " + source + (page.isEmpty ? "" : ", page " + page))
        }
    }
}
