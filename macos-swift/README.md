# myFlatLightLogin - macOS Swift Port

A macOS port of the WPF login/registration UI application, built with SwiftUI.

## Requirements

- macOS 13.0 (Ventura) or later
- Xcode 15.0 or later
- Swift 5.9 or later

## Project Structure

```
macos-swift/
└── myFlatLightLogin/
    ├── Package.swift           # Swift Package Manager configuration
    └── Sources/
        ├── App/                # Application entry point
        ├── Models/             # Data models (User, UserDto)
        ├── ViewModels/         # MVVM ViewModels
        ├── Views/              # SwiftUI Views
        ├── Database/           # SQLite database layer
        └── Extensions/         # Color extensions
```

## Building and Running

### Using Xcode

1. Open `myFlatLightLogin/Package.swift` in Xcode
2. Select the `myFlatLightLogin` scheme
3. Press `Cmd+R` to build and run

### Using Command Line

```bash
cd macos-swift/myFlatLightLogin
swift build
swift run
```

## Architecture

This application uses the MVVM (Model-View-ViewModel) pattern, mirroring the original WPF implementation:

- **Models**: `User` and `UserDto` data structures
- **Views**: SwiftUI views that define the UI
- **ViewModels**: `ObservableObject` classes that handle business logic and state
- **Database**: SQLite database using SQLite.swift library

## Features

- Modern flat design login form
- Email and password input fields
- Remember me checkbox
- Sign in and Register functionality
- Guest login option
- SQLite database for user storage
- Custom color scheme matching the original WPF design

## Dependencies

- [SQLite.swift](https://github.com/stephencelis/SQLite.swift) - Type-safe SQLite wrapper

## Database

The application stores user data in a SQLite database located at:
```
~/Library/Application Support/myFlatLightLogin/security.db3
```

## Color Scheme

The app uses the same color palette as the WPF version:
- Primary Red: `#ff131e`
- Border Gray: `#bdbdbd`
- Text Gray: `#5a5a5a`

## Original Project

This is a port of the WPF/.NET 7.0 application to native macOS using SwiftUI.
