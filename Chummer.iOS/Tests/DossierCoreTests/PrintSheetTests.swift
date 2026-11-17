import XCTest
@testable import DossierCore

final class PrintSheetTests: XCTestCase {
    func testPrintableValuesUseCurrentSheetAndHidePrivateMetadata() throws {
        var runner = try DossierCharacter(data: Data("""
        <character><alias>Print QA</alias><attributes><attribute><name>STR</name><value>5</value></attribute><attribute><name>MAG</name><value>4</value></attribute></attributes><totaless>2.78</totaless>
        <weapons><weapon><name>Test Blade</name><damage>(STR/2+4)P</damage><ap>-2</ap><source>AR</source><page>14</page></weapon></weapons>
        <gears><gear><name>Commlink</name><category>Commlink</category><children><gear><name>Analyze</name><rating>4</rating><equipped>True</equipped><category>Matrix Programs</category><children><gear><name>Optimization</name><rating>2</rating><category>Program Options</category></gear></children></gear></children></gear></gears>
        <cyberwares><cyberware><name>Cybereyes</name><rating>2</rating><children><cyberware><name>Vision Enhancement</name><rating>3</rating></cyberware></children></cyberware></cyberwares>
        <skills><skill><name>English</name><rating>0</rating><totalvalue>0</totalvalue><skillcategory>Language</skillcategory><knowledge>True</knowledge></skill></skills>
        <contacts><contact><name>Friend</name><connection>3</connection><loyalty>2</loyalty></contact></contacts><notes>Visible notes</notes></character>
        """.utf8))
        runner.setDamage(3, physical: true)
        try runner.setDossierField("history.Date first played", value: "2072-04-12")
        try runner.setContactDetail(0, key: "BOD", value: "4")
        let before = runner.data()
        let sheet = runner.printableSheet
        XCTAssertTrue(sheet.text.contains("2.78"))
        XCTAssertTrue(sheet.text.contains("English (Native)"))
        XCTAssertTrue(sheet.text.contains("Vision Enhancement"))
        XCTAssertTrue(sheet.text.contains("Rating 3"))
        XCTAssertTrue(sheet.text.contains("7P"))
        XCTAssertTrue(sheet.text.contains("AR p. 14"))
        XCTAssertTrue(sheet.text.contains("Visible notes"))
        XCTAssertTrue(sheet.text.contains("BOD: 4"))
        XCTAssertTrue(sheet.text.contains("2072-04-12"))
        XCTAssertFalse(sheet.text.contains("CHUMMER-DOSSIER"))
        XCTAssertFalse(sheet.sections.first { $0.title == "Attributes" }!.rows.joined().contains("MAG"))
        let programs = try XCTUnwrap(sheet.sections.first { $0.title == "Programs" })
        XCTAssertEqual(programs.rows[0][0], "Analyze · Optimization 2")
        XCTAssertEqual(programs.rows[0][2], "Yes")
        XCTAssertFalse(sheet.sections.first { $0.title == "Equipment / Commlinks" }!.rows.joined().contains("Analyze"))
        XCTAssertEqual(sheet.monitors[0].damage, 3)
        XCTAssertEqual(runner.data(), before)
    }
    func testRoleSpecificSectionsAndLongTextRemainComplete() throws {
        var runner = try DossierCharacter(data: Data("""
        <character><attributes/><magician>True</magician><adept>True</adept><powers><power><name>Attribute Boost</name><extra>AGI</extra><levels>True</levels><rating>4</rating></power></powers><spells><spell><name>QA Spell</name></spell></spells><spirits><spirit><name>QA Spirit</name></spirit></spirits><complexforms><complexform><name>Hidden Form</name></complexform></complexforms></character>
        """.utf8))
        let long = String(repeating: "A long background paragraph with <tags> & Unicode café.\n", count: 400) + "END OF BACKGROUND"
        runner.setValue("background", long)
        let sheet = runner.printableSheet
        XCTAssertEqual(sheet.sections.first { $0.title == "Background" }?.rows[0][0], long)
        XCTAssertTrue(sheet.text.contains("Attribute Boost (AGI) 4"))
        XCTAssertTrue(sheet.text.contains("QA Spell"))
        XCTAssertTrue(sheet.text.contains("QA Spirit"))
        XCTAssertFalse(sheet.text.contains("Hidden Form"))
        runner.setValue("magician", "False"); runner.setValue("adept", "False"); runner.setValue("resenabled", "True")
        let technomancer = runner.printableSheet
        XCTAssertFalse(technomancer.text.contains("QA Spell"))
        XCTAssertTrue(technomancer.text.contains("Hidden Form"))
        XCTAssertTrue(technomancer.sections.contains { $0.title == "Sprites" })
    }
}
