import XCTest
@testable import DossierCore

final class CommlinkTests: XCTestCase {
    func testNestedProgramsPreserveHierarchyAndIndependentIdentities() throws {
        let data = Data("""
        <character><attributes/><gears><gear><name>Link</name><category>Commlink</category><response>4</response><children>
        <gear><name>Analyze</name><category>Matrix Programs</category><rating>5</rating><children><gear><name>Ergonomic</name><category>Program Options</category></gear></children></gear>
        </children></gear><gear><name>Analyze</name><category>Matrix Programs</category><rating>3</rating></gear></gears></character>
        """.utf8)
        var character = try DossierCharacter(data: data)
        XCTAssertEqual(character.gear[0].children[0]["rating"], "5")
        XCTAssertEqual(character.gear[0].children[0].children[0]["name"], "Ergonomic")
        XCTAssertNotEqual(character.gear[0].children[0].id, character.gear[1].id)
        XCTAssertNil(character.gear[0].fields["children"])
        XCTAssertEqual(try character.exportData(), data)
        character.setValue("age", "37")
        let restored = try DossierCharacter(data: character.exportData())
        XCTAssertEqual(restored.gear[0].descendants.count, 2)
        XCTAssertEqual(restored.gear[1]["rating"], "3")
    }

    func testDialtoneProgramsWhenPrivateFixtureIsProvided() throws {
        guard let path = ProcessInfo.processInfo.environment["CHUMMER_DIALTONE_FIXTURE"] else {
            throw XCTSkip("Set CHUMMER_DIALTONE_FIXTURE to the private character path")
        }
        let data = try Data(contentsOf: URL(fileURLWithPath: path))
        var character = try DossierCharacter(data: data)
        let all = character.gear.flatMap { [$0] + $0.descendants }
        let matrix = all.filter { $0["category"] == "Matrix Programs" }
        XCTAssertEqual(Set(matrix.map { $0["name"] }), Set(["Attack", "Armor", "Analyze", "Blackout", "Biofeedback Filter", "Medic", "Browse", "Command", "Edit", "Encrypt", "Purge", "Scan", "Reality Filter", "Exploit", "Stealth"]))
        XCTAssertEqual(matrix.count, 15)
        XCTAssertEqual(all.filter { $0["category"] == "Sensor Software" }.map { $0["name"] }, ["Empathy Software"])
        let commlink = try XCTUnwrap(character.gear.first { $0["name"] == "Fairlight Caliban" })
        XCTAssertEqual(character.matrixResponse, 5)
        XCTAssertEqual(character.matrixInitiative, 12)
        XCTAssertEqual(character.matrixInitiativePasses, 3)
        XCTAssertEqual(commlink.children.count, 13)
        XCTAssertEqual(commlink["response"], "4")
        XCTAssertEqual(commlink["signal"], "5")
        XCTAssertTrue(commlink.children.contains { $0["name"] == "Response 5" })
        XCTAssertEqual(commlink.children.filter { $0["category"] == "Matrix Programs" }.count, 3)
        XCTAssertEqual(try character.exportData(), data)
        character.setValue("age", "37")
        let reimported = try DossierCharacter(data: character.exportData())
        XCTAssertEqual(reimported.gear.flatMap { [$0] + $0.descendants }.filter { $0["category"] == "Matrix Programs" }.count, 15)
    }
}
