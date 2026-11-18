import XCTest
@testable import DossierCore

final class DossierLibraryTests: XCTestCase {
    func testDamagedPrimaryRecoversBackupAndRetainsDamagedBytesOnRepair() throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: directory) }
        let url = directory.appendingPathComponent("library.json")
        var library = DossierLibrary()
        var runner = try DossierCharacter.demo
        runner.setValue("age", "30")
        let id = try library.save(runner, filename: "runner", id: nil, to: url)
        runner.setValue("age", "31")
        try library.save(runner, filename: "runner", id: id, to: url)
        let damaged = Data("interrupted archive".utf8)
        try damaged.write(to: url)
        var recovered = try DossierLibrary.load(from: url)
        XCTAssertTrue(recovered.recoveredFromBackup)
        XCTAssertEqual(try DossierCharacter(data: recovered.entries[0].data).value("age"), "30")
        XCTAssertEqual(try Data(contentsOf: url), damaged, "Loading must not replace damaged bytes")
        var restored = try DossierCharacter(data: recovered.entries[0].data)
        restored.setValue("age", "32")
        try recovered.save(restored, filename: "runner", id: id, to: url)
        XCTAssertFalse(recovered.recoveredFromBackup)
        let retained = try FileManager.default.contentsOfDirectory(at: directory, includingPropertiesForKeys: nil).filter { $0.lastPathComponent.hasPrefix("library-damaged-") }
        XCTAssertEqual(retained.count, 1)
        XCTAssertEqual(try Data(contentsOf: retained[0]), damaged)
        let clean = try DossierLibrary.load(from: url)
        XCTAssertFalse(clean.recoveredFromBackup)
        XCTAssertEqual(try DossierCharacter(data: clean.entries[0].data).value("age"), "32")
    }
    func testMissingPrimaryAndInvalidCharacterPayloadCanRecover() throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: directory) }
        let url = directory.appendingPathComponent("library.json")
        var library = DossierLibrary()
        try library.save(.demo, filename: "runner", id: nil, to: url)
        let valid = try Data(contentsOf: url)
        try FileManager.default.removeItem(at: url)
        XCTAssertTrue(try DossierLibrary.load(from: url).recoveredFromBackup)
        var archive = try XCTUnwrap(JSONSerialization.jsonObject(with: valid) as? [String: Any])
        var entries = try XCTUnwrap(archive["entries"] as? [[String: Any]])
        entries[0]["data"] = Data("not character XML".utf8).base64EncodedString()
        archive["entries"] = entries
        try JSONSerialization.data(withJSONObject: archive).write(to: url)
        XCTAssertTrue(try DossierLibrary.load(from: url).recoveredFromBackup)
    }
    func testSameNamedImportsRemainIndependentAndHaveDistinctMenuLabels() throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: directory) }
        let url = directory.appendingPathComponent("library.json")
        var library = DossierLibrary()
        let runner = try DossierCharacter.demo
        let first = try library.save(runner, filename: "same", id: nil, to: url)
        let second = try library.save(runner, filename: "same", id: nil, to: url)
        XCTAssertNotEqual(first, second)
        XCTAssertEqual(Set(library.entries.map { library.displayName(for: $0) }).count, 2)
        var edited = runner; edited.setValue("age", "42")
        try library.save(edited, filename: "same", id: first, to: url)
        let restored = try DossierLibrary.load(from: url)
        XCTAssertEqual(try DossierCharacter(data: restored.entries.first { $0.id == second }!.data).value("age"), runner.value("age"))
    }
    func testExplicitFreshStartRetainsBothUnreadableArchives() throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: directory) }
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        let url = directory.appendingPathComponent("library.json")
        let primary = Data("bad primary".utf8), backup = Data("bad backup".utf8)
        try primary.write(to: url); try backup.write(to: url.appendingPathExtension("backup"))
        XCTAssertThrowsError(try DossierLibrary.load(from: url))
        let empty = try DossierLibrary.startFreshPreservingArchives(at: url)
        XCTAssertTrue(empty.entries.isEmpty)
        XCTAssertFalse(try DossierLibrary.load(from: url).recoveredFromBackup)
        let retained = try XCTUnwrap(FileManager.default.contentsOfDirectory(at: directory, includingPropertiesForKeys: nil).first { $0.lastPathComponent.hasPrefix("Recovery-") })
        XCTAssertEqual(try Data(contentsOf: retained.appendingPathComponent("library.json")), primary)
        XCTAssertEqual(try Data(contentsOf: retained.appendingPathComponent("library.json.backup")), backup)
    }
    func testMultipleCharactersRestoreEditsAndSelection() throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: directory) }
        let url = directory.appendingPathComponent("library.json")
        var library = try DossierLibrary.load(from: url)
        var first = try DossierCharacter.demo
        first.setValue("alias", "First")
        let firstID = try library.save(first, filename: "first", id: nil, to: url)
        var second = try DossierCharacter.demo
        second.setValue("alias", "Second")
        let secondID = try library.save(second, filename: "second", id: nil, to: url)
        // Preserve unfinished input even when it cannot yet be exported to desktop.
        first.setValue("karma", "unfinished")
        XCTAssertThrowsError(try first.exportData())
        try library.save(first, filename: "first", id: firstID, to: url)
        let restored = try DossierLibrary.load(from: url)
        XCTAssertEqual(restored.entries.count, 2)
        XCTAssertEqual(restored.selectedID, firstID)
        XCTAssertEqual(try DossierCharacter(data: restored.entries.first { $0.id == firstID }!.data).value("karma"), "unfinished")
        XCTAssertEqual(try DossierCharacter(data: restored.entries.first { $0.id == secondID }!.data).title, "Second")
    }

    func testFailedSaveRetainsPreviousArchiveAndSelection() throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: directory) }
        let url = directory.appendingPathComponent("library.json")
        var library = DossierLibrary()
        let firstID = try library.save(.demo, filename: "first", id: nil, to: url)
        let original = try Data(contentsOf: url)
        // A regular file cannot serve as a parent directory.
        XCTAssertThrowsError(try library.save(.demo, filename: "second", id: nil, to: url.appendingPathComponent("blocked")))
        XCTAssertEqual(library.entries.count, 1)
        XCTAssertEqual(library.selectedID, firstID)
        XCTAssertEqual(try Data(contentsOf: url), original)
    }

    func testCorruptArchiveIsNotSilentlyReplaced() throws {
        let url = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: url) }
        let corrupt = Data("broken archive".utf8)
        try corrupt.write(to: url)
        XCTAssertThrowsError(try DossierLibrary.load(from: url))
        XCTAssertEqual(try Data(contentsOf: url), corrupt)
    }
}
