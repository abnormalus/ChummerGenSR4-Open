import SwiftUI
import UniformTypeIdentifiers
import DossierCore

extension UTType {
    static let chummerCharacter = UTType(exportedAs: "org.chummer.sr4.character", conformingTo: .xml)
}

struct CharacterFile: FileDocument {
    static var readableContentTypes: [UTType] { [.chummerCharacter, .xml] }
    var data: Data
    init(data: Data) { self.data = data }
    init(configuration: ReadConfiguration) throws {
        guard let data = configuration.file.regularFileContents else { throw CocoaError(.fileReadCorruptFile) }
        self.data = data
    }
    func fileWrapper(configuration: WriteConfiguration) throws -> FileWrapper { FileWrapper(regularFileWithContents: data) }
}

@main
struct ChummerDossierApp: App {
    var body: some Scene { WindowGroup { DossierHome() } }
}

struct DossierHome: View {
    @State private var character: DossierCharacter?
    @State private var filename = "Character"
    @State private var importing = false
    @State private var exporting = false
    @State private var exportFile: CharacterFile?
    @State private var isDemo = false
    @State private var errorMessage: String?

    var body: some View {
        NavigationStack {
            Group {
                if let snapshot = character {
                    DossierView(character: Binding(get: { character ?? snapshot }, set: { character = $0 }))
                        .safeAreaInset(edge: .top) {
                            if isDemo { Text("Demo character · Open a Chummer file to save your changes.")
                                    .font(.caption).padding(8).frame(maxWidth: .infinity).background(.thinMaterial) }
                        }
                } else {
                    ContentUnavailableView {
                        Label("Chummer Dossier", systemImage: "person.text.rectangle")
                    } description: {
                        Text("Your runner, on a familiar sheet. Open an SR4 Chummer character from Files.")
                    } actions: {
                        Button("Open Chummer Character") { importing = true }.buttonStyle(.borderedProminent)
                        Button("Explore a Demo") {
                            do { character = try .demo; isDemo = true } catch { errorMessage = error.localizedDescription }
                        }
                    }
                }
            }
            .navigationTitle(character?.title ?? "Chummer Dossier")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItemGroup(placement: .topBarTrailing) {
                    Button("Open", systemImage: "folder") { importing = true }
                    Button("Save Copy", systemImage: "square.and.arrow.up") {
                        do {
                            if let character { exportFile = CharacterFile(data: try character.exportData()); exporting = true }
                        } catch { errorMessage = error.localizedDescription }
                    }
                        .disabled(character == nil || isDemo)
                }
            }
        }
        .tint(SheetTheme.ink)
        .fileImporter(isPresented: $importing, allowedContentTypes: [.chummerCharacter, .xml]) { result in
            do { openCharacter(try result.get()) } catch { errorMessage = error.localizedDescription }
        }
        .onOpenURL { openCharacter($0) }
        .fileExporter(isPresented: $exporting, document: exportFile,
                      contentType: .chummerCharacter, defaultFilename: filename + " - Dossier") { result in
            if case .failure(let error) = result { errorMessage = error.localizedDescription }
        }
        .alert("Couldn't complete that change", isPresented: Binding(get: { errorMessage != nil }, set: { if !$0 { errorMessage = nil } })) {
            Button("OK", role: .cancel) { errorMessage = nil }
        } message: { Text(errorMessage ?? "") }
    }

    private func openCharacter(_ url: URL) {
        do {
            guard url.isFileURL else { throw CocoaError(.fileReadUnsupportedScheme) }
            let scoped = url.startAccessingSecurityScopedResource()
            defer { if scoped { url.stopAccessingSecurityScopedResource() } }
            let opened = try DossierCharacter(data: Data(contentsOf: url))
            character = opened
            filename = url.deletingPathExtension().lastPathComponent
            isDemo = false
        } catch { errorMessage = error.localizedDescription }
    }
}
