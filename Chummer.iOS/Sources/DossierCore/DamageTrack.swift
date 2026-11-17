import Foundation

public enum DamageBoxState: Equatable { case undamaged, damaged, blocked }

/// The first eight boxes are fixed. Optional boxes form one contiguous tail;
/// capacity adjustments are session-only, while damage uses Chummer's saved count.
public struct DamageTrack: Equatable {
    public private(set) var damage: Int
    public private(set) var capacity: Int
    public let boxCount: Int
    public var blocked: Set<Int> { Set(capacity..<boxCount) }
    public init(damage: Int, capacity: Int, boxCount: Int = 18) {
        self.damage = max(0, damage)
        self.boxCount = max(1, boxCount, capacity, damage)
        self.capacity = max(0, min(self.boxCount, max(capacity, damage)))
    }
    public func state(at index: Int) -> DamageBoxState {
        if index >= capacity { return .blocked }
        return index < damage ? .damaged : .undamaged
    }
    public func penalty(at index: Int) -> Int? {
        guard index < capacity, index >= 0, (index + 1).isMultiple(of: 3) else { return nil }
        return -(index + 1) / 3
    }
    public mutating func tap(_ index: Int) {
        guard (0..<capacity).contains(index) else { return }
        damage = damage == index + 1 ? index : index + 1
    }
    public mutating func hold(_ index: Int) {
        guard (8..<boxCount).contains(index), index >= damage else { return }
        // Selecting a blocked box extends through it; blocking trims the tail.
        capacity = index < capacity ? index : index + 1
    }
}
