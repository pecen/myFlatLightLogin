import SwiftUI

/// App color scheme - matching the WPF color palette
extension Color {
    // Primary colors
    static let appRed = Color(hex: "ff131e")

    // Border colors
    static let borderGray = Color(hex: "bdbdbd")
    static let borderLightGray = Color(hex: "e2e9e9")

    // Text colors
    static let textGray = Color(hex: "5a5a5a")
    static let textMediumGray = Color(hex: "707070")

    // Background colors
    static let lightGray = Color(hex: "c7c7c7")
    static let veryLightGray = Color(hex: "dfdfdf")

    // Initialize from hex string
    init(hex: String) {
        let hex = hex.trimmingCharacters(in: CharacterSet.alphanumerics.inverted)
        var int: UInt64 = 0
        Scanner(string: hex).scanHexInt64(&int)
        let a, r, g, b: UInt64
        switch hex.count {
        case 3: // RGB (12-bit)
            (a, r, g, b) = (255, (int >> 8) * 17, (int >> 4 & 0xF) * 17, (int & 0xF) * 17)
        case 6: // RGB (24-bit)
            (a, r, g, b) = (255, int >> 16, int >> 8 & 0xFF, int & 0xFF)
        case 8: // ARGB (32-bit)
            (a, r, g, b) = (int >> 24, int >> 16 & 0xFF, int >> 8 & 0xFF, int & 0xFF)
        default:
            (a, r, g, b) = (255, 0, 0, 0)
        }
        self.init(
            .sRGB,
            red: Double(r) / 255,
            green: Double(g) / 255,
            blue: Double(b) / 255,
            opacity: Double(a) / 255
        )
    }
}
