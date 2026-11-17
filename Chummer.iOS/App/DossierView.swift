import SwiftUI
import UIKit
import DossierCore

enum DossierPage: String, CaseIterable, Identifiable {
    case character = "Character", combat = "Combat", equipment = "Equipment", background = "Background", contacts = "Contacts"
    var id: String { rawValue }
    var icon: String {
        switch self {
        case .character: return "person.text.rectangle"
        case .combat: return "scope"
        case .equipment: return "backpack"
        case .background: return "book.closed"
        case .contacts: return "person.2"
        }
    }
}

struct DossierView: View {
    @Binding var character: DossierCharacter
    @Binding var selectedPage: DossierPage
    @State private var errorMessage: String?
    var body: some View {
        Group {
            if UIDevice.current.userInterfaceIdiom == .pad {
                selectedContent
            } else {
                TabView(selection: $selectedPage) {
                    page { overview }.tabItem { Label("Character", systemImage: "person.text.rectangle") }.tag(DossierPage.character)
                    page { combat }.tabItem { Label("Combat", systemImage: "scope") }.tag(DossierPage.combat)
                    page { EquipmentPage(character: $character) }.tabItem { Label("Equipment", systemImage: "backpack") }.tag(DossierPage.equipment)
                    page { history }.tabItem { Label("Background", systemImage: "book.closed") }.tag(DossierPage.background)
                    page { contacts }.tabItem { Label("Contacts", systemImage: "person.2") }.tag(DossierPage.contacts)
                }
            }
        }
        .alert("Couldn't save that field", isPresented: Binding(get: { errorMessage != nil }, set: { if !$0 { errorMessage = nil } })) {
            Button("OK", role: .cancel) { errorMessage = nil }
        } message: { Text(errorMessage ?? "") }
    }
    @ViewBuilder private var selectedContent: some View {
        switch selectedPage {
        case .character: page { overview }
        case .combat: page { combat }
        case .equipment: page { EquipmentPage(character: $character) }
        case .background: page { history }
        case .contacts: page { contacts }
        }
    }
    private func page<Content: View>(@ViewBuilder content: () -> Content) -> some View {
        ScrollView { LazyVStack(spacing: 16) { content() }.frame(maxWidth: 1100).padding(16).frame(maxWidth: .infinity) }
            .background(SheetTheme.paper)
    }
    private func field(_ key: String) -> Binding<String> {
        Binding(get: { character.value(key) }, set: { character.setValue(key, $0) })
    }
    private func dossier(_ key: String) -> Binding<String> {
        Binding(get: { character.dossierField(key) }, set: { value in
            do { try character.setDossierField(key, value: value) } catch { errorMessage = error.localizedDescription }
        })
    }
    private var overview: some View {
        VStack(spacing: 16) {
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 310), alignment: .top)], alignment: .leading, spacing: 16) {
                SheetCard(title: "Personal data", rowSpacing: 0) {
                    SheetField(title: "Name", value: field("name")).sheetRow(0)
                    SheetField(title: "Primary alias", value: field("alias")).sheetRow(1)
                    HStack { SheetField(title: "Metatype", value: field("metatype")); SheetField(title: "Ethnicity", value: dossier("ethnicity")) }.sheetRow(2)
                    HStack { SheetField(title: "Age", value: field("age")); SheetField(title: "Sex", value: field("sex")) }.sheetRow(3)
                    HStack { SheetField(title: "Total karma", value: field("totalkarma")); SheetField(title: "Current karma", value: field("karma")) }.sheetRow(4)
                    HStack { SheetField(title: "Street cred", value: field("streetcred")); SheetField(title: "Notoriety", value: field("notoriety")) }.sheetRow(5)
                }
                SheetCard(title: "Attributes", rowSpacing: 0) {
                    HStack { Text("Attribute").frame(maxWidth: .infinity, alignment: .leading); Text("Base / Modified").font(.caption) }
                    ForEach(Array(character.attributes.enumerated()), id: \.element.id) { index, attribute in
                        HStack {
                            Text(attribute.id).font(.subheadline.bold()).frame(maxWidth: .infinity, alignment: .leading)
                            NumberField(title: "Base", value: Binding(get: { character.attributes.first { $0.id == attribute.id }?.base ?? 0 },
                                set: { character.setAttribute(attribute.id, base: $0) })).frame(width: 70)
                            NumberField(title: "Modified", value: Binding(get: { character.total(attribute.id) },
                                set: { character.setAttributeTotal(attribute.id, total: $0) })).frame(width: 70)
                        }.sheetRow(index)
                    }
                }

            }
            SheetCard(title: "Skills", rowSpacing: 0) {
                ForEach(character.skills) { skill in
                    HStack {
                        VStack(alignment: .leading) { Text(skill.name); Text("\(skill.attribute) · \(character.total(skill.attribute))").font(.caption).foregroundStyle(SheetTheme.secondary) }
                            .frame(maxWidth: .infinity, alignment: .leading)
                        NumberField(title: "Rating", value: Binding(get: { character.skills.first { $0.id == skill.id }?.rating ?? 0 }, set: { character.setSkillRating(skill.id, rating: $0) })).frame(width: 65)
                        NumberField(title: "Pool", value: Binding(get: { character.skills.first { $0.id == skill.id }?.pool ?? 0 }, set: { character.setSkillPool(skill.id, pool: $0) })).frame(width: 65)
                    }.sheetRow(skill.id)
                }
            }
            SheetCard(title: "IDs / lifestyles / currency") {
                SheetField(title: "Nuyen", value: field("nuyen"))
                SheetField(title: "IDs, licenses and related funds", value: dossier("identities"), multiline: true)
                ForEach(character.rows(collection: "lifestyles", item: "lifestyle")) { row in
                    Text(row["name"] + " · " + row["baselifestyle"]).sheetRow(row.id)
                }
            }
            SheetCard(title: "Qualities", rowSpacing: 0) {
                ForEach(character.rows(collection: "qualities", item: "quality")) { row in Text(row["name"] + " · " + row["extra"]).sheetRow(row.id) }
            }
        }
    }
    @ViewBuilder private var combat: some View {
        LazyVGrid(columns: [GridItem(.adaptive(minimum: 310), alignment: .top)], alignment: .leading, spacing: 16) {
                SheetCard(title: "Initiative", rowSpacing: 0) {
                    HStack { Text("Physical"); Spacer(); Text("\(character.initiative) · \(character.initiativePasses) passes").bold() }.sheetRow(0)
                    if character.isMagician {
                        HStack { Text("Astral"); Spacer(); Text("\(character.astralInitiative) · 3 passes") }.sheetRow(1)
                    }
                    HStack { Text("Matrix"); Spacer(); Text("\(character.matrixInitiative.map(String.init) ?? "—") · \(character.matrixInitiativePasses) passes") }.sheetRow(2)
                    DisclosureGroup("Initiative adjustments") {
                        SheetField(title: "Initiative modifier", value: dossier("initiativeModifier"))
                        SheetField(title: "Initiative passes override (optional)", value: dossier("initiativePasses"))
                        if character.isMagician { SheetField(title: "Astral modifier", value: dossier("astralModifier")) }
                        SheetField(title: "Response override (optional)", value: dossier("matrixResponse"))
                        SheetField(title: "Matrix initiative modifier", value: dossier("matrixInitiativeModifier"))
                        SheetField(title: "Matrix passes modifier", value: dossier("matrixPassesModifier"))
                        SheetField(title: "Special persona initiative (optional)", value: dossier("matrixInitiativeOverride"))
                        Text("Saved attributes and enabled bonuses supply sheet totals. Adjust temporary effects and desktop program-load penalties here.")
                            .font(.caption).foregroundStyle(SheetTheme.secondary)
                    }.padding(.horizontal, 8).padding(.vertical, 4)
                }
                SheetCard(title: "Condition monitor") {
                    HStack(alignment: .top, spacing: 12) {
                        DamageTrackView(title: "Physical", damage: Binding(get: { character.damage(true) }, set: { character.setDamage($0, physical: true) }), capacity: character.trackCapacity(true))
                        DamageTrackView(title: "Stun", damage: Binding(get: { character.damage(false) }, set: { character.setDamage($0, physical: false) }), capacity: character.trackCapacity(false))
                    }
                    Text("Tap to set damage; tap the last marked box to remove one. Hold a box after eight to adjust the optional track. Penalties appear every third usable box.")
                        .font(.caption).foregroundStyle(SheetTheme.secondary)
                }
        }
        EquipmentPage(character: $character, combatOnly: true)
    }
    private let historyKeys = ["Date created", "Date first played", "Date retired", "Biggest debt", "Corps with bounties on your head", "Biggest corps you've run against", "Biggest corps you've run for", "Biggest critters you've squared off with", "Largest karma award", "Largest debt collected", "Most nuyen acquired at one time", "Most people killed in a single firefight", "Most rounds fired in a single firefight", "Number of cities lived in", "Number of runs undertaken", "Number of successful runs", "Number of times dealing with a dragon", "Number of times screwed by Mr. Johnson", "Worst lie told by Mr. Johnson", "Worst lie told to Mr. Johnson", "Final fate"]
    private var history: some View {
        LazyVGrid(columns: [GridItem(.adaptive(minimum: 310), alignment: .top)], alignment: .leading, spacing: 16) {
            SheetCard(title: "Character history and background") {
                SheetField(title: "Background", value: field("background"), multiline: true)
                SheetField(title: "Concept", value: field("concept"), multiline: true)
            }
            SheetCard(title: "Character sketch and appearance") {
                if let data = Data(base64Encoded: character.value("mugshot")), let portrait = UIImage(data: data) {
                    Image(uiImage: portrait).resizable().scaledToFit().frame(maxHeight: 280)
                } else { Image(systemName: "person.crop.rectangle").font(.system(size: 80)).frame(maxWidth: .infinity).foregroundStyle(SheetTheme.ink) }
                HStack { SheetField(title: "Height", value: field("height")); SheetField(title: "Weight", value: field("weight")) }
                HStack { SheetField(title: "Eye color", value: field("eyes")); SheetField(title: "Hair color", value: field("hair")) }
                SheetField(title: "Other features", value: field("description"), multiline: true)
            }
            SheetCard(title: "Important dates and personal facts", rowSpacing: 0) {
                ForEach(historyKeys, id: \.self) { key in
                    SheetField(title: key, value: dossier("history." + key)).sheetRow(historyKeys.firstIndex(of: key) ?? 0)
                }
            }
            SheetCard(title: "Notes") {
                SheetField(title: "Notes", value: Binding(get: { character.plainNotes }, set: { value in
                    do { try character.setPlainNotes(value) } catch { errorMessage = error.localizedDescription }
                }), multiline: true)
            }
        }
    }
    private var contacts: some View {
        VStack(spacing: 16) {
            Button("Add Contact", systemImage: "person.badge.plus") { character.addContact() }.buttonStyle(.bordered)
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 310), alignment: .top)], alignment: .leading, spacing: 16) {
                ForEach(character.rows(collection: "contacts", item: "contact")) { contact in
                    SheetCard(title: contact["name"].isEmpty ? "Contact" : contact["name"], rowSpacing: 0) {
                        SheetField(title: "Name", value: contactField(contact.id, "name")).sheetRow(0)
                        HStack { SheetField(title: "Connection", value: contactField(contact.id, "connection")); SheetField(title: "Loyalty", value: contactField(contact.id, "loyalty")) }.sheetRow(1)
                        LazyVGrid(columns: Array(repeating: GridItem(.flexible()), count: 4)) {
                            ForEach(["BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "EDG", "ESS", "MAG", "RES"], id: \.self) { key in
                                SheetField(title: key, value: contactDetail(contact.id, key)).sheetRow(["BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "EDG", "ESS", "MAG", "RES"].firstIndex(of: key) ?? 0)
                            }
                        }
                        SheetField(title: "Initiative / passes", value: contactDetail(contact.id, "initiative")).sheetRow(2)
                        SheetField(title: "Skills and dice pools", value: contactDetail(contact.id, "skills"), multiline: true)
                        SheetField(title: "Professional rating", value: contactDetail(contact.id, "professionalRating")).sheetRow(3)
                        SheetField(title: "Notes", value: Binding(get: { character.contactNotes(contact.id) }, set: { value in
                            do { try character.setContactNotes(contact.id, text: value) } catch { errorMessage = error.localizedDescription }
                        }), multiline: true)
                    }
                }
            }
        }
    }
    private func contactField(_ index: Int, _ key: String) -> Binding<String> {
        Binding(get: { character.rows(collection: "contacts", item: "contact").first { $0.id == index }?[key] ?? "" },
                set: { character.setRowValue(collection: "contacts", item: "contact", index: index, field: key, value: $0) })
    }
    private func contactDetail(_ index: Int, _ key: String) -> Binding<String> {
        Binding(get: { character.contactDetail(index, key: key) }, set: { value in
            do { try character.setContactDetail(index, key: key, value: value) } catch { errorMessage = error.localizedDescription }
        })
    }
}
