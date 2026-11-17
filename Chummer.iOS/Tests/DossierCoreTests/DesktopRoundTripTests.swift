import XCTest
@testable import DossierCore

/// Opt-in integration checks. Private character data and desktop outputs stay outside the repo.
final class DesktopRoundTripTests: XCTestCase {
    private func directory(phase: String) throws -> URL {
        let env = ProcessInfo.processInfo.environment
        guard env["CHUMMER_ROUNDTRIP_PHASE"] == phase, let path = env["CHUMMER_ROUNDTRIP_DIR"] else {
            throw XCTSkip("Opt-in desktop round-trip phase " + phase)
        }
        return URL(fileURLWithPath: path)
    }
    func testPrepareMobileEdits() throws {
        let dir = try directory(phase: "prepare")
        let fixture = try XCTUnwrap(ProcessInfo.processInfo.environment["CHUMMER_DIALTONE_FIXTURE"])
        let original = try Data(contentsOf: URL(fileURLWithPath: fixture))
        var character = try DossierCharacter(data: original)
        XCTAssertEqual(try character.exportData(), original)
        character.setValue("age", "37")
        character.setDamage(3, physical: true)
        character.setDamage(2, physical: false)
        let agility = try XCTUnwrap(character.attributes.first { $0.id == "AGI" })
        character.setAttribute("AGI", base: agility.base + 1)
        let program = try XCTUnwrap(character.programs.first { $0["name"] == "Blackout" })
        character.setGearValue(path: program.path, field: "equipped", value: "False")
        let weapon = try XCTUnwrap(character.rows(collection: "weapons", item: "weapon").first { $0["name"] == "Ares Viper Slivergun" })
        let ammo = try XCTUnwrap(character.ammunition(forWeapon: weapon).first)
        try character.spendAmmunition(weaponIndex: weapon.id, path: ammo.path, rounds: 3)
        try character.setPlainNotes(character.plainNotes + "\nDossier round-trip QA <&>\n")
        try character.setDossierField("history.Date first played", value: "2072-04-12")
        try character.setDossierField("matrixInitiativeModifier", value: "2")
        character.addContact()
        let contact = try XCTUnwrap(character.rows(collection: "contacts", item: "contact").last)
        character.setRowValue(collection: "contacts", item: "contact", index: contact.id, field: "name", value: "Dossier QA contact")
        try character.setContactDetail(contact.id, key: "professionalRating", value: "4")
        try character.addEquipment(PendingEquipment(name: "ares alp QA", category: "Ranged weapon", adjustments: ["rc": "2"]))
        try character.exportData().write(to: dir.appendingPathComponent("mobile-export.chum"), options: .atomic)
        print("Prepared mobile export with attribute, damage, ammo, program, history, contact and pending gear edits")
    }
    func testReimportDesktopSave() throws {
        let dir = try directory(phase: "verify")
        let mobile = try DossierCharacter(data: Data(contentsOf: dir.appendingPathComponent("mobile-export.chum")))
        for filename in ["desktop-saved.chum", "desktop-resaved.chum"] {
            let character = try DossierCharacter(data: Data(contentsOf: dir.appendingPathComponent(filename)))
            XCTAssertEqual(character.value("age"), "37", filename)
            XCTAssertEqual(character.damage(true), 3, filename)
            XCTAssertEqual(character.damage(false), 2, filename)
            XCTAssertEqual(character.attributes.first { $0.id == "AGI" }?.base, mobile.attributes.first { $0.id == "AGI" }?.base, filename)
            XCTAssertTrue(character.plainNotes.contains("Dossier round-trip QA <&>"), filename)
            XCTAssertEqual(character.dossierField("history.Date first played"), "2072-04-12", filename)
            XCTAssertEqual(character.dossierField("matrixInitiativeModifier"), "2", filename)
            XCTAssertEqual(character.notes.pendingEquipment, mobile.notes.pendingEquipment, filename)
            let contact = try XCTUnwrap(character.rows(collection: "contacts", item: "contact").first { $0["name"] == "Dossier QA contact" })
            XCTAssertEqual(character.contactDetail(contact.id, key: "professionalRating"), "4", filename)
            let flattened = character.gear.flatMap { [$0] + $0.descendants }
            let original = mobile.gear.flatMap { [$0] + $0.descendants }
            XCTAssertEqual(flattened.count, original.count, filename)
            XCTAssertEqual(Set(flattened.map { $0["guid"] }), Set(original.map { $0["guid"] }), filename)
            XCTAssertEqual(character.programs.first { $0["name"] == "Blackout" }?["equipped"].lowercased(), "false", filename)
            let ammoID = try XCTUnwrap(original.first { $0["name"] == "Ammo: Flechette Rounds" }?["guid"])
            XCTAssertEqual(flattened.first { $0["guid"] == ammoID }?["qty"], "97", filename)
            XCTAssertEqual(character.rows(collection: "weapons", item: "weapon").count, mobile.rows(collection: "weapons", item: "weapon").count, filename)
            // Re-export after desktop's recalculation remains accepted by the mobile validator.
            XCTAssertNoThrow(try character.exportData(), filename)
        }
    }
}
