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

    public init() {}

    public static func load(from url: URL) throws -> Self {
        guard FileManager.default.fileExists(atPath: url.path) else { return Self() }
        return try JSONDecoder().decode(Self.self, from: Data(contentsOf: url))
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
        try JSONEncoder().encode(next).write(to: url, options: .atomic)
        self = next
        return entryID
    }
}
