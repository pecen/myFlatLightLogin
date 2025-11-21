import SwiftUI

@main
struct MyFlatLightLoginApp: App {
    var body: some Scene {
        WindowGroup {
            MainWindow()
        }
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)
    }
}
