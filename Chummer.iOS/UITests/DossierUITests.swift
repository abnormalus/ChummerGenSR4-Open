import XCTest

final class DossierUITests: XCTestCase {
    private func launch(theme: String, kind: String = "adept", page: String = "Equipment") -> XCUIApplication {
        let app = XCUIApplication()
        let fixture = """
        <character><name>QA Runner</name><alias>QA Runner</alias><adept>\(kind == "adept")</adept><magician>\(kind == "magician")</magician><resenabled>\(kind == "technomancer")</resenabled>
        <attributes><attribute><name>STR</name><value>5</value></attribute><attribute><name>BOD</name><value>4</value></attribute><attribute><name>WIL</name><value>3</value></attribute></attributes>
        <physicalcmfilled>0</physicalcmfilled><stuncmfilled>0</stuncmfilled>
        <weapons><weapon><name>Nodachi</name><damage>(STR/2+4)P</damage><ap>-2</ap><source>AR</source><page>14</page></weapon><weapon><guid>70000000-0000-0000-0000-000000000001</guid><name>QA pistol</name><type>Ranged</type><category>Heavy Pistols</category><damage>4P</damage><ap>-</ap><mode>SA</mode><ammo>10(c)</ammo><ammoremaining>0</ammoremaining></weapon></weapons>
        <gears><gear><guid>10000000-0000-0000-0000-000000000001</guid><name>Regular rounds</name><category>Ammunition</category><extra>Heavy Pistols</extra><qty>30</qty></gear></gears>
        <contacts><contact><name>QA Contact</name><connection>2</connection><loyalty>1</loyalty><notes></notes></contact></contacts>
        <powers><power><name>Attribute Boost</name><extra>AGI</extra><rating>4</rating><levels>True</levels></power></powers>
        <vehicles><vehicle><name>QA Drone</name><body>3</body><physicalcmfilled>0</physicalcmfilled></vehicle></vehicles>
        <improvements><improvement><improvementttype>InitiativePass</improvementttype><val>3</val><enabled>True</enabled><unique>initiativepass</unique></improvement></improvements>
        </character>
        """
        app.launchEnvironment["DOSSIER_UI_FIXTURE"] = Data(fixture.utf8).base64EncodedString()
        app.launchEnvironment["DOSSIER_UI_THEME"] = theme
        app.launchEnvironment["DOSSIER_UI_PAGE"] = page
        app.launch()
        XCTAssertTrue(app.buttons["Save Copy"].waitForExistence(timeout: 15))
        return app
    }
    private func capture(_ app: XCUIApplication, _ name: String) {
        let attachment = XCTAttachment(screenshot: app.screenshot())
        attachment.name = name; attachment.lifetime = .keepAlways; add(attachment)
    }
    func testEquipmentInBothThemesAndReadablePowerLabels() {
        for theme in ["light", "dark"] {
            let app = launch(theme: theme)
            XCTAssertTrue(app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "Nodachi")).firstMatch.waitForExistence(timeout: 5))
            XCTAssertTrue(app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "7P · AP -2")).firstMatch.exists)
            capture(app, "equipment-\(theme)")
            for _ in 0..<8 {
                if app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "Attribute Boost (AGI) 4")).firstMatch.isHittable { break }
                app.swipeUp()
            }
            XCTAssertTrue(app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "Attribute Boost (AGI) 4")).firstMatch.exists)
            capture(app, "powers-\(theme)")
            app.terminate()
        }
    }
    func testCombatDamageAndVehicleControls() {
        let app = launch(theme: "dark", page: "Combat")
        XCTAssertTrue(app.staticTexts.matching(NSPredicate(format: "label CONTAINS %@", "· 4 passes")).firstMatch.exists)
        app.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.75))
            .press(forDuration: 0.05, thenDragTo: app.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.5)))
        let box = app.buttons["Physical box 3"]
        XCTAssertTrue(box.waitForExistence(timeout: 5))
        if box.frame.midY < 150 {
            app.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.4))
                .press(forDuration: 0.05, thenDragTo: app.coordinate(withNormalizedOffset: CGVector(dx: 0.5, dy: 0.65)))
        }
        box.tap()
        XCTAssertEqual(box.value as? String, "Damage")
        box.press(forDuration: 0.7)
        XCTAssertEqual(box.value as? String, "Damage", "First eight cannot be blocked")
        capture(app, "combat-health-dark")
        for _ in 0..<8 {
            if app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "QA Drone")).firstMatch.isHittable { break }
            app.swipeUp()
        }
        let drone = app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "QA Drone")).firstMatch
        XCTAssertTrue(drone.exists)
        drone.tap()
        XCTAssertTrue(app.buttons["Vehicle damage box 3"].waitForExistence(timeout: 5))
        let vehicleBox = app.buttons["Vehicle damage box 3"]
        vehicleBox.tap()
        XCTAssertEqual(vehicleBox.value as? String, "Damage")
        capture(app, "vehicle-damage-dark")
    }
    func testCompactContactsAndNoPurchasingFlow() {
        let app = launch(theme: "light", page: "Contacts")
        XCTAssertTrue(app.textFields["Connection"].exists)
        XCTAssertTrue(app.textFields["Loyalty"].exists)
        XCTAssertFalse(app.textFields["BOD"].exists)
        app.buttons["Stats"].tap()
        XCTAssertTrue(app.textFields["BOD"].waitForExistence(timeout: 5))
        XCTAssertFalse(app.textFields["Professional rating"].exists)
        app.buttons["Stats"].tap()
        XCTAssertFalse(app.textFields["BOD"].exists)
        capture(app, "compact-contacts")
    }
    func testReloadFireAndUnloadConserveRounds() {
        let app = launch(theme: "dark")
        XCTAssertFalse(app.buttons["Add Equipment"].exists)
        app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "Reserve 30")).firstMatch.tap()
        app.buttons["Reload / change ammo…"].tap()
        XCTAssertTrue(app.buttons["Load Magazine"].waitForExistence(timeout: 5))
        app.buttons["Load Magazine"].tap()
        let loaded = app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "Magazine 1 · Regular rounds · 10 rounds")).firstMatch
        XCTAssertTrue(loaded.waitForExistence(timeout: 5))
        for _ in 0..<4 {
            if loaded.isHittable { break }
            app.swipeUp()
        }
        loaded.tap()
        app.buttons["Fire 1"].tap()
        app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "Magazine 1 · Regular rounds · 9 rounds")).firstMatch.tap()
        app.buttons["Reload / change ammo…"].tap()
        app.buttons["Unload Magazine"].tap()
        XCTAssertTrue(app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "Reserve 29")).firstMatch.waitForExistence(timeout: 5))
        capture(app, "reloaded-fired-unloaded")
    }
    func testCharacterSpecificSections() {
        for kind in ["adept", "magician", "technomancer"] {
            let app = launch(theme: "light", kind: kind)
            var labels = Set<String>()
            for _ in 0..<8 {
                labels.formUnion(app.staticTexts.allElementsBoundByIndex.map(\.label))
                app.swipeUp()
            }
            let text = labels.joined(separator: "|")
            if kind == "technomancer" { XCTAssertTrue(text.contains("COMPLEX FORMS")); XCTAssertTrue(text.contains("SPRITES")) }
            else { XCTAssertFalse(text.contains("COMPLEX FORMS")); XCTAssertFalse(text.contains("SPRITES")) }
            if kind == "adept" { XCTAssertFalse(text.contains("SPIRITS")) }
            app.terminate()
        }
    }
}
