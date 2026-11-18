import Foundation

public struct PendingEquipment: Codable, Equatable, Identifiable {
    public var id: UUID
    public var name: String
    public var category: String
    public var stats: [String: String]
    public var adjustments: [String: String]
    public init(name: String, category: String, stats: [String: String] = [:], adjustments: [String: String] = [:]) {
        id = UUID(); self.name = name; self.category = category
        self.stats = stats; self.adjustments = adjustments
    }
}

/// A versioned envelope inside a supported notes field survives older desktop saves.
/// Unknown schema versions are rejected before edits, rather than silently overwritten.
public struct DossierNotes: Codable, Equatable {
    public var version = 1
    public var fields: [String: String] = [:]
    public var poolModifiers: [String: Int] = [:]
    public var pendingEquipment: [PendingEquipment] = []
    public var contactDetails: [String: [String: String]] = [:]
    public init() {}
    static let start = "[CHUMMER-DOSSIER-V1]"
    static let end = "[/CHUMMER-DOSSIER-V1]"
    static func split(_ notes: String) throws -> (String, DossierNotes) {
        guard let startRange = notes.range(of: start) else { return (notes, DossierNotes()) }
        guard let endRange = notes.range(of: end, range: startRange.upperBound..<notes.endIndex),
              notes.range(of: start, range: startRange.upperBound..<notes.endIndex) == nil,
              let data = Data(base64Encoded: String(notes[startRange.upperBound..<endRange.lowerBound]).trimmingCharacters(in: .whitespacesAndNewlines)),
              let result = try? JSONDecoder().decode(DossierNotes.self, from: data), result.version == 1 else {
            throw DossierError.invalidMetadata
        }
        var plain = notes
        plain.removeSubrange(startRange.lowerBound..<endRange.upperBound)
        return (plain, result)
    }
    func merged(with notes: String) throws -> String {
        let encoder = JSONEncoder(); encoder.outputFormatting = [.sortedKeys]
        let payload = try encoder.encode(self).base64EncodedString()
        return notes + Self.start + "\n" + payload + "\n" + Self.end
    }
}
