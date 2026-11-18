import XCTest
@testable import DossierCore

final class TableAuditTests: XCTestCase {
    func testPrivateDesktopComparison() throws {
        let env = ProcessInfo.processInfo.environment
        guard let root = env["DOSSIER_AUDIT_INPUT"], let expectedRoot = env["DOSSIER_AUDIT_EXPECTED"] else {
            throw XCTSkip("Optional private desktop-engine comparison")
        }
        var differences: [String] = []
        func numericTotal(_ text: String) -> String {
            let segments = text.components(separatedBy: CharacterSet(charactersIn: "0123456789-").inverted).filter { !$0.isEmpty }
            return segments.last ?? text
        }
        for filename in ["Dialtone", "Bandit", "Willow", "Blue Smoke", "Mongoose-Ready to play"] {
            let runner = try DossierCharacter(data: Data(contentsOf: URL(fileURLWithPath: root).appendingPathComponent(filename + ".chum")))
            let data = try Data(contentsOf: URL(fileURLWithPath: expectedRoot).appendingPathComponent(filename + ".json"))
            let expected = try XCTUnwrap(JSONSerialization.jsonObject(with: data) as? [String: Any])
            func compare(_ label: String, _ actual: String, _ expected: String) {
                if actual != expected { differences.append(filename + ": " + label + " mobile=" + actual + " desktop=" + expected) }
            }
            compare("initiative", String(runner.initiative), numericTotal(expected["initiative"] as! String))
            compare("passes", String(runner.initiativePasses), numericTotal(expected["passes"] as! String))
            compare("matrix", runner.matrixInitiative.map(String.init) ?? "unavailable", numericTotal(expected["matrix"] as! String))
            compare("physical", String(runner.trackCapacity(true)), String(expected["physical"] as! Int))
            compare("stun", String(runner.trackCapacity(false)), String(expected["stun"] as! Int))
            for attribute in expected["attributes"] as! [[String: Any]] {
                let name = attribute["name"] as! String
                compare(name, String(runner.total(name)), String(attribute["value"] as! Int))
            }
            for weapon in expected["weapons"] as! [[String: Any]] {
                let name = weapon["name"] as! String
                let row = try XCTUnwrap(runner.rows(collection: "weapons", item: "weapon").first { $0["name"] == name })
                compare(name + " damage", runner.weaponDamage(row), weapon["damage"] as! String)
                compare(name + " AP", runner.weaponAP(row), weapon["ap"] as! String)
            }
            for skill in expected["skills"] as! [[String: Any]] {
                let name = skill["name"] as! String
                if let row = runner.skills.first(where: { $0.name == name }) {
                    compare(name + " pool", String(row.pool), String(skill["pool"] as! Int))
                }
            }
        }
        print("Desktop audit differences (\(differences.count)):\n" + differences.joined(separator: "\n"))
        XCTAssertTrue(differences.isEmpty, differences.joined(separator: "\n"))
    }
}
