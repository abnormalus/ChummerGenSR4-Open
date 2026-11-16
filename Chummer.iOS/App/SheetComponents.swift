import SwiftUI
import DossierCore

enum SheetTheme {
    static let ink = Color(red: 0.04, green: 0.35, blue: 0.40)
    static let header = Color(red: 0.70, green: 0.88, blue: 0.91)
    static let paper = Color(red: 0.95, green: 0.98, blue: 0.99)
}

struct SheetCard<Content: View>: View {
    let title: String
    @ViewBuilder var content: Content
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text(title.uppercased()).font(.headline).tracking(1)
                .foregroundStyle(SheetTheme.ink).padding(10).frame(maxWidth: .infinity, alignment: .leading)
                .background(SheetTheme.header)
            VStack(alignment: .leading, spacing: 12) { content }.padding(12)
        }
        .background(.background).clipShape(RoundedRectangle(cornerRadius: 10))
        .overlay(RoundedRectangle(cornerRadius: 10).stroke(SheetTheme.ink.opacity(0.35), lineWidth: 1))
    }
}

struct SheetField: View {
    let title: String
    @Binding var value: String
    var multiline = false
    var body: some View {
        VStack(alignment: .leading, spacing: 3) {
            Text(title).font(.caption).foregroundStyle(.secondary)
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
            Text(title).font(.caption2).foregroundStyle(.secondary)
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
                        if state == .damaged { Image(systemName: "xmark").font(.title2).foregroundStyle(SheetTheme.ink) }
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
                    .accessibilityAction(named: "Block or unblock") { track.hold(index); damage = track.damage }
                }
            }
            Text("Tap to set damage. Hold to block or unblock.").font(.caption2).foregroundStyle(.secondary)
        }
        .onChange(of: damage) { _, new in
            if track.damage != new { track = DamageTrack(damage: new, capacity: capacity) }
        }
        .onChange(of: capacity) { _, new in track = DamageTrack(damage: damage, capacity: new) }
    }
}
