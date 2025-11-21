import SwiftUI

/// Main window view - equivalent to MainWindow.xaml
struct MainWindow: View {
    @StateObject private var viewModel = MainWindowViewModel()

    var body: some View {
        VStack(spacing: 0) {
            // Top Section - Header Bar
            HeaderBar(viewModel: viewModel)

            // Middle Section - Login Form
            LoginFormSection(viewModel: viewModel)

            // Bottom Section - Footer
            FooterBar()
        }
        .frame(width: 1024, height: 720)
        .background(Color.white)
        .alert("Message", isPresented: $viewModel.showAlert) {
            Button("OK", role: .cancel) { }
        } message: {
            Text(viewModel.alertMessage)
        }
    }
}

// MARK: - Header Bar

struct HeaderBar: View {
    @ObservedObject var viewModel: MainWindowViewModel

    var body: some View {
        HStack {
            // Search button
            HeaderButton(systemName: "magnifyingglass") {
                // Search action (not implemented)
            }

            Spacer()

            // Resize button
            HeaderButton(systemName: "rectangle.arrowtriangle.2.outward") {
                viewModel.toggleWindowSize()
            }

            // Exit button
            HeaderButton(systemName: "xmark", isDestructive: true) {
                viewModel.shutdown()
            }
        }
        .padding(.horizontal, 10)
        .frame(height: 50)
        .background(Color.white)
        .overlay(
            Rectangle()
                .frame(height: 1)
                .foregroundColor(Color.borderLightGray),
            alignment: .bottom
        )
    }
}

struct HeaderButton: View {
    let systemName: String
    var isDestructive: Bool = false
    let action: () -> Void

    @State private var isHovered = false

    var body: some View {
        Button(action: action) {
            Image(systemName: systemName)
                .font(.system(size: 14))
                .foregroundColor(isDestructive || isHovered ? .appRed : .textGray)
                .frame(width: 37, height: 37)
                .background(
                    Circle()
                        .fill(isHovered ? Color.veryLightGray : Color.clear)
                )
        }
        .buttonStyle(.plain)
        .onHover { hovering in
            isHovered = hovering
        }
    }
}

// MARK: - Login Form Section

struct LoginFormSection: View {
    @ObservedObject var viewModel: MainWindowViewModel

    var body: some View {
        ZStack {
            // Background with image
            Color(NSColor.controlBackgroundColor)
                .overlay(
                    Image(systemName: "photo")
                        .resizable()
                        .scaledToFill()
                        .opacity(0.1)
                )
                .clipped()

            // Login form card
            VStack(spacing: 20) {
                // Title
                Text("Login or Register")
                    .font(.system(size: 24, weight: .semibold))
                    .foregroundColor(.textGray)

                // Email field
                InputField(
                    placeholder: "Enter Email",
                    systemImage: "envelope",
                    text: $viewModel.email
                )

                // Password field
                SecureInputField(
                    placeholder: "Enter Password",
                    systemImage: "lock",
                    text: $viewModel.password,
                    onTextChange: viewModel.updatePasswordState
                )

                // Remember me checkbox
                HStack {
                    Toggle("Remember me", isOn: $viewModel.rememberMe)
                        .toggleStyle(CheckboxToggleStyle())
                        .foregroundColor(.textGray)
                    Spacer()
                }

                // Action buttons
                HStack(spacing: 10) {
                    ActionButton(title: "Sign in", isPrimary: true) {
                        viewModel.signIn()
                    }

                    ActionButton(title: "Register", isPrimary: false) {
                        viewModel.register()
                    }
                }

                // Divider with "or"
                HStack {
                    Rectangle()
                        .fill(Color.borderGray)
                        .frame(height: 1)
                    Text("or")
                        .font(.system(size: 12))
                        .foregroundColor(.textMediumGray)
                        .padding(.horizontal, 10)
                    Rectangle()
                        .fill(Color.borderGray)
                        .frame(height: 1)
                }

                // Guest login button
                GuestLoginButton {
                    viewModel.loginAsGuest()
                }
            }
            .padding(30)
            .frame(width: 300)
            .background(Color.white)
            .cornerRadius(5)
            .shadow(color: Color.black.opacity(0.1), radius: 10, x: 0, y: 5)
        }
    }
}

// MARK: - Input Fields

struct InputField: View {
    let placeholder: String
    let systemImage: String
    @Binding var text: String

    @State private var isFocused = false

    var body: some View {
        HStack(spacing: 10) {
            Image(systemName: systemImage)
                .foregroundColor(.textMediumGray)
                .frame(width: 20)

            TextField(placeholder, text: $text)
                .textFieldStyle(.plain)
                .font(.system(size: 14))
        }
        .padding(10)
        .background(Color.white)
        .overlay(
            RoundedRectangle(cornerRadius: 3)
                .stroke(isFocused ? Color.appRed : Color.borderGray, lineWidth: 1)
        )
        .onTapGesture {
            isFocused = true
        }
    }
}

struct SecureInputField: View {
    let placeholder: String
    let systemImage: String
    @Binding var text: String
    var onTextChange: () -> Void = {}

    @State private var isFocused = false

    var body: some View {
        HStack(spacing: 10) {
            Image(systemName: systemImage)
                .foregroundColor(.textMediumGray)
                .frame(width: 20)

            SecureField(placeholder, text: $text)
                .textFieldStyle(.plain)
                .font(.system(size: 14))
                .onChange(of: text) { _, _ in
                    onTextChange()
                }
        }
        .padding(10)
        .background(Color.white)
        .overlay(
            RoundedRectangle(cornerRadius: 3)
                .stroke(isFocused ? Color.appRed : Color.borderGray, lineWidth: 1)
        )
        .onTapGesture {
            isFocused = true
        }
    }
}

// MARK: - Buttons

struct ActionButton: View {
    let title: String
    let isPrimary: Bool
    let action: () -> Void

    @State private var isHovered = false

    var body: some View {
        Button(action: action) {
            Text(title)
                .font(.system(size: 14, weight: .medium))
                .foregroundColor(isPrimary || isHovered ? .white : .textGray)
                .frame(maxWidth: .infinity)
                .padding(.vertical, 10)
                .background(
                    RoundedRectangle(cornerRadius: 20)
                        .fill(isPrimary || isHovered ? Color.appRed : Color.white)
                )
                .overlay(
                    RoundedRectangle(cornerRadius: 20)
                        .stroke(isPrimary ? Color.clear : Color.borderGray, lineWidth: 1)
                )
        }
        .buttonStyle(.plain)
        .onHover { hovering in
            isHovered = hovering
        }
    }
}

struct GuestLoginButton: View {
    let action: () -> Void

    @State private var isHovered = false

    var body: some View {
        Button(action: action) {
            Text("Login as Guest")
                .font(.system(size: 14, weight: .medium))
                .foregroundColor(isHovered ? .white : .appRed)
                .frame(maxWidth: .infinity)
                .padding(.vertical, 10)
                .background(
                    RoundedRectangle(cornerRadius: 20)
                        .fill(isHovered ? Color.appRed : Color.white)
                )
                .overlay(
                    RoundedRectangle(cornerRadius: 20)
                        .stroke(Color.appRed, lineWidth: 1)
                )
        }
        .buttonStyle(.plain)
        .onHover { hovering in
            isHovered = hovering
        }
    }
}

// MARK: - Checkbox Style

struct CheckboxToggleStyle: ToggleStyle {
    func makeBody(configuration: Configuration) -> some View {
        HStack {
            Image(systemName: configuration.isOn ? "checkmark.square.fill" : "square")
                .foregroundColor(configuration.isOn ? .appRed : .borderGray)
                .onTapGesture {
                    configuration.isOn.toggle()
                }
            configuration.label
        }
    }
}

// MARK: - Footer Bar

struct FooterBar: View {
    var body: some View {
        HStack {
            // Copyright
            Text("Copyright 2020")
                .font(.system(size: 12))
                .foregroundColor(.textMediumGray)

            Spacer()

            // Navigation menu
            HStack(spacing: 5) {
                FooterButton(title: "Home")
                FooterButton(title: "Features")
                FooterButton(title: "Solutions")
                FooterButton(title: "Videos")
                FooterButton(title: "About")
                FooterButton(title: "Login", isPrimary: true)
            }
        }
        .padding(.horizontal, 20)
        .frame(height: 50)
        .background(Color.white)
        .overlay(
            Rectangle()
                .frame(height: 1)
                .foregroundColor(Color.borderLightGray),
            alignment: .top
        )
    }
}

struct FooterButton: View {
    let title: String
    var isPrimary: Bool = false

    @State private var isHovered = false

    var body: some View {
        Button(action: {
            // Navigation action (not implemented)
        }) {
            Text(title)
                .font(.system(size: 12))
                .foregroundColor(isPrimary || isHovered ? .appRed : .textMediumGray)
                .padding(.horizontal, 10)
                .padding(.vertical, 5)
        }
        .buttonStyle(.plain)
        .onHover { hovering in
            isHovered = hovering
        }
    }
}

// MARK: - Preview

#Preview {
    MainWindow()
}
