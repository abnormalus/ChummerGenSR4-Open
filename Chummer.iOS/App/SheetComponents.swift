import SwiftUI
import DossierCore

enum SheetTheme {
    // Labels and controls must adapt together with the card surface.
    static let ink = Color(uiColor: UIColor { traits in
        traits.userInterfaceStyle == .dark
            ? UIColor(red: 0.45, green: 0.86, blue: 0.92, alpha: 1)
            : UIColor(red: 0.02, green: 0.31, blue: 0.36, alpha: 1)
    })
    static let header = Color(uiColor: UIColor { traits in
        traits.userInterfaceStyle == .dark
            ? UIColor(red: 0.08, green: 0.22, blue: 0.25, alpha: 1)
            : UIColor(red: 0.70, green: 0.88, blue: 0.91, alpha: 1)
    })
    static let paper = Color(uiColor: .systemGroupedBackground)
    static let secondary = Color(uiColor: UIColor { traits in
        traits.userInterfaceStyle == .dark
            ? UIColor(red: 0.72, green: 0.77, blue: 0.79, alpha: 1)
            : UIColor(red: 0.28, green: 0.32, blue: 0.34, alpha: 1)
    })
    static let card = Color(uiColor: .secondarySystemGroupedBackground)
}

struct SheetCard<Content: View>: View {
    let title: String
    var rowSpacing: CGFloat = 12
    var headerAccessory: AnyView? = nil
    @ViewBuilder var content: Content
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack(spacing: 8) {
                Text(title.uppercased()).font(.headline).tracking(1)
                Spacer(minLength: 0)
                headerAccessory
            }
            .foregroundStyle(SheetTheme.ink).padding(.horizontal, 10).frame(minHeight: 44)
            .background(SheetTheme.header)
            VStack(alignment: .leading, spacing: rowSpacing) { content }.padding(12)
        }
        .foregroundStyle(.primary)
        .background(SheetTheme.card).clipShape(RoundedRectangle(cornerRadius: 10))
        .overlay(RoundedRectangle(cornerRadius: 10).stroke(SheetTheme.ink.opacity(0.35), lineWidth: 1))
    }
}

/// Shared treatment for structured entries, never prose blocks.
struct SheetRowStyle: ViewModifier {
    let index: Int
    func body(content: Content) -> some View {
        content
            .padding(.horizontal, 8).padding(.vertical, 4)
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(SheetTheme.ink.opacity(index.isMultiple(of: 2) ? 0.08 : 0.025))
            .overlay(alignment: .bottom) {
                Rectangle().fill(SheetTheme.ink.opacity(0.18)).frame(height: 0.5)
            }
    }
}
extension View {
    func sheetRow(_ index: Int) -> some View { modifier(SheetRowStyle(index: index)) }
}

struct SheetField: View {
    let title: String
    @Binding var value: String
    var multiline = false
    var body: some View {
        VStack(alignment: .leading, spacing: 3) {
            Text(title).font(.caption).foregroundStyle(SheetTheme.secondary)
            TextField(title, text: $value, axis: multiline ? .vertical : .horizontal)
                .lineLimit(multiline ? 3...12 : 1...1)
                .textFieldStyle(.plain).padding(.vertical, 3)
            Rectangle().fill(SheetTheme.ink.opacity(0.25)).frame(height: 1)
        }
    }
}

struct NumberField: View {
    let title: String
    @Binding var value: Int
    var body: some View {
        VStack(spacing: 4) {
            Text(title).font(.caption2).foregroundStyle(SheetTheme.secondary)
            TextField(title, value: $value, format: .number)
                .keyboardType(.numbersAndPunctuation).multilineTextAlignment(.center)
                .padding(6).background(SheetTheme.header.opacity(0.3), in: RoundedRectangle(cornerRadius: 5))
        }
        .frame(minWidth: 44)
    }
}

struct DamageTrackView: View {
    let title: String
    @Binding var damage: Int
    let capacity: Int
    @State private var track: DamageTrack
    init(title: String, damage: Binding<Int>, capacity: Int) {
        self.title = title; self._damage = damage; self.capacity = capacity
        _track = State(initialValue: DamageTrack(damage: damage.wrappedValue, capacity: capacity))
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("\(title) · \(damage)").font(.subheadline.bold())
            LazyVGrid(columns: Array(repeating: GridItem(.fixed(44), spacing: 3), count: 3), spacing: 3) {
                ForEach(0..<track.boxCount, id: \.self) { index in
                    let state = track.state(at: index)
                    ZStack {
                        Rectangle().fill(state == .blocked ? SheetTheme.ink : Color.clear)
                        Rectangle().stroke(SheetTheme.ink, lineWidth: 1)
                        if let penalty = track.penalty(at: index) {
                            Text(String(penalty)).font(.caption.bold())
                                .foregroundStyle(.primary).frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topTrailing).padding(3)
                        }
                        if state == .damaged { Image(systemName: "xmark").font(.title2).foregroundStyle(.primary) }
                    }
                    .frame(width: 44, height: 44).contentShape(Rectangle())
                    .gesture(LongPressGesture(minimumDuration: 0.5).exclusively(before: TapGesture()).onEnded { gesture in
                        switch gesture {
                        case .first: track.hold(index)
                        case .second: track.tap(index)
                        }
                        if damage != track.damage { damage = track.damage }
                    })
                    .accessibilityElement(children: .ignore)
                    .accessibilityLabel("\(title) box \(index + 1)")
                    .accessibilityValue(state == .blocked ? "Blocked" : state == .damaged ? "Damage" : "Undamaged")
                    .accessibilityAddTraits(.isButton)
                    .accessibilityAction { track.tap(index); damage = track.damage }
                    .accessibilityAction(named: "Adjust optional boxes after eight") { track.hold(index); damage = track.damage }
                }
            }
        }
        .onChange(of: damage) { _, new in
            if track.damage != new { track = DamageTrack(damage: new, capacity: capacity) }
        }
        .onChange(of: capacity) { _, new in track = DamageTrack(damage: damage, capacity: new) }
    }
}
