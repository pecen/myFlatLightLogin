# Automatic Sync Implementation

This document describes the automatic synchronization features implemented to sync offline-created users to Firebase.

## Features Implemented

### 1. ✅ Automatic Sync on Connectivity Restore (All Users)

**Location:** `src/myFlatLightLogin.UI.Wpf/App.xaml.cs:193-229`

When the application detects that network connectivity has been restored (WiFi/Ethernet comes back online), it automatically checks for any users that need to be synced to Firebase and uploads them.

**How it works:**
- The `NetworkConnectivityService` monitors network changes in real-time
- When connectivity changes from offline → online, the `OnConnectivityChanged` event fires
- The app automatically checks `GetPendingSyncCount()` to see if there are users needing sync
- If users are pending, it triggers `SyncService.SyncAsync()` automatically
- All operations happen in the background without blocking the UI
- Results are logged for troubleshooting

**User Experience:**
- Transparent - happens automatically in the background
- No user action required
- Logs show: "Connection restored - found X users pending sync, starting automatic sync..."

### 2. ✅ Automatic Sync on App Startup (All Users)

**Location:** `src/myFlatLightLogin.UI.Wpf/App.xaml.cs:129-167`

When the application starts and the device is online, it automatically checks for and syncs any pending users.

**How it works:**
- During `OnStartup`, the app checks if the device is online
- If online, it checks for pending users using `GetPendingSyncCount()`
- If users are pending, it triggers `SyncService.SyncAsync()` automatically
- Runs in the background to avoid blocking UI startup
- Results are logged for troubleshooting

**User Experience:**
- Transparent - happens automatically on startup
- No user action required
- Doesn't block the main window from appearing
- Logs show: "App startup - checking for pending sync..."

### 3. ✅ Manual "Sync Now" Button (Admin Only)

**Location:**
- ViewModel: `src/myFlatLightLogin.UI.Wpf/MVVM/ViewModel/MainWindowViewModel.cs:253-301`
- View: `src/myFlatLightLogin.UI.Wpf/MVVM/View/MainWindow.xaml:161-175`

Administrators can manually trigger a sync operation at any time using a "Sync Now" button in the UI.

**How it works:**
- Button is only visible when `IsUserAdministrator = true`
- Command: `SyncNowCommand` (async)
- When clicked, triggers `SyncService.SyncAsync()`
- Shows a dialog with sync results (users uploaded, duration)
- Icon spins while syncing is in progress
- Button is disabled while sync is active

**User Experience:**
- Admin logs in → sees "Sync Now" button in bottom toolbar
- Click button → sync starts
- Icon spins during sync
- Dialog shows results: "Sync completed successfully! Users uploaded: X, Duration: Y seconds"

**UI Elements:**
- Button with spinning sync icon (MahApps.Metro icon)
- Located in bottom toolbar, before "View Logs" button
- Only visible to administrators

### 4. ✅ Sync Status Display (Admin Only)

**Location:**
- ViewModel: `src/myFlatLightLogin.UI.Wpf/MVVM/ViewModel/MainWindowViewModel.cs:44-72, 303-362`
- View: `src/myFlatLightLogin.UI.Wpf/MVVM/View/MainWindow.xaml:153-160`

Administrators can see real-time sync status in the UI, showing how many users are pending sync.

**How it works:**
- Properties:
  - `PendingSyncCount` - number of users needing sync
  - `SyncStatusMessage` - friendly status message
  - `IsSyncing` - whether sync is currently active
- Updates automatically:
  - When user logs in/out
  - When sync starts/completes
  - When connectivity changes
- Subscribes to `SyncService` events for real-time updates

**User Experience:**
- Admin logs in → sees sync status: "Sync: All synced" or "Sync: 3 user(s) pending sync"
- Status updates in real-time as sync progresses
- During sync: "Sync: Syncing..." or "Sync: Uploading to Firebase..."
- After sync: "Sync: All synced"

**UI Elements:**
- TextBlock showing status message
- Located in bottom toolbar, next to "Sync Now" button
- Only visible to administrators
- Format: "Sync: **[status message]**"

## Architecture Changes

### Dependency Injection Updates

**File:** `src/myFlatLightLogin.UI.Wpf/App.xaml.cs:37-66`

Registered new singleton services in the DI container:
- `NetworkConnectivityService` - for monitoring network connectivity
- `SyncService` - for synchronization operations
- `HybridUserDal` - for offline-first data access

These services are now shared across the application and injected where needed.

### ViewModel Updates

**File:** `src/myFlatLightLogin.UI.Wpf/MVVM/ViewModel/MainWindowViewModel.cs`

**Constructor Changes:**
```csharp
// Old
public MainWindowViewModel(INavigationService navigationService, LoginViewModel loginViewModel)

// New
public MainWindowViewModel(INavigationService navigationService, LoginViewModel loginViewModel,
    HybridUserDal hybridUserDal, SyncService syncService)
```

**New Properties:**
- `PendingSyncCount` (int) - number of users pending sync
- `SyncStatusMessage` (string) - current sync status message
- `IsSyncing` (bool) - whether sync is in progress

**New Commands:**
- `SyncNowCommand` (AsyncRelayCommand) - triggers manual sync

**New Methods:**
- `SyncNowAsync()` - executes manual sync with UI feedback
- `RefreshSyncStatus()` - updates sync status display
- `OnSyncStarted()` - handles sync started event
- `OnSyncCompleted()` - handles sync completed event
- `OnSyncProgress()` - handles sync progress updates

**Event Subscriptions:**
- Subscribes to `SyncService.SyncStarted`
- Subscribes to `SyncService.SyncCompleted`
- Subscribes to `SyncService.SyncProgress`

## Testing Instructions

### Test Scenario 1: Offline Registration + Connectivity Restore

1. **Disconnect from internet** (disable WiFi)
2. Open the application
3. Register a new user (e.g., `testuser@example.com`)
4. Verify user is saved to SQLite: Check `security.db3` or sign in locally
5. **Reconnect to internet** (enable WiFi)
6. Check application logs - should see: "Connection restored - found 1 users pending sync, starting automatic sync..."
7. Wait a few seconds for sync to complete
8. Check Firebase Console → Authentication → Users
9. Verify `testuser@example.com` appears in Firebase

**Expected Result:** ✅ User automatically synced to Firebase when connection restored

### Test Scenario 2: Offline Registration + App Restart

1. **Disconnect from internet**
2. Open the application
3. Register a new user (e.g., `testuser2@example.com`)
4. Close the application
5. **Reconnect to internet**
6. Reopen the application
7. Check application logs - should see: "App startup - checking for pending sync..."
8. Wait a few seconds for sync to complete
9. Check Firebase Console → verify user appears

**Expected Result:** ✅ User automatically synced to Firebase on app startup

### Test Scenario 3: Manual Sync (Admin)

1. Register as first user (becomes admin automatically)
2. Log in as admin
3. Look at bottom toolbar → should see:
   - "Sync: All synced" (or "X user(s) pending sync")
   - "Sync Now" button with sync icon
4. **Disconnect from internet**
5. Register another user (e.g., `testuser3@example.com`)
6. Status should update to: "Sync: 1 user(s) pending sync"
7. **Reconnect to internet**
8. Click "Sync Now" button
9. Sync icon should spin
10. Dialog appears: "Sync completed successfully! Users uploaded: 1"
11. Status updates to: "Sync: All synced"
12. Check Firebase Console → verify user appears

**Expected Result:** ✅ Admin can manually trigger sync and see results

### Test Scenario 4: Sync Status Visibility (Non-Admin)

1. Create a second user account (not admin)
2. Log in as non-admin user
3. Look at bottom toolbar
4. Verify:
   - "Sync Now" button is **NOT visible**
   - Sync status message is **NOT visible**
   - Only regular menu buttons are shown

**Expected Result:** ✅ Sync features are admin-only

### Test Scenario 5: Multiple Offline Users

1. **Disconnect from internet**
2. Register 3 users offline
3. Status should show: "Sync: 3 user(s) pending sync"
4. **Reconnect to internet**
5. Automatic sync should start
6. All 3 users should be uploaded to Firebase
7. Status updates to: "Sync: All synced"

**Expected Result:** ✅ Multiple users synced in batch

## Logging

All sync operations are logged using Serilog. Check logs at:
- Location: `[Application Directory]/logs/myFlatLightLogin-[date].log`

**Log Messages to Look For:**

**Connectivity Changes:**
```
Connectivity changed: IsOnline = True
Connection restored - found 2 users pending sync, starting automatic sync...
Automatic sync completed successfully. Uploaded: 2
```

**App Startup:**
```
App startup - checking for pending sync...
Found 1 users pending sync, starting sync...
Startup sync completed successfully. Uploaded: 1
```

**Manual Sync:**
```
[Admin triggered manual sync]
Sync started
Uploading to Firebase...
Sync completed successfully. Uploaded: 1
```

## Troubleshooting

### Issue: Sync not happening automatically

**Check:**
1. Is the device actually online? Check `NetworkConnectivityService.IsOnline`
2. Are there users pending sync? Check logs for "No users pending sync"
3. Can the app reach Firebase? Check logs for Firebase connectivity tests
4. Check logs for any exceptions during sync

### Issue: "Sync Now" button not visible

**Check:**
1. Is the user logged in as Admin?
2. Check `CurrentUserService.Instance.IsAdmin`
3. First registered user should automatically be Admin

### Issue: Sync fails with errors

**Common causes:**
1. Firebase configuration not set (check `appsettings.json`)
2. Firebase Realtime Database rules too restrictive
3. Network firewall blocking Firebase access
4. Invalid email format or password too short (Firebase requires 6+ chars)

**Check logs for specific error messages:**
- "Firebase configuration 'Firebase:ApiKey' is not set"
- "Permission denied" - check Firebase security rules
- "Email already exists" - user already registered

### Issue: Status shows wrong count

**Solution:**
- The count is cached and refreshes on:
  - User login/logout
  - Sync completion
  - Manual refresh (not yet implemented)
- Close and reopen the app to force refresh

## Security Considerations

### Admin-Only Features

All sync UI features are restricted to administrators only:
- Button visibility: `Visibility="{Binding IsUserAdministrator, Converter={StaticResource BoolToVis}}"`
- Command availability: `SyncNowCommand = new AsyncRelayCommand(SyncNowAsync, () => IsUserAdministrator && !IsSyncing)`
- First registered user automatically becomes Admin
- Non-admin users cannot see or access sync features

### Sync Security

- Sync operations use authenticated Firebase connections
- Each user can only sync their own data (enforced by Firebase rules)
- Passwords are hashed before storage (SHA256 for SQLite)
- Firebase handles password security with industry-standard bcrypt
- All sync operations are logged for audit purposes

## Performance

### Sync Timing

- **Connectivity restore:** Sync triggers within 1-2 seconds of connectivity change
- **App startup:** Sync starts within 1-2 seconds after window appears
- **Manual sync:** Immediate response to button click

### Network Efficiency

- Only syncs users with `NeedsSync = true` flag
- Batch operations minimize Firebase API calls
- No redundant syncs (users marked as synced after successful upload)
- Background operations don't block UI thread

### UI Responsiveness

- All sync operations are async (non-blocking)
- UI updates via `Dispatcher.Invoke()` for thread safety
- Progress indicator shows sync is in progress
- Button disabled during sync to prevent duplicate operations

## Future Enhancements

### Potential Improvements

1. **Periodic sync timer** - Sync every 5-10 minutes when online
2. **Sync conflict resolution UI** - Let users choose which version to keep
3. **Sync history log** - Show past sync operations
4. **Sync notifications** - Toast notifications for sync completion
5. **Selective sync** - Let admins choose which users to sync
6. **Background sync service** - Windows service for always-on sync
7. **Real-time Firebase sync** - Use Firebase listeners for instant updates
8. **Sync scheduling** - Configure sync frequency and timing

## Conclusion

The automatic sync implementation ensures that users created offline are automatically uploaded to Firebase as soon as connectivity is restored. The system is:

- ✅ **Automatic** - No user intervention required
- ✅ **Transparent** - Works in the background
- ✅ **Reliable** - Multiple sync triggers ensure data is uploaded
- ✅ **Admin-Friendly** - Admins have full visibility and control
- ✅ **Secure** - Admin-only features, authenticated operations
- ✅ **Logged** - All operations logged for troubleshooting

Users can now work offline with confidence, knowing their data will automatically sync when they're back online! 🎉
