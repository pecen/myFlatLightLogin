# Firebase Authentication Setup Guide

This guide will help you set up Firebase Authentication and Realtime Database for the myFlatLightLogin application.

## Prerequisites

- A Google account
- Visual Studio 2022 or later with .NET 8 SDK
- Internet connection

## Step 1: Create a Firebase Project

1. Go to the [Firebase Console](https://console.firebase.google.com/)
2. Click **"Add project"** (or **"Create a project"** if this is your first project)
3. Enter a project name (e.g., "myFlatLightLogin")
4. Click **Continue**
5. (Optional) Enable Google Analytics if desired
6. Click **Create project**
7. Wait for the project to be created, then click **Continue**

## Step 2: Register Your App

1. In the Firebase Console, click the **Web** icon (`</>`) to add a web app
2. Enter an app nickname (e.g., "myFlatLightLogin WPF App")
3. **Do NOT** check "Also set up Firebase Hosting"
4. Click **Register app**
5. You'll see a Firebase SDK snippet - **keep this page open**, you'll need these values later

## Step 3: Enable Email/Password Authentication

1. In the Firebase Console, click **Authentication** in the left sidebar
2. Click **Get started** (if this is your first time using Authentication)
3. Click the **Sign-in method** tab
4. Click **Email/Password** in the providers list
5. Toggle the **Enable** switch to ON
6. Click **Save**

## Step 4: Create Realtime Database

1. In the Firebase Console, click **Realtime Database** in the left sidebar
2. Click **Create Database**
3. Select a database location (choose the closest to your users)
4. Choose **Start in test mode** for now (you can change security rules later)
5. Click **Enable**

## Step 5: Get Your Firebase Configuration

1. In the Firebase Console, click the **Settings** gear icon → **Project settings**
2. Scroll down to the **Your apps** section
3. You should see your web app listed
4. Copy the following values:
   - **API Key** (apiKey)
   - **Auth Domain** (authDomain)
   - **Database URL** (databaseURL)

## Step 6: Configure Your Application (Secure Method)

Your Firebase credentials are now stored securely using configuration files that are NOT checked into Git. You have two options:

### Option A: Using appsettings.json (Recommended for most users)

1. Navigate to `src/myFlatLightLogin.DalFirebase/` folder
2. Copy `appsettings.template.json` to `appsettings.json`:
   ```bash
   # In the src/myFlatLightLogin.DalFirebase/ folder:
   copy appsettings.template.json appsettings.json
   ```
3. Open `appsettings.json` and replace the placeholder values with your actual Firebase configuration:

```json
{
  "Firebase": {
    "ApiKey": "YOUR_ACTUAL_API_KEY_HERE",
    "DatabaseUrl": "https://YOUR-PROJECT-ID.firebaseio.com/",
    "AuthDomain": "YOUR-PROJECT-ID.firebaseapp.com"
  }
}
```

**Example with real values:**
```json
{
  "Firebase": {
    "ApiKey": "AIzaSyC1234567890abcdefghijklmnopqrstuv",
    "DatabaseUrl": "https://myflatlight-12345.europe-west1.firebasedatabase.app/",
    "AuthDomain": "myflatlight-12345.firebaseapp.com"
  }
}
```

**Important:** The `appsettings.json` file is automatically excluded from Git (via `.gitignore`), so your credentials will never be committed to source control.

### Option B: Using User Secrets (Advanced - for developers)

User Secrets are perfect for development as they're stored outside your project folder and never accidentally committed.

1. **Right-click** on the `myFlatLightLogin.DalFirebase` project in Visual Studio
2. Select **Manage User Secrets**
3. Visual Studio will open a `secrets.json` file
4. Add your Firebase configuration:

```json
{
  "Firebase": {
    "ApiKey": "YOUR_ACTUAL_API_KEY_HERE",
    "DatabaseUrl": "https://YOUR-PROJECT-ID.firebaseio.com/",
    "AuthDomain": "YOUR-PROJECT-ID.firebaseapp.com"
  }
}
```

**Or using the command line:**
```bash
cd src/myFlatLightLogin.DalFirebase

dotnet user-secrets set "Firebase:ApiKey" "YOUR_ACTUAL_API_KEY_HERE"
dotnet user-secrets set "Firebase:DatabaseUrl" "https://YOUR-PROJECT-ID.firebaseio.com/"
dotnet user-secrets set "Firebase:AuthDomain" "YOUR-PROJECT-ID.firebaseapp.com"
```

**How it works:**
- In **DEBUG** mode, User Secrets take priority over appsettings.json
- In **RELEASE** mode, only appsettings.json is used
- User Secrets are stored in: `%APPDATA%\Microsoft\UserSecrets\myFlatLightLogin-firebase-secrets\`

## Step 7: Build and Run

1. Build the solution in Visual Studio (`Ctrl+Shift+B`)
2. Fix any build errors if they occur
3. Run the application (`F5`)
4. Try registering a new user
5. Try logging in with the registered user

## Step 8: Verify User Creation

1. Go to the [Firebase Console](https://console.firebase.google.com/)
2. Select your project
3. Click **Authentication** in the left sidebar
4. Click the **Users** tab
5. You should see the user you registered in the list

## Step 9: Configure Database Security Rules (Important!)

The default test mode rules allow anyone to read/write your database. You should update these rules for production:

1. In the Firebase Console, click **Realtime Database**
2. Click the **Rules** tab
3. Replace the rules with:

```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    }
  }
}
```

4. Click **Publish**

These rules ensure that:
- Only authenticated users can access their own user data
- Users cannot read or write other users' data

**Important:** The application automatically includes the authentication token with all database requests by creating authenticated FirebaseClient instances (using `FirebaseOptions` with `AuthTokenAsyncFactory`), so these security rules will work correctly for both registration and login operations.

## Architecture Overview

### Project Structure

- **myFlatLightLogin.Dal**: Abstract DAL layer with interfaces and DTOs
- **myFlatLightLogin.DalFirebase**: Firebase implementation of the DAL
- **myFlatLightLogin.DalSQLite**: SQLite implementation of the DAL (for offline use)
- **myFlatLightLogin.UI.Wpf**: WPF user interface
- **myFlatLightLogin.Core**: Core MVVM components and utilities

### How It Works

1. **User Registration** (`RegisterUserViewModel.cs`):
   - User enters Name, Lastname, Email, and Password
   - `UserDal.Insert()` creates a Firebase Authentication user
   - User profile (Name, Lastname) is stored in Firebase Realtime Database

2. **User Login** (`LoginViewModel.cs`):
   - User enters Email and Password
   - `UserDal.SignInAsync()` authenticates with Firebase
   - User profile is loaded from Realtime Database
   - User is navigated to the main application

3. **Password Security**:
   - Passwords are handled securely using WPF's PasswordBox control
   - Passwords are transmitted securely to Firebase over HTTPS
   - Firebase handles password hashing and storage

### Key Components

**FirebaseConfig.cs** (`myFlatLightLogin.DalFirebase/FirebaseConfig.cs:1`)
- Loads Firebase configuration from appsettings.json or User Secrets
- Validates that configuration values are properly set
- Automatically prevents placeholder values from being used

**UserDal.cs** (`myFlatLightLogin.DalFirebase/UserDal.cs:1`)
- Implements `IUserDal` interface
- Handles Firebase Authentication and Realtime Database operations
- Methods: `SignInAsync()`, `Insert()`, `Update()`, `Delete()`

**LoginViewModel.cs** (`myFlatLightLogin.UI.Wpf/MVVM/ViewModel/LoginViewModel.cs:1`)
- Handles user login UI logic
- Uses `AsyncRelayCommand` for async operations
- Binds to `Login.xaml` view

**RegisterUserViewModel.cs** (`myFlatLightLogin.UI.Wpf/MVVM/ViewModel/RegisterUserViewModel.cs:1`)
- Handles user registration UI logic
- Validates password matching
- Binds to `RegisterUser.xaml` view

## Troubleshooting

### Build Errors

**Error: Package restore failed**
- Solution: Right-click the solution → **Restore NuGet Packages**

**Error: Configuration packages missing**
- Solution: Make sure the following NuGet packages are installed in DalFirebase project:
  - Microsoft.Extensions.Configuration
  - Microsoft.Extensions.Configuration.Json
  - Microsoft.Extensions.Configuration.UserSecrets

### Runtime Errors

**Error: "Firebase configuration 'Firebase:ApiKey' is not set"**
- This means your configuration file is missing or incomplete
- Solution: Follow Step 6 to create `appsettings.json` with your Firebase credentials
- Or use User Secrets (Option B in Step 6)

**Error: "Firebase Authentication failed"**
- Check your API Key in `appsettings.json` or User Secrets
- Ensure Email/Password authentication is enabled in Firebase Console
- Check your internet connection

**Error: "Database operation failed"**
- Check your Database URL in `appsettings.json` or User Secrets
- Ensure the Realtime Database exists in Firebase Console
- Check database security rules

**Error: "Permission denied"**
- This happens when database security rules are enabled but requests aren't authenticated
- Make sure you're on the latest version of the code (with FirebaseOptions authentication)
- Check that your security rules in Firebase Console match Step 9

**Error: "User already exists"**
- This is expected if you try to register with an email that's already registered
- Try logging in instead, or use a different email

### Firebase Console

**Can't see registered users**
- Go to **Authentication** → **Users** tab in Firebase Console
- Make sure you're looking at the correct project

**Database is empty**
- Go to **Realtime Database** → **Data** tab in Firebase Console
- After registering a user, you should see a `users` node with user data

## Security Considerations

1. **✅ Secure Configuration Storage**
   - Firebase credentials are stored in `appsettings.json` which is **automatically excluded from Git**
   - Never commit `appsettings.json` to version control (already protected via `.gitignore`)
   - The `appsettings.template.json` file (which contains no secrets) is safe to commit
   - For extra security during development, use User Secrets (Option B in Step 6)

2. **✅ Configuration Validation**
   - The application automatically validates that configuration values are set
   - Placeholder values like "YOUR_API_KEY_HERE" are automatically detected and rejected
   - Clear error messages guide you to fix configuration issues

3. **⚠️ Database Security Rules (Critical)**
   - The default test mode rules are NOT secure for production
   - **You MUST follow Step 9** to configure proper security rules
   - Without proper rules, anyone can read/write your database

4. **✅ HTTPS Only**
   - Firebase automatically uses HTTPS for all connections
   - All data transmission is encrypted
   - Never disable SSL/TLS verification

5. **⚠️ Password Requirements**
   - Firebase requires passwords to be at least 6 characters
   - Consider adding additional password strength requirements in your app
   - Consider implementing password reset and email verification

6. **✅ Source Control Safety**
   - `.gitignore` is configured to exclude all `appsettings.json` files
   - Only `appsettings.template.json` (without secrets) is tracked in Git
   - User Secrets are stored outside the project folder and never committed

## Next Steps

1. **Offline Support**: Configure the application to use SQLite when offline
2. **Data Synchronization**: Implement sync between Firebase and SQLite
3. **User Profile**: Add user profile editing functionality
4. **Password Reset**: Implement Firebase password reset functionality
5. **Email Verification**: Enable email verification for new users

## Useful Links

- [Firebase Documentation](https://firebase.google.com/docs)
- [Firebase Authentication Documentation](https://firebase.google.com/docs/auth)
- [Firebase Realtime Database Documentation](https://firebase.google.com/docs/database)
- [FirebaseAuthentication.net Library](https://github.com/step-up-labs/firebase-authentication-dotnet)
- [FirebaseDatabase.net Library](https://github.com/step-up-labs/firebase-database-dotnet)

## Support

If you encounter any issues:
1. Check the troubleshooting section above
2. Review the Firebase Console for errors
3. Check the Visual Studio Output window for detailed error messages
4. Review the Firebase documentation
