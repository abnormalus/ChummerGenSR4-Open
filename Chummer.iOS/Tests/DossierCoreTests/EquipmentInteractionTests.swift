import XCTest
@testable import DossierCore

final class EquipmentInteractionTests: XCTestCase {
    private var sample: Data { Data("""
    <character><attributes/><weapons>
    <weapon><name>First</name><type>Ranged</type><category>Heavy Pistols</category><mode>SA/BF</mode><ammoremaining>8</ammoremaining><ammoremaining2>4</ammoremaining2></weapon>
    <weapon><name>Second</name><type>Ranged</type><category>Heavy Pistols</category><mode>SA</mode></weapon>
    <weapon><name>Club</name><type>Melee</type><category>Clubs</category></weapon>
    </weapons><gears>
    <gear><name>Rounds</name><category>Ammunition</category><extra>Heavy Pistols</extra><qty>100</qty></gear>
    <gear><name>Wrong rounds</name><category>Ammunition</category><extra>Light Pistols</extra><qty>200</qty></gear>
    <gear><name>Link</name><category>Commlink</category><children>
    <gear><name>Zulu</name><category>Matrix Programs</category><equipped>True</equipped><children><gear><name>Ergonomic</name><category>Program Options</category></gear></children></gear>
    <gear><name>Alpha</name><category>Matrix Programs</category><equipped>False</equipped></gear>
    </children></gear></gears></character>
    """.utf8) }

    func testAmmoCompatibilitySharedReserveAndNoNegativeConsumption() throws {
        var character = try DossierCharacter(data: sample)
        let weapons = character.rows(collection: "weapons", item: "weapon")
        XCTAssertEqual(character.ammunition(forWeapon: weapons[0]).map { $0["name"] }, ["Rounds"])
        XCTAssertTrue(character.ammunition(forWeapon: weapons[2]).isEmpty)
        try character.spendAmmunition(weaponIndex: 0, path: [0], rounds: 3)
        try character.spendAmmunition(weaponIndex: 1, path: [0], rounds: 2)
        XCTAssertEqual(character.gear[0]["qty"], "95")
        let prior = character.data()
        XCTAssertThrowsError(try character.spendAmmunition(weaponIndex: 0, path: [0], rounds: 100))
        XCTAssertThrowsError(try character.spendAmmunition(weaponIndex: 0, path: [1], rounds: 1))
        XCTAssertEqual(character.data(), prior)
        XCTAssertEqual(character.suggestedFireCounts(for: weapons[0]), [1, 2, 3, 6])
        XCTAssertEqual(character.suggestedFireCounts(for: weapons[1]), [1, 2])
        let restored = try DossierCharacter(data: character.exportData())
        XCTAssertEqual(restored.gear[0]["qty"], "95")
    }
    func testLoadedMagazineDoesNotSpendReservesAgain() throws {
        var character = try DossierCharacter(data: sample)
        try character.spendLoadedAmmunition(weaponIndex: 0, slot: 2, rounds: 3)
        XCTAssertEqual(character.rows(collection: "weapons", item: "weapon")[0]["ammoremaining2"], "1")
        XCTAssertEqual(character.rows(collection: "weapons", item: "weapon")[0]["ammoremaining"], "8")
        XCTAssertEqual(character.gear[0]["qty"], "100")
        XCTAssertThrowsError(try character.spendLoadedAmmunition(weaponIndex: 0, slot: 2, rounds: 2))
    }
    func testAlphabeticalProgramsToggleOriginalNestedInstance() throws {
        var character = try DossierCharacter(data: sample)
        XCTAssertEqual(character.programs.map { $0["name"] }, ["Alpha", "Zulu"])
        let alpha = character.programs[0]
        character.setGearValue(path: alpha.path, field: "equipped", value: "True")
        let restored = try DossierCharacter(data: character.exportData())
        XCTAssertEqual(restored.programs.map { $0["name"] }, ["Alpha", "Zulu"])
        XCTAssertEqual(restored.gear[2].children[1]["equipped"], "True")
        XCTAssertEqual(restored.gear[2].children[0].children[0]["name"], "Ergonomic")
        let previous = character.data()
        character.setGearValue(path: [99, 2], field: "equipped", value: "False")
        XCTAssertEqual(character.data(), previous)
    }
}
