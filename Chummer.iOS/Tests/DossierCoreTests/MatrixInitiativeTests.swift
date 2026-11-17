import XCTest
@testable import DossierCore

final class MatrixInitiativeTests: XCTestCase {
    func testUpgradedActiveCommlinkBonusesAndLinkedIntuition() throws {
        var character = try DossierCharacter(data: Data("""
        <character><attributes><attribute><name>INT</name><value>4</value><totalvalue>5</totalvalue></attribute></attributes>
        <gears><gear><category>Commlink</category><active>True</active><response>3</response><children><gear><category>Commlink Upgrade</category><response>5</response></gear></children></gear>
        <gear><category>Commlink</category><active>False</active><response>9</response></gear></gears>
        <improvements><improvement><improvementttype>MatrixInitiative</improvementttype><enabled>True</enabled><val>1</val></improvement>
        <improvement><improvementttype>MatrixInitiativePass</improvementttype><unique>sim</unique><val>1</val></improvement>
        <improvement><improvementttype>MatrixInitiativePass</improvementttype><unique>sim</unique><val>2</val></improvement>
        <improvement><improvementttype>MatrixInitiative</improvementttype><enabled>False</enabled><val>99</val></improvement></improvements></character>
        """.utf8))
        XCTAssertEqual(character.matrixInitiative, 11)
        XCTAssertEqual(character.matrixInitiativePasses, 3)
        character.setAttribute("INT", base: 5)
        XCTAssertEqual(character.matrixInitiative, 12)
        try character.setDossierField("matrixResponse", value: "4")
        try character.setDossierField("matrixInitiativeModifier", value: "2")
        let restored = try DossierCharacter(data: character.exportData())
        XCTAssertEqual(restored.matrixInitiative, 13)
    }
    func testMissingResponseIsNotInventedAndTechnomancerUsesLivingPersona() throws {
        var character = try DossierCharacter(data: Data("<character><attributes><attribute><name>INT</name><value>4</value><totalvalue>4</totalvalue></attribute></attributes></character>".utf8))
        XCTAssertNil(character.matrixInitiative)
        character.setValue("resenabled", "True")
        XCTAssertEqual(character.matrixInitiative, 9)
        XCTAssertEqual(character.matrixInitiativePasses, 3)
    }
}
