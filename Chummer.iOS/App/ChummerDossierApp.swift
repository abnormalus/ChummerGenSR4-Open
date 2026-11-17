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
    @State private var selectedPage = DossierPage.character
    @State private var filename = "Character"
    @State private var importing = false
    @State private var exporting = false
    @State private var exportFile: CharacterFile?
    @State private var isDemo = false
    @State private var errorMessage: String?
    @Environment(\.scenePhase) private var scenePhase
    @State private var library = DossierLibrary()
    @State private var selectedID: UUID?
    @State private var loadedLibrary = false
    @State private var libraryAvailable = false
    @State private var savedData: Data?

    private var testTheme: ColorScheme? {
        #if DEBUG
        if ProcessInfo.processInfo.environment["DOSSIER_UI_FIXTURE"] != nil {
            return ProcessInfo.processInfo.environment["DOSSIER_UI_THEME"] == "dark" ? .dark : .light
        }
        #endif
        return nil
    }
    private var libraryURL: URL {
        FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent(libraryPath)
    }


    private var libraryPath: String {
        #if DEBUG
        if ProcessInfo.processInfo.environment["DOSSIER_UI_FIXTURE"] != nil { return "Dossier-UITests/library.json" }
        #endif
        return "Dossier/library.json"
    }
    var body: some View {
        NavigationStack {
            Group {
                if let snapshot = character {
                    DossierView(character: Binding(get: { character ?? snapshot }, set: { character = $0 }), selectedPage: $selectedPage)
                        .id(selectedID)
                        .environment(\.saveDossier, { _ = autosave() })
                } else {
                    ContentUnavailableView {
                        Label("SR 4 Dossier", systemImage: "person.text.rectangle")
                    } description: {
                        Text("Your runner, on a familiar sheet. Open an SR4 Chummer character from Files.")
                    } actions: {
                        Button("Open Chummer Character") { importing = true }.buttonStyle(.borderedProminent)
                        Button("Explore a Demo") {
                            do { let demo = try DossierCharacter.demo; character = demo; savedData = demo.data(); selectedID = nil; isDemo = true } catch { errorMessage = error.localizedDescription }
                        }
                    }
                }
            }
            .navigationTitle(character?.title ?? "SR 4 Dossier")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .principal) {
                    Menu {
                        if isDemo { Text("Demo · Open a character to save changes") }
                        ForEach(library.entries) { entry in
                            Button { switchCharacter(to: entry) } label: {
                                if entry.id == selectedID { Label(library.displayName(for: entry), systemImage: "checkmark") }
                                else { Text(library.displayName(for: entry)) }
                            }
                        }
                        Divider()
                        Button("Open Character…", systemImage: "folder") { importing = true }
                    } label: {
                        HStack(spacing: 4) {
                            Text((character?.title ?? "SR 4 Dossier") + (isDemo ? " · Demo" : "")).font(.headline).lineLimit(1)
                            Image(systemName: "chevron.down").font(.caption2)
                        }
                    }
                    .accessibilityLabel("\(character?.title ?? "SR 4 Dossier"), switch character")
                }
                ToolbarItemGroup(placement: .topBarTrailing) {
                    if character != nil && UIDevice.current.userInterfaceIdiom == .pad {
                        Menu {
                            ForEach(DossierPage.allCases) { page in
                                Button { selectedPage = page } label: {
                                    Label(page.rawValue, systemImage: page == selectedPage ? "checkmark" : page.icon)
                                }
                            }
                        } label: { Image(systemName: selectedPage.icon) }
                        .accessibilityLabel("\(selectedPage.rawValue), choose dossier page")
                    }
                    Button("Save Copy", systemImage: "square.and.arrow.up") {
                        do {
                            if let character { exportFile = CharacterFile(data: try character.exportData()); exporting = true }
                        } catch { errorMessage = error.localizedDescription }
                    }
                        .disabled(character == nil || isDemo)
                }
            }
        }
        .preferredColorScheme(testTheme)
        .tint(SheetTheme.ink)
        .task { restoreLibrary() }
        .task(id: character?.revision) {
            do { try await Task.sleep(for: .milliseconds(700)) } catch { return }
            _ = autosave()
        }
        .onReceive(NotificationCenter.default.publisher(for: UIResponder.keyboardDidHideNotification)) { _ in _ = autosave() }
        .onChange(of: scenePhase) { _, phase in
            if phase != .active { _ = autosave() }
        }
        .fileImporter(isPresented: $importing, allowedContentTypes: [.chummerCharacter, .xml]) { result in
            do { openCharacter(try result.get()) } catch { errorMessage = error.localizedDescription }
        }
        .onOpenURL { openCharacter($0) }
        .fileExporter(isPresented: $exporting, document: exportFile,
                      contentType: .chummerCharacter, defaultFilename: filename + " - Dossier") { result in
            switch result {
            case .success: break
            case .failure(let error): errorMessage = error.localizedDescription
            }
        }
        .alert(errorMessage?.hasPrefix("Recovered characters") == true ? "Saved characters recovered" : "Couldn't complete that change", isPresented: Binding(get: { errorMessage != nil }, set: { if !$0 { errorMessage = nil } })) {
            if loadedLibrary && !libraryAvailable {
                Button("Keep Recovery Copies and Start Fresh") { startFreshLibrary() }
            }
            Button("OK", role: .cancel) { errorMessage = nil }
        } message: {
            Text((errorMessage ?? "") + (loadedLibrary && !libraryAvailable ? "\nYour saved archives have not been replaced. Starting fresh keeps recovery copies; you can then reopen your original characters from Files." : ""))
        }

    }

    private func startFreshLibrary() {
        do {
            library = try DossierLibrary.startFreshPreservingArchives(at: libraryURL)
            libraryAvailable = true; character = nil; selectedID = nil; savedData = nil; isDemo = false
        } catch { errorMessage = "Couldn't retain recovery copies. Your saved files have been kept. " + error.localizedDescription }
    }

    private func restoreLibrary() {
        guard !loadedLibrary else { return }
        loadedLibrary = true
        do {
            #if DEBUG
            if let name = ProcessInfo.processInfo.environment["DOSSIER_UI_PAGE"], let page = DossierPage(rawValue: name) { selectedPage = page }
            if ProcessInfo.processInfo.environment["DOSSIER_UI_RESTORE"] != "1", let encoded = ProcessInfo.processInfo.environment["DOSSIER_UI_FIXTURE"], let data = Data(base64Encoded: encoded) {
                let opened = try DossierCharacter(data: data)
                library = DossierLibrary(); libraryAvailable = true
                selectedID = try library.save(opened, filename: "QA Runner", id: nil, to: libraryURL)
                if let second = ProcessInfo.processInfo.environment["DOSSIER_UI_SECOND_FIXTURE"], let bytes = Data(base64Encoded: second) {
                    _ = try library.save(DossierCharacter(data: bytes), filename: "Second", id: nil, to: libraryURL)
                    _ = try library.save(opened, filename: "QA Runner", id: selectedID, to: libraryURL)
                }
                if let damage = ProcessInfo.processInfo.environment["DOSSIER_UI_DAMAGE_ARCHIVE"], !damage.isEmpty {
                    try Data("damaged test archive".utf8).write(to: libraryURL)
                    if damage == "both" { try Data("damaged test backup".utf8).write(to: libraryURL.appendingPathExtension("backup")) }
                }
                character = opened; savedData = data
                return
            }
            #endif
            library = try DossierLibrary.load(from: libraryURL)
            libraryAvailable = true
            if library.recoveredFromBackup { errorMessage = "Recovered characters from the previous saved copy. The latest edits may be missing. The damaged archive will be retained when you next save." }
            if let entry = library.entries.first(where: { $0.id == library.selectedID }) {
                character = try DossierCharacter(data: entry.data)
                filename = entry.filename
                selectedID = entry.id
                savedData = entry.data
            }
        } catch { errorMessage = "Couldn't restore saved characters: " + error.localizedDescription }
    }

    @discardableResult
    private func autosave() -> Bool {
        guard let character, !isDemo else { return true }
        guard libraryAvailable else {
            errorMessage = "Saved characters couldn't be loaded. Your current sheet is still open; use Save Copy to keep it."
            return false
        }
        guard character.data() != savedData || selectedID != library.selectedID || library.recoveredFromBackup else { return true }
        do {
            selectedID = try library.save(character, filename: filename, id: selectedID, to: libraryURL)
            savedData = character.data()
            return true
        } catch {
            errorMessage = "Couldn't save this character. Keep it open and try Save Copy. " + error.localizedDescription
            return false
        }
    }

    private func switchCharacter(to entry: DossierLibrary.Entry) {
        guard entry.id != selectedID else { return }
        do {
            let opened = try DossierCharacter(data: entry.data)
            guard autosave() else { return }
            _ = try library.save(opened, filename: entry.filename, id: entry.id, to: libraryURL)
            character = opened
            filename = entry.filename
            selectedID = entry.id
            isDemo = false
            savedData = opened.data()
        } catch { errorMessage = error.localizedDescription }
    }

    private func openCharacter(_ url: URL) {
        do {
            restoreLibrary()
            guard url.isFileURL else { throw CocoaError(.fileReadUnsupportedScheme) }
            let scoped = url.startAccessingSecurityScopedResource()
            defer { if scoped { url.stopAccessingSecurityScopedResource() } }
            let opened = try DossierCharacter(data: Data(contentsOf: url))
            guard autosave() else { return }
            // Persist the incoming working copy before replacing the current screen.
            let name = url.deletingPathExtension().lastPathComponent
            guard libraryAvailable else { throw CocoaError(.fileWriteUnknown) }
            let id = try library.save(opened, filename: name, id: nil, to: libraryURL)
            character = opened
            filename = name
            selectedID = id
            savedData = opened.data()
            isDemo = false
        } catch { errorMessage = error.localizedDescription }
    }
}
