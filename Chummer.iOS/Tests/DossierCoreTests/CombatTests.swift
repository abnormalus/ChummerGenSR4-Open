import XCTest
@testable import DossierCore

final class CombatTests: XCTestCase {
    private func character(_ body: String) throws -> DossierCharacter {
        try DossierCharacter(data: Data(("<character><attributes><attribute><name>STR</name><value>5</value></attribute></attributes>" + body + "</character>").utf8))
    }
    func testDamageResolvesAttributesRoundUpAndPreservesUnsupportedExpressions() throws {
        var runner = try character("<weapons><weapon><name>Nodachi</name><damage>(STR/2+4)P</damage></weapon></weapons>")
        var weapon = runner.rows(collection: "weapons", item: "weapon")[0]
        XCTAssertEqual(runner.weaponDamage(weapon), "7P")
        runner.setAttributeTotal("STR", total: 8)
        XCTAssertEqual(runner.weaponDamage(weapon), "8P")
        weapon.fields["damage"] = "min(STR,6)S(e)"
        XCTAssertEqual(runner.weaponDamage(weapon), "6S(e)")
        for raw in ["Special", "(STR/0)P", "(Rating+2)P", "as round", "100000000000000000000P"] {
            weapon.fields["damage"] = raw
            XCTAssertEqual(runner.weaponDamage(weapon), raw)
        }
        XCTAssertEqual(runner.rows(collection: "weapons", item: "weapon")[0]["damage"], "(STR/2+4)P")
    }
    func testPassesUseEnabledPowerAndCyberwareUniqueGroups() throws {
        let runner = try character("""
        <improvements>
        <improvement><improvementttype>InitiativePass</improvementttype><unique>initiativepass</unique><val>3</val><enabled>True</enabled><improvementsource>Power</improvementsource></improvement>
        <improvement><improvementttype>InitiativePass</improvementttype><unique>initiativepass</unique><val>2</val><enabled>True</enabled><improvementsource>Cyberware</improvementsource></improvement>
        <improvement><improvementttype>InitiativePass</improvementttype><val>9</val><enabled>False</enabled></improvement>
        <improvement><improvementttype>InitiativePassAdd</improvementttype><val>1</val><enabled>True</enabled></improvement>
        </improvements>
        """)
        XCTAssertEqual(runner.initiativePasses, 5)
        XCTAssertEqual(try character("").initiativePasses, 1)
    }
    func testOptionalBoxesCannotCreateHolesOrEraseDamageAndPenaltiesStopAtCapacity() {
        var track = DamageTrack(damage: 2, capacity: 10)
        for index in 0..<8 { track.hold(index) }
        XCTAssertEqual(track.capacity, 10)
        XCTAssertEqual(track.damage, 2)
        XCTAssertEqual(track.penalty(at: 2), -1)
        XCTAssertEqual(track.penalty(at: 5), -2)
        XCTAssertEqual(track.penalty(at: 8), -3)
        XCTAssertNil(track.penalty(at: 11))
        track.hold(13)
        XCTAssertEqual(track.capacity, 14)
        XCTAssertEqual(track.penalty(at: 11), -4)
        track.tap(12)
        track.hold(9)
        XCTAssertEqual(track.capacity, 14, "Blocking cannot delete marked damage")
        track.tap(3); track.hold(9)
        XCTAssertEqual(track.capacity, 9)
        XCTAssertEqual(track.state(at: 8), .undamaged)
        XCTAssertEqual(track.state(at: 9), .blocked)
        let overflow = DamageTrack(damage: 22, capacity: 10)
        XCTAssertEqual(overflow.damage, 22)
        XCTAssertEqual(overflow.boxCount, 22)
    }
    func testCharacterKindsPowerSummaryAndVehicleDamagePreserveSavedStructure() throws {
        var runner = try character("""
        <adept>True</adept><magician>False</magician><resenabled>False</resenabled>
        <powers><power><name>Attribute Boost</name><extra>AGI</extra><rating>4</rating><levels>True</levels></power></powers>
        <vehicles><vehicle><guid>one</guid><name>Drone</name><body>3</body><physicalcmfilled>1</physicalcmfilled><mods><mod><name>Armor</name></mod></mods></vehicle>
        <vehicle><guid>two</guid><body>14</body><physicalcmfilled>0</physicalcmfilled></vehicle></vehicles>
        """)
        XCTAssertTrue(runner.isAdept); XCTAssertFalse(runner.isMagician); XCTAssertFalse(runner.isTechnomancer)
        var mage = runner; mage.setValue("magician", "True"); XCTAssertTrue(mage.isMagician)
        var techno = runner; techno.setValue("resenabled", "True"); XCTAssertTrue(techno.isTechnomancer)
        XCTAssertEqual(runner.powerSummary(runner.rows(collection: "powers", item: "power")[0]), "Attribute Boost (AGI) 4")
        let before = try CharacterXML(data: runner.data())
        XCTAssertEqual(runner.vehicleCapacity(runner.rows(collection: "vehicles", item: "vehicle")[0]), 10)
        runner.setVehicleDamage(6, index: 0)
        let reopened = try DossierCharacter(data: runner.exportData())
        XCTAssertEqual(reopened.vehicleDamage(reopened.rows(collection: "vehicles", item: "vehicle")[0]), 6)
        XCTAssertEqual(reopened.vehicleDamage(reopened.rows(collection: "vehicles", item: "vehicle")[1]), 0)
        let after = try CharacterXML(data: reopened.data())
        XCTAssertEqual(before.root.child("vehicles")?.children("vehicle")[0].child("mods"), after.root.child("vehicles")?.children("vehicle")[0].child("mods"))
    }
    func testProjectionCachesInvalidateWithoutChangingPriorSnapshots() throws {
        var runner = try character("<gears><gear><name>Analyze</name><category>Matrix Programs</category><equipped>True</equipped></gear></gears>")
        let old = runner
        _ = runner.attributes; _ = runner.gear; _ = runner.programs; _ = runner.skills; _ = runner.notes; _ = runner.data()
        let token = runner.revision
        runner.setValue("name", "Changed")
        XCTAssertNotEqual(runner.revision, token)
        let changed = runner.revision
        runner.setValue("name", "Changed")
        XCTAssertEqual(runner.revision, changed)
        runner.setGearValue(path: [0], field: "equipped", value: "False")
        XCTAssertEqual(runner.programs[0]["equipped"], "False")
        XCTAssertEqual(old.programs[0]["equipped"], "True")
        XCTAssertEqual(old.value("name"), "")
        XCTAssertEqual(try DossierCharacter(data: runner.data()).value("name"), "Changed")
    }
    func testPrivateBanditAndReadWorkload() throws {
        guard let path = ProcessInfo.processInfo.environment["CHUMMER_BANDIT"] else { throw XCTSkip("Optional private device-report fixture") }
        var runner = try DossierCharacter(data: Data(contentsOf: URL(fileURLWithPath: path)))
        XCTAssertTrue(runner.isAdept); XCTAssertFalse(runner.isMagician)
        XCTAssertEqual(runner.initiativePasses, 4)
        let weapons = runner.rows(collection: "weapons", item: "weapon")
        let nodachi = try XCTUnwrap(weapons.first { $0["name"] == "Nodachi" })
        print("Bandit base Nodachi DV: \(runner.weaponDamage(nodachi)); passes: \(runner.initiativePasses)")
        runner.setVehicleDamage(4, index: 0)
        if let output = ProcessInfo.processInfo.environment["CHUMMER_BANDIT_EXPORT"] { try runner.exportData().write(to: URL(fileURLWithPath: output)) }
        let start = Date()
        for _ in 0..<100 {
            _ = runner.data(); _ = runner.skills; _ = runner.gear; _ = runner.programs; _ = runner.notes
            for skill in runner.skills { _ = runner.skills.first { $0.id == skill.id } }
        }
        print("100 edited-character read workloads: \(Date().timeIntervalSince(start))s")
    }
}
