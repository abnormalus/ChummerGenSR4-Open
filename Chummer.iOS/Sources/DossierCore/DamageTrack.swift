import Foundation

public enum DamageBoxState: Equatable { case undamaged, damaged, blocked }

/// Blocking is session-only presentation state. Only `damage` is written to Chummer.
public struct DamageTrack: Equatable {
    public private(set) var damage: Int
    public private(set) var blocked: Set<Int>
    public let boxCount: Int
    public init(damage: Int, capacity: Int, boxCount: Int = 18) {
        self.boxCount = max(1, boxCount)
        self.damage = max(0, damage)
        let usable = max(0, min(capacity, self.boxCount))
        // Never hide imported damage, including overflow; it remains exportable unchanged.
        self.blocked = Set(usable..<self.boxCount).filter { $0 >= damage }
    }
    public func state(at index: Int) -> DamageBoxState {
        if blocked.contains(index) { return .blocked }
        return index < damage ? .damaged : .undamaged
    }
    public mutating func tap(_ index: Int) {
        guard (0..<boxCount).contains(index), !(0...index).contains(where: { blocked.contains($0) }) else { return }
        damage = damage == index + 1 ? index : index + 1
    }
    public mutating func hold(_ index: Int) {
        guard (0..<boxCount).contains(index) else { return }
        if blocked.remove(index) == nil {
            blocked.insert(index)
            // Chummer stores a count, so damage must remain a contiguous prefix.
            if index < damage { damage = index }
        }
    }
}
