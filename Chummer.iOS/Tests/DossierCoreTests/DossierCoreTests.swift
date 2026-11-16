import XCTest
@testable import DossierCore

final class DossierCoreTests: XCTestCase {
    private var sample: Data {
        Data("""
        <?xml version="1.0" encoding="utf-8"?>
        <?dossier preserve?>
        <character future="yes"><gameedition>SR4</gameedition><name>Runner &amp; Co</name>
        <!--Keep this--><custom xmlns:x="urn:test"><x:extension flag="a&amp;b"><![CDATA[<untouched>]]></x:extension></custom>
        <attributes>
        <attribute><name>AGI</name><value>4</value><totalvalue>6</totalvalue><augmodifier>2</augmodifier></attribute>
        <attribute><name>BOD</name><value>4</value><totalvalue>4</totalvalue></attribute>
        <attribute><name>WIL</name><value>3</value><totalvalue>3</totalvalue></attribute>
        </attributes><skills>
        <skill><name>Pistols</name><attribute>AGI</attribute><rating>3</rating><totalvalue>10</totalvalue></skill>
        <skill><name>Perception</name><attribute>INT</attribute><rating>2</rating><totalvalue>5</totalvalue></skill>
        </skills><physicalcmfilled>2</physicalcmfilled><stuncmfilled>1</stuncmfilled>
        <weapons><weapon><guid>existing-id</guid><name>Ares Alpha</name><rc>2</rc><accessories><accessory custom="true"/></accessories></weapon></weapons>
        <contacts><contact><name>Doc</name><connection>3</connection><loyalty>2</loyalty><notes>Trusted friend.</notes></contact></contacts>
        <notes>Existing notes &amp; details.</notes></character>
        """.utf8)
    }
    func testUneditedFileIsByteIdenticalIncludingUTF16() throws {
        for data in [sample, String(decoding: sample, as: UTF8.self)
            .replacingOccurrences(of: "encoding=\"utf-8\"", with: "encoding=\"utf-16\"").data(using: .utf16)!] {
            XCTAssertEqual(try DossierCharacter(data: data).data(), data)
        }
    }
    func testBaseAgilityFlowsIntoModifiedAttributeAndPools() throws {
        var character = try DossierCharacter(data: sample)
        character.setAttribute("AGI", base: 5)
        XCTAssertEqual(character.total("AGI"), 7)
        XCTAssertEqual(character.skills[0].pool, 11)
        XCTAssertEqual(character.skills[1].pool, 5)
        let reopened = try DossierCharacter(data: character.data())
        XCTAssertEqual(reopened.attributes.first?.base, 5)
        XCTAssertEqual(reopened.value("name"), "Runner & Co")
        XCTAssertEqual(reopened.skills[0].pool, 11)
    }
    func testManualPoolOffsetSurvivesLinkedEdits() throws {
        var character = try DossierCharacter(data: sample)
        character.setSkillPool(0, pool: 12)
        character.setAttribute("AGI", base: 5)
        character.setSkillRating(0, rating: 4)
        XCTAssertEqual(character.skills[0].pool, 14)
        character.setAttributeTotal("AGI", total: 8)
        XCTAssertEqual(character.skills[0].pool, 15)
        XCTAssertEqual(character.attributes[0].base, 5)
    }
    func testEditsPreserveUnknownXMLAndValueSnapshots() throws {
        let original = try DossierCharacter(data: sample)
        var edited = original
        edited.setValue("name", "A < B & \"C\"")
        edited.setDamage(6, physical: true)
        let result = try DossierCharacter(data: edited.data())
        XCTAssertEqual(original.value("name"), "Runner & Co")
        XCTAssertEqual(original.damage(true), 2)
        XCTAssertEqual(result.value("name"), "A < B & \"C\"")
        let before = try CharacterXML(data: sample), after = try CharacterXML(data: edited.data())
        XCTAssertEqual(before.root.child("custom"), after.root.child("custom"))
        XCTAssertEqual(before.root.child("weapons"), after.root.child("weapons"))
        XCTAssertEqual(before.root.attributes, after.root.attributes)
        XCTAssertTrue(String(decoding: edited.data(), as: UTF8.self).contains("<!--Keep this-->"))
        XCTAssertTrue(String(decoding: edited.data(), as: UTF8.self).contains("<?dossier preserve?>"))
    }
    func testContactDetailsAndPendingGearStayInSupportedNotes() throws {
        var character = try DossierCharacter(data: sample)
        try character.setContactDetail(0, key: "AGI", value: "4")
        try character.setContactNotes(0, text: "Updated contact notes")
        let gear = PendingEquipment(name: "ares alp", category: "Ranged weapon", stats: ["damage": "6P"], adjustments: ["rc": "2"])
        try character.addEquipment(gear)
        try character.setDossierField("history.Date created", value: "2072")
        let reopened = try DossierCharacter(data: character.data())
        XCTAssertEqual(reopened.contactDetail(0, key: "AGI"), "4")
        XCTAssertEqual(reopened.contactNotes(0), "Updated contact notes")
        XCTAssertEqual(reopened.notes.pendingEquipment, [gear])
        XCTAssertEqual(reopened.dossierField("history.Date created"), "2072")
        XCTAssertEqual(reopened.plainNotes, "Existing notes & details.")
        XCTAssertEqual(reopened.rows(collection: "weapons", item: "weapon").count, 1)
        // Simulate desktop retaining known notes and discarding unrecognized top-level fields.
        let raw = try CharacterXML(data: character.data())
        let desktopRewrite = Data(("<character><attributes/>" + raw.root.child("notes")!.xml + raw.root.child("contacts")!.xml + "</character>").utf8)
        XCTAssertEqual(try DossierCharacter(data: desktopRewrite).notes.pendingEquipment, [gear])
    }
    func testAddedContactHasDesktopRequiredFields() throws {
        var character = try DossierCharacter(data: sample)
        character.addContact()
        let contact = try XCTUnwrap(character.rows(collection: "contacts", item: "contact").last)
        for key in ["connection", "loyalty", "membership", "areaofinfluence", "magicalresources", "matrixresources", "colour"] {
            XCTAssertNotNil(Int(contact[key]), key)
        }
        XCTAssertEqual(contact["type"], "Contact")
        XCTAssertEqual(try DossierCharacter(data: character.data()).rows(collection: "contacts", item: "contact").count, 2)
    }
    func testDamageTapHoldAndExportOnlyDamage() throws {
        var track = DamageTrack(damage: 0, capacity: 10)
        XCTAssertEqual(track.state(at: 10), .blocked)
        track.tap(3); XCTAssertEqual(track.damage, 4)
        track.tap(6); XCTAssertEqual(track.damage, 7)
        track.tap(1); XCTAssertEqual(track.damage, 2)
        track.tap(1); XCTAssertEqual(track.damage, 1)
        track.tap(11); XCTAssertEqual(track.damage, 1)
        track.hold(10); XCTAssertEqual(track.state(at: 10), .undamaged)
        track.tap(10); XCTAssertEqual(track.damage, 11)
        track.hold(5); XCTAssertEqual(track.damage, 5)
        XCTAssertEqual(track.state(at: 5), .blocked)
        track.tap(8); XCTAssertEqual(track.damage, 5, "Damage cannot jump over an unavailable box")
        var character = try DossierCharacter(data: sample)
        character.setDamage(track.damage, physical: true)
        let text = String(decoding: character.data(), as: UTF8.self)
        XCTAssertFalse(text.contains("blocked"))
        XCTAssertEqual(try DossierCharacter(data: character.data()).damage(true), 5)
        XCTAssertEqual(try DossierCharacter(data: character.data()).damage(false), 1)
    }
    func testInvalidFilesAreRejected() throws {
        for data in [Data("<character>".utf8), Data("<html/>".utf8),
                     Data("<character><gameedition>SR5</gameedition><attributes/></character>".utf8),
                     Data("<!DOCTYPE character [<!ENTITY x 'bad'>]><character><attributes/></character>".utf8),
                     Data("<character><attributes/><notes>[CHUMMER-DOSSIER-V1]bad[/CHUMMER-DOSSIER-V1]</notes></character>".utf8)] {
            XCTAssertThrowsError(try DossierCharacter(data: data))
        }
    }
    func testInvalidNumericEditCannotBeExported() throws {
        var character = try DossierCharacter(data: sample)
        character.setValue("nuyen", "lots")
        XCTAssertThrowsError(try character.exportData())
        character.setValue("nuyen", "1000")
        XCTAssertNoThrow(try character.exportData())
    }
    func testDemoAndInitiativeLinks() throws {
        var character = try DossierCharacter.demo
        XCTAssertEqual(character.initiative, 6)
        character.setAttribute("REA", base: 4)
        try character.setDossierField("initiativeModifier", value: "2")
        XCTAssertEqual(character.initiative, 9)
        character.setAttribute("INT", base: 5)
        XCTAssertEqual(character.astralInitiative, 10)
        XCTAssertEqual(character.initiative, 11)
    }
    func testRepositoryCharacterCorpus() throws {
        guard let directory = ProcessInfo.processInfo.environment["CHUMMER_FIXTURE_ROOT"] else {
            throw XCTSkip("Set CHUMMER_FIXTURE_ROOT to Chummer.Mac/assets/saves for repository fixture coverage")
        }
        let enumerator = try XCTUnwrap(FileManager.default.enumerator(at: URL(fileURLWithPath: directory), includingPropertiesForKeys: nil))
        var count = 0
        for case let url as URL in enumerator where url.pathExtension.lowercased() == "chum" {
            let original = try Data(contentsOf: url)
            var character = try DossierCharacter(data: original)
            XCTAssertEqual(character.data(), original, url.lastPathComponent)
            let xml = try CharacterXML(data: original)
            character.setValue("name", "Round-trip <test> & runner")
            character.setDamage(3, physical: true)
            let reopened = try DossierCharacter(data: character.data())
            XCTAssertEqual(reopened.value("name"), "Round-trip <test> & runner")
            XCTAssertEqual(reopened.damage(true), 3)
            var after = try CharacterXML(data: character.data()).root
            // Restore only the edited nodes and compare the complete remaining value tree.
            after.set("name", xml.root.value("name"))
            after.set("physicalcmfilled", xml.root.value("physicalcmfilled"))
            var expected = xml.root
            expected.set("name", xml.root.value("name"))
            expected.set("physicalcmfilled", xml.root.value("physicalcmfilled"))
            XCTAssertEqual(after, expected, url.lastPathComponent)
            count += 1
        }
        XCTAssertGreaterThan(count, 0)
        print("Verified \(count) repository character files; originals were read only.")
    }
}
