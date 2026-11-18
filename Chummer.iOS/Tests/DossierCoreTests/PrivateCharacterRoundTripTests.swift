import XCTest
@testable import DossierCore

/// The private fixture directory and generated exports are never repository assets.
final class PrivateCharacterRoundTripTests: XCTestCase {
    private func cases(phase: String) throws -> [(URL, URL)] {
        let env = ProcessInfo.processInfo.environment
        guard env["CHUMMER_PRIVATE_PHASE"] == phase,
              let input = env["CHUMMER_PRIVATE_FIXTURE_DIR"], let output = env["CHUMMER_PRIVATE_OUTPUT_DIR"] else {
            throw XCTSkip("Opt-in private character phase " + phase)
        }
        return try FileManager.default.contentsOfDirectory(at: URL(fileURLWithPath: input), includingPropertiesForKeys: nil)
            .filter { $0.pathExtension.lowercased() == "chum" }.sorted { $0.path < $1.path }.map {
                ($0, URL(fileURLWithPath: output).appendingPathComponent($0.deletingPathExtension().lastPathComponent))
            }
    }
    func testPrepareAdditionalCharacters() throws {
        let fixtures = try cases(phase: "prepare")
        XCTAssertGreaterThanOrEqual(fixtures.count, 3)
        for (source, directory) in fixtures {
            try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
            let bytes = try Data(contentsOf: source)
            var character = try DossierCharacter(data: bytes)
            XCTAssertEqual(try character.exportData(), bytes, source.lastPathComponent)
            character.setValue("age", "37")
            character.setDamage(3, physical: true)
            character.setDamage(2, physical: false)
            if let agility = character.attributes.first(where: { $0.id == "AGI" }) { character.setAttribute("AGI", base: agility.base + 1) }
            if let program = character.programs.first { character.setGearValue(path: program.path, field: "equipped", value: "False") }
            try character.setPlainNotes(character.plainNotes + "\nPrivate-fixture round-trip QA <&>\n")
            try character.setDossierField("history.Date first played", value: "2072-04-12")
            character.addContact()
            let contact = try XCTUnwrap(character.rows(collection: "contacts", item: "contact").last)
            character.setRowValue(collection: "contacts", item: "contact", index: contact.id, field: "name", value: "Dossier QA contact")
            try character.setContactDetail(contact.id, key: "professionalRating", value: "4")
            try character.addEquipment(PendingEquipment(name: "QA custom equipment", category: "Gear", adjustments: ["rc": "2"]))
            try character.exportData().write(to: directory.appendingPathComponent("mobile-export.chum"), options: .atomic)
            print("Prepared " + source.lastPathComponent)
        }
    }
    func testReimportAdditionalCharacters() throws {
        for (source, directory) in try cases(phase: "verify") {
            let mobile = try DossierCharacter(data: Data(contentsOf: directory.appendingPathComponent("mobile-export.chum")))
            let control = try DossierCharacter(data: Data(contentsOf: directory.appendingPathComponent("control/desktop-saved.chum")))
            for filename in ["desktop-saved.chum", "desktop-resaved.chum"] {
                let context = source.lastPathComponent + ": " + filename
                let desktop = try DossierCharacter(data: Data(contentsOf: directory.appendingPathComponent(filename)))
                XCTAssertEqual(desktop.value("age"), "37", context)
                XCTAssertEqual(desktop.damage(true), 3, context)
                XCTAssertEqual(desktop.damage(false), 2, context)
                XCTAssertEqual(desktop.attributes.first { $0.id == "AGI" }?.base, mobile.attributes.first { $0.id == "AGI" }?.base, context)
                XCTAssertTrue(desktop.plainNotes.contains("Private-fixture round-trip QA <&>"), context)
                XCTAssertEqual(desktop.notes.pendingEquipment, mobile.notes.pendingEquipment, context)
                XCTAssertEqual(desktop.dossierField("history.Date first played"), "2072-04-12", context)
                let contact = try XCTUnwrap(desktop.rows(collection: "contacts", item: "contact").first { $0["name"] == "Dossier QA contact" })
                XCTAssertEqual(desktop.contactDetail(contact.id, key: "professionalRating"), "4", context)
                for (collection, item) in [("weapons", "weapon"), ("armors", "armor"), ("gears", "gear"), ("cyberwares", "cyberware"), ("spells", "spell"), ("powers", "power"), ("contacts", "contact"), ("vehicles", "vehicle")] {
                    XCTAssertEqual(desktop.rows(collection: collection, item: item).count, mobile.rows(collection: collection, item: item).count, context + ": " + collection)
                }
                let before = mobile.gear.flatMap { [$0] + $0.descendants }
                let after = desktop.gear.flatMap { [$0] + $0.descendants }
                XCTAssertEqual(before.count, after.count, context)
                XCTAssertEqual(Set(before.map { $0["guid"] }), Set(after.map { $0["guid"] }), context)
                // Desktop loads can add catalog default skills even without mobile edits.
                // Compare to an independent unedited control and retain every original skill.
                XCTAssertEqual(desktop.skills.count, control.skills.count, context)
                XCTAssertEqual(Set(desktop.skills.map(\.name)), Set(control.skills.map(\.name)), context)
                XCTAssertTrue(Set(mobile.skills.map(\.name)).isSubset(of: Set(desktop.skills.map(\.name))), context)
                if let program = mobile.programs.first {
                    XCTAssertEqual(desktop.programs.first { $0["guid"] == program["guid"] }?["equipped"].lowercased(), "false", context)
                }
                XCTAssertNoThrow(try desktop.exportData(), context)
            }
        }
    }
}
