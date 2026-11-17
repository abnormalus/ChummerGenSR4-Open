import SwiftUI
import UIKit
import PDFKit
import CoreText
import UniformTypeIdentifiers
import DossierCore

struct GeneratedCharacterSheet: Identifiable {
    let id = UUID()
    let title: String
    let data: Data
    var previewTitle = "Character Sheet"
}

/// Uses the same current-value projection as the readable snapshot. Text is
/// measured and split into lines before drawing; oversized rows continue on the
/// next page with their section and column headings repeated.
struct CharacterSheetPDF {
    static func data(for sheet: PrintableSheet, startingPage: Int = 1) -> Data {
        let bounds = CGRect(x: 0, y: 0, width: 612, height: 792)
        let format = UIGraphicsPDFRendererFormat()
        format.documentInfo = [kCGPDFContextTitle as String: sheet.title + " - Character Sheet", kCGPDFContextCreator as String: "SR 4 Dossier"]
        let renderer = UIGraphicsPDFRenderer(bounds: bounds, format: format)
        return renderer.pdfData { context in
            let left: CGFloat = 36, width: CGFloat = 540, bottom: CGFloat = 748
            let font = UIFont.systemFont(ofSize: 9)
            let bold = UIFont.boldSystemFont(ofSize: 9)
            var y: CGFloat = 0, page = startingPage - 1
            func draw(_ text: String, x: CGFloat, y: CGFloat, font: UIFont, color: UIColor = .black) {
                (text as NSString).draw(at: CGPoint(x: x, y: y), withAttributes: [.font: font, .foregroundColor: color])
            }
            func lines(_ text: String, width: CGFloat, font: UIFont) -> [String] {
                text.components(separatedBy: .newlines).flatMap { paragraph -> [String] in
                    if paragraph.isEmpty { return [""] }
                    let attributed = NSAttributedString(string: paragraph, attributes: [.font: font])
                    let setter = CTTypesetterCreateWithAttributedString(attributed)
                    let string = paragraph as NSString
                    var index = 0, result: [String] = []
                    while index < string.length {
                        let suggested = CTTypesetterSuggestLineBreak(setter, index, Double(max(1, width)))
                        let count = max(1, suggested > 0 ? suggested : CTTypesetterSuggestClusterBreak(setter, index, Double(max(1, width))))
                        result.append(string.substring(with: NSRange(location: index, length: min(count, string.length - index))))
                        index += count
                    }
                    return result
                }
            }
            func newPage() {
                context.beginPage(); page += 1
                draw("SR 4 DOSSIER", x: left, y: 24, font: UIFont.boldSystemFont(ofSize: 10))
                draw("CHARACTER SHEET", x: 438, y: 24, font: UIFont.systemFont(ofSize: 9))
                let titleLines = lines(sheet.title, width: width, font: UIFont.boldSystemFont(ofSize: 18))
                var titleY: CGFloat = 40
                for line in titleLines.prefix(2) { draw(line, x: left, y: titleY, font: UIFont.boldSystemFont(ofSize: 18)); titleY += 22 }
                y = titleY + 10
                context.cgContext.setStrokeColor(UIColor.gray.cgColor)
                context.cgContext.stroke(CGRect(x: left, y: 763, width: width, height: 0.3))
                draw("SR4 character snapshot - current sheet values", x: left, y: 770, font: UIFont.systemFont(ofSize: 8), color: .darkGray)
                draw("Page \(page)", x: 540, y: 770, font: UIFont.systemFont(ofSize: 8), color: .darkGray)
            }
            newPage()
            for section in sheet.sections {
                let sum = section.weights.reduce(0, +)
                let widths = section.weights.map { width * CGFloat($0 / sum) }
                let headerLines = section.columns.enumerated().map { lines($0.element, width: widths[$0.offset] - 10, font: bold) }
                let columnHeight = section.columns.allSatisfy(\.isEmpty) ? 0 : CGFloat(headerLines.map(\.count).max() ?? 1) * 12 + 8
                func heading(continued: Bool) {
                    context.cgContext.setFillColor(UIColor(white: 0.18, alpha: 1).cgColor)
                    context.cgContext.fill(CGRect(x: left, y: y, width: width, height: 22))
                    draw(section.title.uppercased() + (continued ? " (CONTINUED)" : ""), x: left + 6, y: y + 5, font: bold, color: .white)
                    y += 22
                    var x = left
                    for index in section.columns.indices {
                        for (line, text) in headerLines[index].enumerated() { draw(text, x: x + 5, y: y + 4 + CGFloat(line) * 12, font: bold) }
                        x += widths[index]
                    }
                    y += columnHeight
                }
                if y + 22 + columnHeight + 22 > bottom { newPage() }
                heading(continued: false)
                for (rowIndex, row) in section.rows.enumerated() {
                    let cells = section.columns.indices.map { index in lines(row.indices.contains(index) ? row[index] : "", width: widths[index] - 10, font: font) }
                    let count = cells.map(\.count).max() ?? 1
                    var offset = 0
                    while offset < count {
                        if y + 20 > bottom { newPage(); heading(continued: true) }
                        let fit = max(1, Int((bottom - y - 8) / 12))
                        let take = min(fit, count - offset)
                        let height = CGFloat(take) * 12 + 8
                        if rowIndex.isMultiple(of: 2) {
                            context.cgContext.setFillColor(UIColor(white: 0.94, alpha: 1).cgColor)
                            context.cgContext.fill(CGRect(x: left, y: y, width: width, height: height))
                        }
                        var x = left
                        for index in cells.indices {
                            // Repeat short identifying cells on a continued row.
                            // A consumed short cell must never form an inverted range.
                            let start = offset < cells[index].count ? offset : (cells[index].count <= 2 ? 0 : cells[index].count)
                            for line in start..<min(start + take, cells[index].count) {
                                draw(cells[index][line], x: x + 5, y: y + 4 + CGFloat(line - start) * 12, font: font)
                            }
                            x += widths[index]
                        }
                        context.cgContext.setStrokeColor(UIColor(white: 0.75, alpha: 1).cgColor)
                        context.cgContext.stroke(CGRect(x: left, y: y + height, width: width, height: 0.3))
                        y += height; offset += take
                    }
                }
                if section.title == "Condition monitor" {
                    let largest = sheet.monitors.map { min(36, max(12, $0.capacity, $0.damage)) }.max() ?? 12
                    let clipped = sheet.monitors.contains { max($0.capacity, $0.damage) > 36 }
                    let height = CGFloat((largest + 11) / 12) * 19 + 34 + (clipped ? 10 : 0)
                    if y + height > bottom { newPage(); heading(continued: true) }
                    for (side, monitor) in sheet.monitors.enumerated() {
                        let x = left + CGFloat(side) * (width / 2)
                        draw(monitor.title + " - \(monitor.damage) / \(monitor.capacity)", x: x + 5, y: y + 8, font: bold)
                        let total = min(36, max(12, monitor.capacity, monitor.damage))
                        for box in 0..<total {
                            let rect = CGRect(x: x + 5 + CGFloat(box % 12) * 19, y: y + 24 + CGFloat(box / 12) * 19, width: 17, height: 17)
                            if box >= max(monitor.capacity, monitor.damage) {
                                context.cgContext.setFillColor(UIColor(white: 0.65, alpha: 1).cgColor); context.cgContext.fill(rect)
                            }
                            context.cgContext.setStrokeColor(UIColor.black.cgColor); context.cgContext.stroke(rect)
                            if box < monitor.damage { draw("X", x: rect.minX + 4, y: rect.minY + 2, font: bold) }
                            let penalty = box + 1 - monitor.offset
                            if !monitor.ignored, box < monitor.capacity, penalty > 0, penalty.isMultiple(of: monitor.threshold) {
                                draw(String(-penalty / monitor.threshold), x: rect.minX + 9, y: rect.minY, font: UIFont.systemFont(ofSize: 6))
                            }
                        }
                        if max(monitor.capacity, monitor.damage) > 36 { draw("First 36 boxes shown; full counts above.", x: x + 5, y: y + height - 10, font: UIFont.systemFont(ofSize: 7)) }
                    }
                    y += height
                }
                y += 12
            }
        }
    }
}

private struct PDFFile: FileDocument {
    static let readableContentTypes: [UTType] = [.pdf]
    var data: Data
    init(data: Data) { self.data = data }
    init(configuration: ReadConfiguration) throws {
        guard let bytes = configuration.file.regularFileContents else { throw CocoaError(.fileReadCorruptFile) }
        data = bytes
    }
    func fileWrapper(configuration: WriteConfiguration) throws -> FileWrapper { FileWrapper(regularFileWithContents: data) }
}

private struct SheetPDFView: UIViewRepresentable {
    let data: Data
    func makeUIView(context: Context) -> PDFView {
        let view = PDFView(); view.autoScales = true; view.displayMode = .singlePageContinuous
        view.document = PDFDocument(data: data)
        view.accessibilityLabel = "Character sheet PDF preview"
        return view
    }
    func updateUIView(_ view: PDFView, context: Context) {}
}

struct CharacterSheetPreview: View {
    let sheet: GeneratedCharacterSheet
    @Environment(\.dismiss) private var dismiss
    @State private var exporting = false
    @State private var errorMessage: String?
    private var filename: String {
        let title = String(sheet.title.prefix(80)).components(separatedBy: CharacterSet.controlCharacters.union(CharacterSet(charactersIn: "/:"))).joined(separator: " ").trimmingCharacters(in: .whitespacesAndNewlines)
        return (title.isEmpty ? "Character" : title) + " - " + sheet.previewTitle
    }
    var body: some View {
        NavigationStack {
            SheetPDFView(data: sheet.data).ignoresSafeArea(edges: .bottom)
                .navigationTitle(sheet.previewTitle).navigationBarTitleDisplayMode(.inline)
                .toolbar {
                    ToolbarItem(placement: .cancellationAction) { Button("Done") { dismiss() } }
                    ToolbarItemGroup(placement: .primaryAction) {
                        Button("Save PDF", systemImage: "square.and.arrow.down") { exporting = true }
                        Button("Print", systemImage: "printer") { printSheet() }
                    }
                }
        }
        .fileExporter(isPresented: $exporting, document: PDFFile(data: sheet.data), contentType: .pdf, defaultFilename: filename) { result in
            if case .failure(let error) = result, (error as NSError).code != NSUserCancelledError { errorMessage = error.localizedDescription }
        }
        .alert("Couldn't export the sheet", isPresented: Binding(get: { errorMessage != nil }, set: { if !$0 { errorMessage = nil } })) {
            Button("OK", role: .cancel) { errorMessage = nil }
        } message: { Text(errorMessage ?? "") }
    }
    private func printSheet() {
        let controller = UIPrintInteractionController.shared
        let info = UIPrintInfo(dictionary: nil)
        info.jobName = sheet.title + " - " + sheet.previewTitle; info.outputType = .general
        controller.printInfo = info; controller.printingItem = sheet.data
        let completion: UIPrintInteractionController.CompletionHandler = { _, _, error in
            if let error { errorMessage = error.localizedDescription }
        }
        if let window = UIApplication.shared.connectedScenes.compactMap({ $0 as? UIWindowScene }).flatMap(\.windows).first(where: \.isKeyWindow) {
            controller.present(from: CGRect(x: window.bounds.maxX - 70, y: window.safeAreaInsets.top + 20, width: 1, height: 1), in: window, animated: true, completionHandler: completion)
        } else { controller.present(animated: true, completionHandler: completion) }
    }
}
