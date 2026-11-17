import Foundation

/// App-owned working copies. One atomic archive keeps selection and character data together.
/// Drafts use data(), not exportData(), so incomplete numeric edits remain recoverable.
public struct DossierLibrary: Codable {
    public struct Entry: Codable, Identifiable {
        public let id: UUID
        public var filename: String
        public var data: Data
        public var title: String
    }
    public private(set) var entries: [Entry] = []
    public private(set) var selectedID: UUID?

    public private(set) var recoveredFromBackup = false
    private var persistedData: Data?
    private enum CodingKeys: String, CodingKey { case entries, selectedID }

    public init() {}

    public func displayName(for entry: Entry) -> String {
        let copies = entries.filter { $0.title == entry.title }
        guard copies.count > 1, let index = copies.firstIndex(where: { $0.id == entry.id }) else { return entry.title }
        return entry.title + " · Copy " + String(index + 1)
    }

    private static func read(_ url: URL) throws -> Self {
        let bytes = try Data(contentsOf: url)
        var library = try JSONDecoder().decode(Self.self, from: bytes)
        library.persistedData = bytes
        guard Set(library.entries.map(\.id)).count == library.entries.count,
              library.selectedID == nil || library.entries.contains(where: { $0.id == library.selectedID }) else {
            throw CocoaError(.fileReadCorruptFile)
        }
        for entry in library.entries { _ = try DossierCharacter(data: entry.data) }
        return library
    }
    private static func backupURL(for url: URL) -> URL { url.appendingPathExtension("backup") }


    public static func load(from url: URL) throws -> Self {
        let backup = backupURL(for: url)
        guard FileManager.default.fileExists(atPath: url.path) || FileManager.default.fileExists(atPath: backup.path) else { return Self() }
        do { return try read(url) }
        catch {
            let primaryError = error
            guard var recovered = try? read(backup) else { throw primaryError }
            recovered.recoveredFromBackup = true
            return recovered
        }
    }

    /// Explicit recovery action: retain both archives before creating an empty
    /// library. Failure leaves the existing files and caller's state intact.
    public static func startFreshPreservingArchives(at url: URL) throws -> Self {
        let manager = FileManager.default
        let recovery = url.deletingLastPathComponent().appendingPathComponent("Recovery-" + UUID().uuidString)
        try manager.createDirectory(at: recovery, withIntermediateDirectories: true)
        for archive in [url, backupURL(for: url)] where manager.fileExists(atPath: archive.path) {
            try Data(contentsOf: archive).write(to: recovery.appendingPathComponent(archive.lastPathComponent), options: .atomic)
        }
        var empty = Self()
        let data = try JSONEncoder().encode(empty)
        try data.write(to: url, options: .atomic)
        empty.persistedData = data
        return empty
    }

    @discardableResult
    public mutating func save(_ character: DossierCharacter, filename: String, id: UUID?, to url: URL) throws -> UUID {
        var next = self
        let entryID = id ?? UUID()
        let entry = Entry(id: entryID, filename: filename, data: character.data(), title: character.title)
        if let index = next.entries.firstIndex(where: { $0.id == entryID }) {
            next.entries[index] = entry
        } else {
            next.entries.append(entry)
        }
        next.selectedID = entryID
        try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
        let encoded = try JSONEncoder().encode(next)
        let backup = Self.backupURL(for: url)
        if FileManager.default.fileExists(atPath: url.path) {
            let previous = try Data(contentsOf: url)
            if previous == persistedData || (try? Self.read(url)) != nil {
                try previous.write(to: backup, options: .atomic)
            } else {
                // Never replace the damaged bytes with a repair until a copy exists.
                let damaged = url.deletingLastPathComponent().appendingPathComponent("library-damaged-" + UUID().uuidString + ".json")
                try previous.write(to: damaged, options: .atomic)
            }
        } else if !FileManager.default.fileExists(atPath: backup.path) {
            try encoded.write(to: backup, options: .atomic)
        }
        try encoded.write(to: url, options: .atomic)
        next.recoveredFromBackup = false
        next.persistedData = encoded
        self = next
        return entryID
    }
}
