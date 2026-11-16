import SwiftUI
import DossierCore

private struct EquipmentSection: Identifiable {
    let title: String, collection: String, item: String
    let fields: [String]
    var id: String { collection }
}

struct EquipmentPage: View {
    @Binding var character: DossierCharacter
    @State private var adding = false
    @State private var errorMessage: String?
    private let sections: [EquipmentSection] = [
        .init(title: "Weapons", collection: "weapons", item: "weapon", fields: ["name", "damage", "ap", "mode", "rc", "ammo"]),
        .init(title: "Armor", collection: "armors", item: "armor", fields: ["name", "b", "i", "notes"]),
        .init(title: "Gear / commlinks", collection: "gears", item: "gear", fields: ["name", "rating", "qty", "notes"]),
        .init(title: "Augmentations", collection: "cyberwares", item: "cyberware", fields: ["name", "rating", "ess", "notes"]),
        .init(title: "Spells", collection: "spells", item: "spell", fields: ["name", "type", "range", "duration", "dv"]),
        .init(title: "Adept powers", collection: "powers", item: "power", fields: ["name", "rating", "extra"]),
        .init(title: "Complex forms", collection: "complexforms", item: "complexform", fields: ["name", "rating", "extra"]),
        .init(title: "Spirits / sprites", collection: "spirits", item: "spirit", fields: ["name", "force", "services", "bound"]),
        .init(title: "Vehicles", collection: "vehicles", item: "vehicle", fields: ["name", "handling", "accel", "speed", "body", "armor", "pilot", "sensor"])
    ]
    var body: some View {
        VStack(spacing: 16) {
            Button("Add Equipment", systemImage: "plus.circle") { adding = true }.buttonStyle(.borderedProminent)
            if !character.notes.pendingEquipment.isEmpty {
                SheetCard(title: "New equipment awaiting desktop review") {
                    Text("These entries stay in your dossier. Matching them to desktop Chummer equipment is the next development stage.")
                        .font(.caption).foregroundStyle(.secondary)
                    ForEach(character.notes.pendingEquipment) { gear in
                        HStack {
                            VStack(alignment: .leading) { Text(gear.name).bold(); Text(gear.category).font(.caption) }
                            Spacer()
                            Button("Remove", role: .destructive) {
                                do { try character.removePendingEquipment(gear.id) } catch { errorMessage = error.localizedDescription }
                            }
                        }
                        if !gear.stats.isEmpty { Text(gear.stats.keys.sorted().map { "\($0): \(gear.stats[$0] ?? "")" }.joined(separator: " · ")).font(.caption) }
                        if !gear.adjustments.isEmpty { Text("Adjustments: " + gear.adjustments.keys.sorted().map { "\($0): \(gear.adjustments[$0] ?? "")" }.joined(separator: " · ")).font(.caption) }
                    }
                }
            }
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 310), alignment: .top)], alignment: .leading, spacing: 16) {
                ForEach(sections) { section in
                    SheetCard(title: section.title) {
                        let rows = character.rows(collection: section.collection, item: section.item)
                        if rows.isEmpty { Text("No entries").font(.caption).foregroundStyle(.secondary) }
                        ForEach(rows) { row in
                            Text(row["name"]).font(.subheadline.bold())
                            ForEach(section.fields, id: \.self) { key in
                                SheetField(title: key.uppercased(), value: Binding(get: {
                                    character.rows(collection: section.collection, item: section.item).first { $0.id == row.id }?[key] ?? ""
                                }, set: { character.setRowValue(collection: section.collection, item: section.item, index: row.id, field: key, value: $0) }))
                            }
                            Divider()
                        }
                    }
                }
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
