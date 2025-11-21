import SwiftUI
import Combine

/// Main window ViewModel - equivalent to MainWindowViewModel.cs
@MainActor
class MainWindowViewModel: ObservableObject {
    // MARK: - Published Properties (equivalent to INotifyPropertyChanged)

    @Published var email: String = ""
    @Published var password: String = ""
    @Published var rememberMe: Bool = true
    @Published var isPasswordEmpty: Bool = true
    @Published var showAlert: Bool = false
    @Published var alertMessage: String = ""

    // MARK: - Database Manager

    private let dbManager = DatabaseManager.shared

    // MARK: - Computed Properties

    var isFormValid: Bool {
        !email.isEmpty && !password.isEmpty
    }

    // MARK: - Commands (equivalent to RelayCommand)

    /// Sign in command
    func signIn() {
        guard isFormValid else {
            showAlert(message: "Please enter email and password")
            return
        }

        // Check if user exists
        if let user = dbManager.fetchUser(byUsername: email) {
            if user.password == password {
                showAlert(message: "Login successful! Welcome, \(user.name.isEmpty ? user.username : user.name)")
            } else {
                showAlert(message: "Invalid password")
            }
        } else {
            showAlert(message: "User not found")
        }
    }

    /// Register command
    func register() {
        guard isFormValid else {
            showAlert(message: "Please enter email and password")
            return
        }

        // Check if user already exists
        if dbManager.fetchUser(byUsername: email) != nil {
            showAlert(message: "User already exists")
            return
        }

        // Create new user
        let newUser = User(
            name: "",
            lastname: "",
            username: email,
            password: password
        )

        if dbManager.insertUser(newUser) {
            showAlert(message: "Registration successful!")
            // Clear form
            email = ""
            password = ""
        } else {
            showAlert(message: "Registration failed")
        }
    }

    /// Login as guest command
    func loginAsGuest() {
        showAlert(message: "Logged in as Guest")
    }

    /// Shutdown window command
    func shutdown() {
        NSApplication.shared.terminate(nil)
    }

    /// Minimize window command
    func minimizeWindow() {
        NSApplication.shared.windows.first?.miniaturize(nil)
    }

    /// Toggle window size command
    func toggleWindowSize() {
        guard let window = NSApplication.shared.windows.first else { return }
        if window.styleMask.contains(.fullScreen) {
            window.toggleFullScreen(nil)
        } else {
            window.zoom(nil)
        }
    }

    // MARK: - Helper Methods

    private func showAlert(message: String) {
        alertMessage = message
        showAlert = true
    }

    /// Update password empty state
    func updatePasswordState() {
        isPasswordEmpty = password.isEmpty
    }
}
