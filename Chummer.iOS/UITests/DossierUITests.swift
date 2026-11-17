import XCTest

final class DossierUITests: XCTestCase {
    private func launch(theme: String, kind: String = "adept", page: String = "Equipment", secondCharacter: Bool = false, damageArchive: String = "", pdfStress: Bool = false, longVehicle: Bool = false) -> XCUIApplication {
        let app = XCUIApplication()
        let fixture = """
        <character><name>QA Runner</name><alias>QA Runner</alias><adept>\(kind == "adept")</adept><magician>\(kind == "magician")</magician><resenabled>\(kind == "technomancer")</resenabled>
        <attributes><attribute><name>STR</name><value>5</value></attribute><attribute><name>BOD</name><value>4</value></attribute><attribute><name>WIL</name><value>3</value></attribute></attributes>
        <totaless>2.78</totaless>
        <skills><skill><name>Positive skill</name><rating>1</rating><totalvalue>2</totalvalue></skill><skill><name>Zero skill</name><rating>0</rating><totalvalue>0</totalvalue></skill><skill><name>Negative skill</name><rating>0</rating><totalvalue>-1</totalvalue></skill></skills>
        <cyberwares><cyberware><name>Wired Reflexes</name><rating>2</rating></cyberware><cyberware><name>Trauma Damper</name><rating>0</rating></cyberware></cyberwares>
        <physicalcmfilled>0</physicalcmfilled><stuncmfilled>0</stuncmfilled>
        <weapons><weapon><name>Nodachi</name><damage>(STR/2+4)P</damage><ap>-2</ap><source>AR</source><page>14</page></weapon><weapon><guid>70000000-0000-0000-0000-000000000001</guid><name>QA pistol</name><type>Ranged</type><category>Heavy Pistols</category><damage>4P</damage><ap>-</ap><mode>SA</mode><ammo>10(c)</ammo><ammoremaining>0</ammoremaining></weapon><weapon><guid>70000000-0000-0000-0000-000000000003</guid><name>Grenade: Flash-Bang</name><category>Gear</category><ammo>0</ammo></weapon></weapons>
        <gears><gear><guid>10000000-0000-0000-0000-000000000001</guid><name>Regular rounds</name><category>Ammunition</category><extra>Heavy Pistols</extra><qty>30</qty></gear><gear><name>Grenade: Flash-Bang</name><category>Ammunition</category><qty>2</qty><weaponguid>70000000-0000-0000-0000-000000000003</weaponguid></gear></gears>
        <contacts><contact><name>QA Contact</name><connection>2</connection><loyalty>1</loyalty><notes></notes></contact></contacts>
        <powers><power><name>Attribute Boost</name><extra>AGI</extra><rating>4</rating><levels>True</levels></power></powers>
        <vehicles><vehicle><name>QA Drone</name><body>3</body><physicalcmfilled>0</physicalcmfilled></vehicle></vehicles>
        <improvements><improvement><improvementttype>InitiativePass</improvementttype><val>3</val><enabled>True</enabled><unique>initiativepass</unique></improvement></improvements>
        </character>
        """
        app.launchEnvironment["DOSSIER_UI_EXPORT_PDF"] = "1"
        let stressNotes = String(repeating: "Long contact text spanning pages. ", count: 700) + "END OF LONG CONTACT"
        var printableFixture = pdfStress ? fixture.replacingOccurrences(of: "<notes></notes>", with: "<notes>" + stressNotes + "</notes>") : fixture
        if longVehicle { printableFixture = printableFixture.replacingOccurrences(of: "QA Drone", with: "QA Drone - Extended Vehicle Model Name") }
        app.launchEnvironment["DOSSIER_UI_FIXTURE"] = Data(printableFixture.utf8).base64EncodedString()
        if secondCharacter {
            let second = fixture.replacingOccurrences(of: "QA Runner", with: "Second Runner").replacingOccurrences(of: "<attributes>", with: "<age>22</age><attributes>")
            app.launchEnvironment["DOSSIER_UI_SECOND_FIXTURE"] = Data(second.utf8).base64EncodedString()
        }
        app.launchEnvironment["DOSSIER_UI_DAMAGE_ARCHIVE"] = damageArchive
        app.launchEnvironment["DOSSIER_UI_THEME"] = theme
        app.launchEnvironment["DOSSIER_UI_PAGE"] = page
        app.launch()
        XCTAssertTrue(app.buttons["Share character"].waitForExistence(timeout: 15))
        return app
    }
    func testOfficialCharacterSheetPreviewAndExportChoices() {
        let app = launch(theme: "light", longVehicle: true)
        app.buttons["Share character"].tap()
        XCTAssertTrue(app.buttons["Print full character"].exists)
        XCTAssertTrue(app.buttons["Print official character sheet"].exists)
        app.buttons["Print official character sheet"].tap()
        XCTAssertTrue(app.buttons["Save PDF"].waitForExistence(timeout: 10))
        XCTAssertTrue(app.buttons["Print"].exists)
        XCTAssertTrue(app.navigationBars["Official SR4A Sheet"].exists)
        capture(app, "official-sr4a-preview")
    }
    func testPDFPaginationWithUnevenLongRows() {
        let app = launch(theme: "light", pdfStress: true)
        app.buttons["Share character"].tap()
        app.buttons["Print full character"].tap()
        XCTAssertTrue(app.buttons["Save PDF"].waitForExistence(timeout: 10))
        capture(app, "long-row-pdf-preview")
    }
    func testPrintableSheetPreviewSaveAndPrintControls() {
        let app = launch(theme: "light")
        app.buttons["Share character"].tap()
        app.buttons["Print full character"].tap()
        XCTAssertTrue(app.buttons["Save PDF"].waitForExistence(timeout: 10))
        XCTAssertTrue(app.buttons["Print"].exists)
        capture(app, "character-sheet-preview")
        app.buttons["Print"].tap()
        let printer = app.buttons.matching(NSPredicate(format: "label CONTAINS[c] %@", "Printer")).firstMatch
        XCTAssertTrue(printer.waitForExistence(timeout: 5) || app.staticTexts["Printer"].exists)
        capture(app, "system-print-options")
        // Never choose a printer or submit a print job during simulator tests.
    }
    func testPDFSavePickerAndReadableSnapshotSharing() {
        let app = launch(theme: "dark")
        app.buttons["Share character"].tap()
        XCTAssertTrue(app.buttons["Save Character Copy"].exists)
        XCTAssertTrue(app.buttons["Share Readable Snapshot"].exists)
        app.buttons["Print full character"].tap()
        app.buttons["Save PDF"].tap()
        XCTAssertTrue(app.buttons["Export"].waitForExistence(timeout: 5) || app.buttons["Save"].exists)
        capture(app, "pdf-save-picker")
        if app.buttons["Cancel"].exists { app.buttons["Cancel"].tap() }
        XCTAssertTrue(app.buttons["Save PDF"].waitForExistence(timeout: 5))
        app.buttons["Done"].tap()
        app.buttons["Share character"].tap()
        app.buttons["Share Readable Snapshot"].tap()
        XCTAssertTrue(app.buttons["Copy"].waitForExistence(timeout: 5) || app.staticTexts["Copy"].exists)
        capture(app, "readable-snapshot-share")
    }
    private func go(_ page: String, in app: XCUIApplication) {
        let chooser = app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "choose dossier page")).firstMatch
        if chooser.exists { chooser.tap(); app.buttons[page].tap() }
        else { app.tabBars.buttons[page].tap() }
    }
    func testUnreadableLibraryOffersExplicitSafeFreshStart() {
        let app = launch(theme: "light", page: "Character", damageArchive: "both")
        app.terminate()
        app.launchEnvironment["DOSSIER_UI_RESTORE"] = "1"
        app.launch()
        XCTAssertTrue(app.buttons["Keep Recovery Copies and Start Fresh"].waitForExistence(timeout: 10))
        app.buttons["Keep Recovery Copies and Start Fresh"].tap()
        XCTAssertTrue(app.buttons["Open Chummer Character"].waitForExistence(timeout: 5))
        capture(app, "recovered-empty-library")
    }
    func testFocusLossSwitchingAndBackgroundRelaunchPreserveEdits() {
        let app = launch(theme: "light", page: "Character", secondCharacter: true)
        let age = app.textFields["Age"]
        age.tap(); age.typeText("44")
        app.textFields["Sex"].tap()
        app.buttons["QA Runner, switch character"].tap()
        app.buttons["Second Runner"].tap()
        XCTAssertEqual(app.textFields["Age"].value as? String, "22")
        app.buttons["Second Runner, switch character"].tap()
        app.buttons["QA Runner"].tap()
        XCTAssertEqual(app.textFields["Age"].value as? String, "44")
        XCUIDevice.shared.press(.home)
        app.terminate()
        app.launchEnvironment["DOSSIER_UI_RESTORE"] = "1"
        app.launch()
        XCTAssertTrue(app.buttons["QA Runner, switch character"].waitForExistence(timeout: 10))
        XCTAssertEqual(app.textFields["Age"].value as? String, "44")
        app.buttons["QA Runner, switch character"].tap()
        app.buttons["Second Runner"].tap()
        XCTAssertEqual(app.textFields["Age"].value as? String, "22")
        capture(app, "restored-character-library")
    }
    func testDamageUpdatesWoundPenaltyAndSkillPools() {
        let app = launch(theme: "dark", page: "Combat")
        let box = app.buttons["Physical box 3"]
        XCTAssertTrue(box.waitForExistence(timeout: 5))
        for _ in 0..<3 { if box.isHittable { break }; app.swipeUp() }
        box.tap()
        XCTAssertTrue(app.staticTexts["CONDITION MONITOR · -1"].exists)
        go("Character", in: app)
        let filter = app.buttons["skills-positive-pool-filter"]
        for _ in 0..<6 { if filter.isHittable { break }; app.swipeUp() }
        XCTAssertEqual(app.textFields["Pool"].firstMatch.value as? String, "1")
        capture(app, "wounded-skill-pools")
    }
    private func capture(_ app: XCUIApplication, _ name: String) {
        let attachment = XCTAttachment(screenshot: app.screenshot())
        attachment.name = name; attachment.lifetime = .keepAlways; add(attachment)
    }
    func testEssenceAndSkillsHeaderFilter() {
        let app = launch(theme: "light", page: "Character")
        let essence = app.textFields["character-essence"]
        XCTAssertTrue(essence.waitForExistence(timeout: 5))
        XCTAssertEqual(essence.value as? String, "2.78")
        let filter = app.buttons["skills-positive-pool-filter"]
        for _ in 0..<6 { if filter.isHittable { break }; app.swipeUp() }
        XCTAssertTrue(filter.isHittable)
        XCTAssertEqual(filter.value as? String, "Off")
        XCTAssertTrue(app.staticTexts["Zero skill"].exists)
        let header = app.staticTexts["SKILLS"]
        XCTAssertLessThan(abs(filter.frame.midY - header.frame.midY), 5)
        filter.tap()
        XCTAssertEqual(filter.value as? String, "On")
        XCTAssertTrue(app.staticTexts["Positive skill"].exists)
        XCTAssertFalse(app.staticTexts["Zero skill"].exists)
        XCTAssertFalse(app.staticTexts["Negative skill"].exists)
        filter.tap()
        XCTAssertTrue(app.staticTexts["Zero skill"].exists)
        capture(app, "skills-header-filter")
    }
    func testGrenadeUsageAndAugmentationRatings() {
        let app = launch(theme: "dark")
        let use = app.buttons["Use Grenade: Flash-Bang"]
        XCTAssertTrue(use.waitForExistence(timeout: 5))
        let count = app.staticTexts["grenade-count-70000000-0000-0000-0000-000000000003"]
        XCTAssertEqual(count.label, "×2")
        use.tap()
        XCTAssertEqual(count.label, "×1")
        XCTAssertTrue(use.isEnabled)
        use.tap()
        XCTAssertEqual(count.label, "×0")
        XCTAssertFalse(use.isEnabled)
        capture(app, "grenade-used-up")
        for _ in 0..<8 {
            if app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "Wired Reflexes Rating 2")).firstMatch.isHittable { break }
            app.swipeUp()
        }
        XCTAssertTrue(app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "Wired Reflexes Rating 2")).firstMatch.exists)
        XCTAssertFalse(app.buttons.matching(NSPredicate(format: "label CONTAINS %@", "Trauma Damper Rating 0")).firstMatch.exists)
        capture(app, "augmentation-ratings")
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
        let stats = app.buttons["Stats"]
        XCTAssertLessThan(abs(stats.frame.midY - app.staticTexts["QA CONTACT"].frame.midY), 5)
        stats.tap()
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
