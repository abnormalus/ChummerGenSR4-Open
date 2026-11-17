import Foundation
#if canImport(FoundationXML)
import FoundationXML
#endif

public enum DossierError: LocalizedError {
    case invalidXML(String), unsupportedCharacter, invalidMetadata
    public var errorDescription: String? {
        switch self {
        case .invalidXML(let reason): return "This character XML could not be read: \(reason)"
        case .unsupportedCharacter: return "Open an SR4 Chummer character (.chum), rather than a print export or another edition."
        case .invalidMetadata: return "The dossier notes contain unreadable data. The file has not been changed."
        }
    }
}

// A value tree gives FileDocument snapshots independent ownership. Keep unknown elements,
// attributes, comments, processing instructions and mixed content instead of rebuilding a
// character from just the fields the sheet understands.
indirect enum XMLContent: Equatable {
    case element(CharacterElement), text(String), comment(String), cdata(String), instruction(String, String?)
    var xml: String {
        switch self {
        case .element(let node): return node.xml
        case .text(let text): return escapeXML(text)
        case .comment(let text): return "<!--\(text)-->"
        case .cdata(let text): return "<![CDATA[\(text)]]>"
        case .instruction(let target, let data): return "<?\(target)\(data.map { " " + $0 } ?? "")?>"
        }
    }
}

func escapeXML(_ text: String) -> String {
    text.replacingOccurrences(of: "&", with: "&amp;")
        .replacingOccurrences(of: "<", with: "&lt;")
        .replacingOccurrences(of: ">", with: "&gt;")
        .replacingOccurrences(of: "\"", with: "&quot;")
        .replacingOccurrences(of: "\r", with: "&#13;")
}

struct CharacterElement: Equatable {
    var name: String
    var attributes: [String: String] = [:]
    var content: [XMLContent] = []
    var text: String {
        content.map {
            switch $0 {
            case .text(let value), .cdata(let value): return value
            case .element(let node): return node.text
            default: return ""
            }
        }.joined()
    }
    var xml: String {
        let attrs = attributes.keys.sorted().map { " \($0)=\"\(escapeXML(attributes[$0]!))\"" }.joined()
        return "<\(name)\(attrs)>\(content.map(\.xml).joined())</\(name)>"
    }
    func children(_ name: String) -> [CharacterElement] {
        content.compactMap { if case .element(let node) = $0, node.name == name { return node }; return nil }
    }
    func child(_ name: String) -> CharacterElement? {
        for case .element(let node) in content where node.name == name { return node }
        return nil
    }
    func value(_ name: String) -> String { child(name)?.text ?? "" }
    mutating func update(_ name: String, matching: (CharacterElement) -> Bool = { _ in true },
                         _ body: (inout CharacterElement) -> Void) {
        if let index = content.firstIndex(where: {
            if case .element(let node) = $0 { return node.name == name && matching(node) }; return false
        }), case .element(var node) = content[index] {
            body(&node)
            content[index] = .element(node)
        } else {
            var node = CharacterElement(name: name)
            body(&node)
            content.append(.element(node))
        }
    }
    mutating func set(_ name: String, _ value: String) {
        update(name) { $0.content = [.text(value)] }
    }
    mutating func updateRow(_ name: String, index: Int, _ body: (inout CharacterElement) -> Void) {
        let indices = content.indices.filter {
            if case .element(let node) = content[$0] { return node.name == name }; return false
        }
        guard indices.indices.contains(index), case .element(var node) = content[indices[index]] else { return }
        body(&node)
        content[indices[index]] = .element(node)
    }
}

private final class TreeParser: NSObject, XMLParserDelegate {
    var stack: [CharacterElement] = []
    var document: [XMLContent] = []
    var failure: String?
    func append(_ content: XMLContent) {
        func appendMerged(_ content: XMLContent, to list: inout [XMLContent]) {
            // XMLParser can split the same text differently around entity references
            // and input-buffer boundaries. Those chunks are not document structure.
            if case .text(let text) = content, case .text(let previous) = list.last {
                list[list.count - 1] = .text(previous + text)
            } else { list.append(content) }
        }
        if stack.isEmpty { appendMerged(content, to: &document) }
        else { appendMerged(content, to: &stack[stack.count - 1].content) }
    }
    func parser(_ parser: XMLParser, didStartElement name: String, namespaceURI: String?, qualifiedName: String?, attributes: [String: String]) {
        guard stack.count < 128 else { failure = "XML nesting is too deep"; parser.abortParsing(); return }
        stack.append(CharacterElement(name: name, attributes: attributes))
    }
    func parser(_ parser: XMLParser, didEndElement name: String, namespaceURI: String?, qualifiedName: String?) {
        guard let node = stack.popLast() else { return }
        append(.element(node))
    }
    func parser(_ parser: XMLParser, foundCharacters text: String) { append(.text(text)) }
    func parser(_ parser: XMLParser, foundIgnorableWhitespace text: String) { append(.text(text)) }
    func parser(_ parser: XMLParser, foundComment text: String) { append(.comment(text)) }
    func parser(_ parser: XMLParser, foundCDATA data: Data) { append(.cdata(String(decoding: data, as: UTF8.self))) }
    func parser(_ parser: XMLParser, foundProcessingInstructionWithTarget target: String, data: String?) {
        append(.instruction(target, data))
    }
    func parser(_ parser: XMLParser, parseErrorOccurred error: Error) { if failure == nil { failure = error.localizedDescription } }
    func parser(_ parser: XMLParser, foundInternalEntityDeclarationWithName name: String, value: String?) {
        failure = "Entity declarations are unsupported"; parser.abortParsing()
    }
    func parser(_ parser: XMLParser, foundExternalEntityDeclarationWithName name: String, publicID: String?, systemID: String?) {
        failure = "External entities are unsupported"; parser.abortParsing()
    }
}

struct CharacterXML {
    var document: [XMLContent]
    var original: Data
    var changed = false
    var rootIndex: Int
    var root: CharacterElement {
        get { if case .element(let root) = document[rootIndex] { return root }; preconditionFailure() }
        set { document[rootIndex] = .element(newValue); changed = true }
    }
    init(data: Data) throws {
        guard data.count <= 32 * 1024 * 1024 else { throw DossierError.invalidXML("File exceeds 32 MB") }
        // Normalize only to inspect the declaration. XMLParser itself detects the source encoding.
        let decoded = String(data: data, encoding: .utf8) ?? String(data: data, encoding: .utf16) ?? ""
        guard !decoded.uppercased().contains("<!DOCTYPE") else { throw DossierError.invalidXML("Document type declarations are unsupported") }
        let parser = XMLParser(data: data), delegate = TreeParser()
        parser.delegate = delegate
        parser.shouldResolveExternalEntities = false
        parser.shouldProcessNamespaces = false
        guard parser.parse(), delegate.failure == nil else {
            throw DossierError.invalidXML(delegate.failure ?? "Malformed document")
        }
        guard let index = delegate.document.firstIndex(where: { if case .element = $0 { return true }; return false }),
              case .element(let root) = delegate.document[index], root.name == "character",
              root.child("attributes") != nil,
              root.value("gameedition").isEmpty || root.value("gameedition") == "SR4" else {
            throw DossierError.unsupportedCharacter
        }
        self.document = delegate.document; self.original = data; self.rootIndex = index
    }
    func data() -> Data {
        if !changed { return original }
        return Data(("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + document.map(\.xml).joined()).utf8)
    }
}
