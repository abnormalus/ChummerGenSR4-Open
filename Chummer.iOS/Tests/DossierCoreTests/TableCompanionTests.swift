import XCTest
@testable import DossierCore

final class TableCompanionTests: XCTestCase {
    private var sample: Data { Data("""
    <character><attributes><attribute><name>STR</name><value>5</value><totalvalue>7</totalvalue></attribute></attributes>
    <karma>12</karma><totalkarma>40</totalkarma><nuyen>500</nuyen>
    <skills><skill><name>Blades</name><attribute>STR</attribute><rating>3</rating><totalvalue>10</totalvalue></skill></skills>
    <weapons><weapon><guid>70000000-0000-0000-0000-000000000001</guid><name>Test gun</name><type>Ranged</type><category>Heavy Pistols</category><damage>5P</damage><ap>-1</ap><ammo>10(c)</ammo><ammoremaining>0</ammoremaining>
    <weaponmods><weaponmod><installed>True</installed><dvbonus>1</dvbonus><apbonus>-1</apbonus></weaponmod><weaponmod><installed>False</installed><dvbonus>99</dvbonus><apbonus>-99</apbonus></weaponmod></weaponmods></weapon></weapons>
    <gears><gear><guid>10000000-0000-0000-0000-000000000001</guid><name>Regular rounds</name><category>Ammunition</category><extra>Heavy Pistols</extra><qty>50</qty></gear>
    <gear><name>Case</name><rating>1</rating><children><gear><guid>10000000-0000-0000-0000-000000000002</guid><name>Shock rounds</name><category>Ammunition</category><extra>Heavy Pistols</extra><qty>20</qty><weaponbonus><damagereplace>6S(e)</damagereplace><apreplace>-half</apreplace></weaponbonus></gear></children></gear></gears>
    </character>
    """.utf8) }
    func testManualChangesNeverSpendKarmaOrNuyenAndNestedEditsStayOnSelectedItem() throws {
        var runner = try DossierCharacter(data: sample)
        runner.setAttribute("STR", base: 6)
        runner.setSkillRating(0, rating: 4)
        runner.setSkillPool(0, pool: 15)
        runner.setGearValue(path: [1, 0], field: "rating", value: "4")
        let saved = try DossierCharacter(data: runner.exportData())
        XCTAssertEqual(saved.attributes[0].base, 6)
        XCTAssertEqual(saved.total("STR"), 8)
        XCTAssertEqual(saved.skills[0].rating, 4)
        XCTAssertEqual(saved.skills[0].pool, 15)
        XCTAssertEqual(saved.gear(at: [1, 0])?["rating"], "4")
        XCTAssertEqual(saved.gear(at: [1])?["rating"], "1")
        XCTAssertEqual(saved.value("karma"), "12"); XCTAssertEqual(saved.value("totalkarma"), "40"); XCTAssertEqual(saved.value("nuyen"), "500")
    }
    func testReloadFireSwitchUnloadAndFailureAreConservativeAndAtomic() throws {
        var runner = try DossierCharacter(data: sample)
        try runner.reloadAmmunition(weaponIndex: 0, path: [0], slot: 1, rounds: 10)
        XCTAssertEqual(runner.gear(at: [0])?["qty"], "40")
        XCTAssertEqual(runner.weaponDamage(runner.rows(collection: "weapons", item: "weapon")[0]), "6P")
        try runner.spendLoadedAmmunition(weaponIndex: 0, slot: 1, rounds: 3)
        XCTAssertEqual(runner.gear(at: [0])?["qty"], "40", "Fire does not double-charge the reserve")
        try runner.reloadAmmunition(weaponIndex: 0, path: [1, 0], slot: 1, rounds: 8)
        XCTAssertEqual(runner.gear(at: [0])?["qty"], "47", "Remaining old rounds return to their lot")
        XCTAssertEqual(runner.gear(at: [1, 0])?["qty"], "12")
        let weapon = runner.rows(collection: "weapons", item: "weapon")[0]
        XCTAssertEqual(runner.weaponDamage(weapon), "6S(e)")
        XCTAssertEqual(runner.weaponAP(weapon), "-half")
        let before = runner.data()
        XCTAssertThrowsError(try runner.reloadAmmunition(weaponIndex: 0, path: [0], slot: 1, rounds: 999))
        XCTAssertEqual(runner.data(), before)
        try runner.unloadAmmunition(weaponIndex: 0, slot: 1)
        XCTAssertEqual(runner.gear(at: [1, 0])?["qty"], "20")
        let reopened = try DossierCharacter(data: runner.exportData())
        XCTAssertEqual(reopened.rows(collection: "weapons", item: "weapon")[0]["ammoremaining"], "0")
        XCTAssertEqual(reopened.weaponDamage(reopened.rows(collection: "weapons", item: "weapon")[0]), "6P")
    }
    func testMagazineSelectionAndFlechetteBonusesDoNotDoubleApply() throws {
        var runner = try DossierCharacter(data: sample)
        try runner.reloadAmmunition(weaponIndex: 0, path: [0], slot: 1, rounds: 10)
        try runner.reloadAmmunition(weaponIndex: 0, path: [1, 0], slot: 2, rounds: 5)
        var weapon = runner.rows(collection: "weapons", item: "weapon")[0]
        XCTAssertEqual(runner.activeAmmoSlot(for: weapon), 2)
        XCTAssertEqual(runner.weaponDamage(weapon), "6S(e)")
        try runner.spendLoadedAmmunition(weaponIndex: 0, slot: 1, rounds: 1)
        weapon = runner.rows(collection: "weapons", item: "weapon")[0]
        XCTAssertEqual(runner.activeAmmoSlot(for: weapon), 1)
        XCTAssertEqual(runner.weaponDamage(weapon), "6P")
        XCTAssertEqual(weapon["ammoremaining2"], "5")

        let raw = String(decoding: sample, as: UTF8.self)
            .replacingOccurrences(of: "<damagereplace>6S(e)</damagereplace>", with: "<damage>2</damage><damagetype>P(f)</damagetype>")
            .replacingOccurrences(of: "<apreplace>-half</apreplace>", with: "<ap>5</ap>")
        var flechette = try DossierCharacter(data: Data(raw.utf8))
        flechette.setRowValue(collection: "weapons", item: "weapon", index: 0, field: "damage", value: "8P(f)")
        flechette.setRowValue(collection: "weapons", item: "weapon", index: 0, field: "ap", value: "+5")
        try flechette.reloadAmmunition(weaponIndex: 0, path: [1, 0], slot: 1, rounds: 5)
        weapon = flechette.rows(collection: "weapons", item: "weapon")[0]
        XCTAssertEqual(flechette.weaponDamage(weapon), "9P(f)", "Only the installed mod adds DV to a flechette-native weapon")
        XCTAssertEqual(flechette.weaponAP(weapon), "+4")
    }
    func testExplicitWeaponDisplayEditsAreStoredSeparatelyFromBaseAndBonuses() throws {
        var runner = try DossierCharacter(data: sample)
        let weapon = runner.rows(collection: "weapons", item: "weapon")[0]
        try runner.setWeaponAdjustment(weapon, field: "damage", value: "9P")
        try runner.setWeaponAdjustment(weapon, field: "ap", value: "-3")
        let saved = try DossierCharacter(data: runner.exportData())
        XCTAssertEqual(saved.weaponDamage(weapon), "9P")
        XCTAssertEqual(saved.weaponAP(weapon), "-3")
        XCTAssertEqual(saved.rows(collection: "weapons", item: "weapon")[0]["damage"], "5P")
        try runner.setWeaponAdjustment(weapon, field: "damage", value: "")
        XCTAssertEqual(runner.weaponDamage(weapon), "6P")
    }
    func testPrivateManualEditsDesktopRoundTrip() throws {
        let env = ProcessInfo.processInfo.environment
        guard let input = env["DOSSIER_TABLE_INPUT"], let output = env["DOSSIER_TABLE_OUTPUT"] else { throw XCTSkip("Optional private desktop integration") }
        let original = try DossierCharacter(data: Data(contentsOf: URL(fileURLWithPath: input)))
        if env["DOSSIER_TABLE_VERIFY"] == "1" {
            let mobile = try DossierCharacter(data: Data(contentsOf: URL(fileURLWithPath: output).appendingPathComponent("mobile.chum")))
            let saved = try DossierCharacter(data: Data(contentsOf: URL(fileURLWithPath: output).appendingPathComponent("desktop.chum")))
            let mobileWeapon = try XCTUnwrap(mobile.rows(collection: "weapons", item: "weapon").first { $0["name"] == "FN 5-7C" })
            let savedWeapon = try XCTUnwrap(saved.rows(collection: "weapons", item: "weapon").first { $0["name"] == "FN 5-7C" })
            XCTAssertEqual(savedWeapon["ammoremaining"], "10")
            XCTAssertEqual(savedWeapon["ammoloaded"], mobileWeapon["ammoloaded"])
            XCTAssertEqual(saved.loadedAmmunition(for: savedWeapon)?["qty"], mobile.loadedAmmunition(for: mobileWeapon)?["qty"])
            XCTAssertEqual(saved.weaponDamage(savedWeapon), "9P")
            XCTAssertEqual(saved.attributes.first { $0.id == "AGI" }?.base, mobile.attributes.first { $0.id == "AGI" }?.base)
            XCTAssertEqual(saved.skills.first?.rating, mobile.skills.first?.rating)
            XCTAssertEqual(saved.value("karma"), original.value("karma")); XCTAssertEqual(saved.value("nuyen"), original.value("nuyen"))
        } else {
            var mobile = original
            let weapon = try XCTUnwrap(mobile.rows(collection: "weapons", item: "weapon").first { $0["name"] == "FN 5-7C" })
            let reserve = try XCTUnwrap(mobile.loadedAmmunition(for: weapon))
            try mobile.reloadAmmunition(weaponIndex: weapon.id, path: reserve.path, slot: 1, rounds: 10)
            try mobile.setWeaponAdjustment(weapon, field: "damage", value: "9P")
            mobile.setAttribute("AGI", base: try XCTUnwrap(mobile.attributes.first { $0.id == "AGI" }).base + 1)
            mobile.setSkillRating(0, rating: mobile.skills[0].rating + 1)
            try FileManager.default.createDirectory(atPath: output, withIntermediateDirectories: true)
            try mobile.exportData().write(to: URL(fileURLWithPath: output).appendingPathComponent("mobile.chum"))
        }
    }

}
