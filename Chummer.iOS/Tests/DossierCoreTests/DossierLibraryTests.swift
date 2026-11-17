import XCTest
@testable import DossierCore

final class DossierLibraryTests: XCTestCase {
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
