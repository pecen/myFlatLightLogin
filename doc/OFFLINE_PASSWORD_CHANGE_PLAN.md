# Offline Password Change - Implementation Plan

## Overview

This document outlines the implementation plan for allowing users to change their password while offline, with proper Firebase synchronization when they come back online.

## User Flow

### Scenario 1: Online Password Change
1. User is logged in and online
2. User opens "Change Password" dialog
3. Enters: Current Password, New Password, Confirm New Password
4. System validates current password with Firebase
5. System updates password in Firebase immediately
6. System updates password hash in SQLite
7. Success message shown

### Scenario 2: Offline Password Change
1. User is logged in but offline
2. User opens "Change Password" dialog
3. **WARNING DIALOG SHOWN:**
   ```
   You are currently offline.

   If you change your password now, you will need to remember your OLD password
   when you come back online to sync with Firebase.

   Recommendation: Wait until you're online to change your password.

   Do you want to proceed?
   [Cancel] [Change Password Anyway]
   ```
4. If user proceeds:
   - Current password validated against SQLite hash
   - New password hash stored in SQLite
   - **Old password hash** stored in `OldPasswordHash` field
   - `PendingPasswordChange = true`
   - `PasswordChangedDate = DateTime.UtcNow`
   - `NeedsSync = true`
5. User can continue using app with new password (offline mode)

### Scenario 3: Sync After Offline Password Change
1. User comes back online
2. Automatic sync detects user with `PendingPasswordChange = true`
3. **PASSWORD SYNC DIALOG SHOWN:**
   ```
   Your password was changed while offline and needs to be synced with Firebase.

   Please enter your OLD password (before the change):

   Old Password: [________]

   [Cancel] [Sync Password]
   ```
4. User enters old password
5. System validates old password against `OldPasswordHash`
6. If valid:
   - System authenticates with Firebase using old password
   - System updates Firebase password to new password
   - System clears `OldPasswordHash`
   - System sets `PendingPasswordChange = false`
   - System sets `NeedsSync = false`
   - Success message shown
7. If invalid:
   - Error message: "Old password is incorrect. Cannot sync password."
   - User remains with pending password change
   - User can try again later

## Implementation Components

### 1. Database Fields ✅ DONE

Already implemented:
- `User.PendingPasswordChange` (bool)
- `User.OldPasswordHash` (string)
- `User.PasswordChangedDate` (string)
- `UserDto.PendingPasswordChange` (bool)
- `UserDto.OldPasswordHash` (string)
- `UserDto.PasswordChangedDate` (string)

### 2. Change Password UI

**New View: `ChangePassword.xaml`**
- TextBox: Current Password (PasswordBox)
- TextBox: New Password (PasswordBox)
- TextBox: Confirm New Password (PasswordBox)
- Button: Change Password
- Button: Cancel
- Connection status indicator (🟢 Online / 🔴 Offline)

**Location:** `src/myFlatLightLogin.UI.Wpf/MVVM/View/ChangePassword.xaml`

### 3. Change Password ViewModel

**New ViewModel: `ChangePasswordViewModel.cs`**

**Properties:**
```csharp
public string CurrentPassword { get; set; }
public string NewPassword { get; set; }
public string ConfirmNewPassword { get; set; }
public bool IsOnline => _connectivityService.IsOnline;
public string ConnectionStatus => IsOnline ? "🟢 Online" : "🔴 Offline";
```

**Commands:**
```csharp
public AsyncRelayCommand ChangePasswordCommand { get; set; }
public RelayCommand CancelCommand { get; set; }
```

**Key Methods:**
```csharp
private async Task ChangePasswordAsync()
{
    // 1. Validate inputs
    if (!ValidateInputs()) return;

    // 2. Check if online or offline
    if (_connectivityService.IsOnline)
    {
        await ChangePasswordOnlineAsync();
    }
    else
    {
        await ChangePasswordOfflineAsync();
    }
}

private async Task ChangePasswordOnlineAsync()
{
    // Online flow:
    // 1. Verify current password with Firebase
    // 2. Update Firebase password
    // 3. Update SQLite password hash
    // 4. Show success
}

private async Task ChangePasswordOfflineAsync()
{
    // Offline flow:
    // 1. Show warning dialog
    // 2. If user confirms:
    //    - Verify current password with SQLite
    //    - Store old password hash
    //    - Update to new password hash
    //    - Set PendingPasswordChange = true
    //    - Show success with reminder
}
```

**Location:** `src/myFlatLightLogin.UI.Wpf/MVVM/ViewModel/ChangePasswordViewModel.cs`

### 4. Add "Change Password" to Home View

**Update: `Home.xaml`**
Add button to navigate to Change Password view.

### 5. Update HybridUserDal

**New Method: `ChangePasswordAsync()`**

```csharp
/// <summary>
/// Changes user password with online/offline support.
/// </summary>
/// <param name="userId">User ID</param>
/// <param name="currentPassword">Current password (plain text)</param>
/// <param name="newPassword">New password (plain text)</param>
/// <returns>PasswordChangeResult with success/failure details</returns>
public async Task<PasswordChangeResult> ChangePasswordAsync(
    int userId,
    string currentPassword,
    string newPassword)
{
    if (_connectivityService.IsOnline)
    {
        // Online: Update Firebase immediately
        return await ChangePasswordOnlineAsync(userId, currentPassword, newPassword);
    }
    else
    {
        // Offline: Store for later sync
        return ChangePasswordOffline(userId, currentPassword, newPassword);
    }
}

private async Task<PasswordChangeResult> ChangePasswordOnlineAsync(...)
{
    try
    {
        // 1. Get user from SQLite
        var user = _sqliteDal.Fetch(userId);

        // 2. Authenticate with Firebase using current password
        var authResult = await _firebaseDal.SignInAsync(user.Email, currentPassword);
        if (authResult == null)
            return PasswordChangeResult.Failure("Current password is incorrect");

        // 3. Update Firebase password
        await _firebaseDal.UpdatePasswordAsync(newPassword);

        // 4. Update SQLite password hash
        user.Password = SecurityHelper.HashPassword(newPassword);
        user.PasswordChangedDate = DateTime.UtcNow.ToString("o");
        _sqliteDal.Update(user);

        return PasswordChangeResult.Success("Password changed successfully");
    }
    catch (Exception ex)
    {
        return PasswordChangeResult.Failure($"Failed to change password: {ex.Message}");
    }
}

private PasswordChangeResult ChangePasswordOffline(...)
{
    try
    {
        // 1. Get user from SQLite
        var user = _sqliteDal.Fetch(userId);

        // 2. Verify current password against SQLite hash
        var currentHash = SecurityHelper.HashPassword(currentPassword);
        if (user.Password != currentHash)
            return PasswordChangeResult.Failure("Current password is incorrect");

        // 3. Store old password hash
        user.OldPasswordHash = user.Password;

        // 4. Update to new password hash
        user.Password = SecurityHelper.HashPassword(newPassword);
        user.PendingPasswordChange = true;
        user.PasswordChangedDate = DateTime.UtcNow.ToString("o");
        user.NeedsSync = true;

        // 5. Update SQLite
        _sqliteDal.Update(user);

        return PasswordChangeResult.OfflineSuccess(
            "Password changed offline. You will need your OLD password to sync when online.");
    }
    catch (Exception ex)
    {
        return PasswordChangeResult.Failure($"Failed to change password: {ex.Message}");
    }
}
```

### 6. Create PasswordChangeResult Class

**New Class: `PasswordChangeResult.cs`**

```csharp
public class PasswordChangeResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public bool IsOfflineChange { get; set; }

    public static PasswordChangeResult Success(string message) =>
        new PasswordChangeResult { Success = true, Message = message };

    public static PasswordChangeResult OfflineSuccess(string message) =>
        new PasswordChangeResult { Success = true, Message = message, IsOfflineChange = true };

    public static PasswordChangeResult Failure(string message) =>
        new PasswordChangeResult { Success = false, Message = message };
}
```

**Location:** `src/myFlatLightLogin.Dal/PasswordChangeResult.cs`

### 7. Add Firebase Password Update Method

**Update: `FirebaseUserDal.cs`**

```csharp
/// <summary>
/// Updates the current user's password in Firebase Authentication.
/// Requires an active authenticated session.
/// </summary>
public async Task<bool> UpdatePasswordAsync(string newPassword)
{
    try
    {
        if (_currentUser?.User == null)
            throw new InvalidOperationException("No authenticated user");

        // Use Firebase Authentication API to update password
        // Note: This requires the user to be recently authenticated
        await _authClient.ChangePasswordAsync(_currentUser.User.Credential.IdToken, newPassword);

        return true;
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed to update password: {ex.Message}", ex);
    }
}

/// <summary>
/// Updates user password using old password for authentication.
/// Used for syncing password changes made offline.
/// </summary>
public async Task<bool> UpdatePasswordWithOldPasswordAsync(
    string email,
    string oldPassword,
    string newPassword)
{
    try
    {
        // 1. Sign in with old password to get fresh credentials
        var credential = await _authClient.SignInWithEmailAndPasswordAsync(email, oldPassword);

        if (credential?.User == null)
            return false;

        // 2. Update to new password
        await _authClient.ChangePasswordAsync(credential.User.Credential.IdToken, newPassword);

        // 3. Update current user session
        _currentUser = credential;

        return true;
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed to update password: {ex.Message}", ex);
    }
}
```

### 8. Update SyncService for Password Sync

**Update: `SyncService.cs`**

Add password change detection and handling:

```csharp
private async Task<SyncOperationResult> UploadToFirebaseAsync()
{
    var result = new SyncOperationResult();

    try
    {
        var usersNeedingSync = _sqliteDal.GetUsersNeedingSync();

        if (usersNeedingSync == null || usersNeedingSync.Count == 0)
        {
            result.Count = 0;
            result.Success = true;
            return result;
        }

        int successCount = 0;
        int failureCount = 0;

        foreach (var user in usersNeedingSync)
        {
            try
            {
                // Check if this is a password change sync
                if (user.PendingPasswordChange)
                {
                    // Password changes require user interaction - skip automatic sync
                    // This will be handled by interactive password sync dialog
                    continue;
                }

                // ... existing sync logic for new users ...
            }
            catch (Exception ex)
            {
                failureCount++;
                result.ErrorMessage += $"Failed to sync user {user.Email}: {ex.Message}; ";
            }
        }

        result.Count = successCount;
        result.Success = failureCount == 0;

        return result;
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.ErrorMessage = ex.Message;
        return result;
    }
}

/// <summary>
/// Gets users with pending password changes that need interactive sync.
/// </summary>
public List<UserDto> GetUsersWithPendingPasswordChanges()
{
    return _sqliteDal.GetUsersWithPendingPasswordChanges();
}
```

### 9. Add Password Sync Dialog

**New View: `PasswordSyncDialog.xaml`**

Simple dialog for prompting old password during sync.

**Properties:**
```xaml
<TextBlock>
    Your password was changed while offline and needs to be synced.

    Please enter your OLD password (before the change):
</TextBlock>

<PasswordBox x:Name="OldPasswordBox" />

<StackPanel Orientation="Horizontal">
    <Button Content="Cancel" />
    <Button Content="Sync Password" />
</StackPanel>
```

**Location:** `src/myFlatLightLogin.UI.Wpf/MVVM/View/PasswordSyncDialog.xaml`

### 10. Add Password Sync Detection on Startup/Login

**Update: `App.xaml.cs` or `LoginViewModel.cs`**

After successful login, check for pending password changes:

```csharp
// After login succeeds
var pendingPasswordChanges = _syncService.GetUsersWithPendingPasswordChanges();

if (pendingPasswordChanges.Any(u => u.Email == currentUser.Email))
{
    // Show password sync dialog
    var dialog = new PasswordSyncDialog(currentUser);
    var result = await dialog.ShowAsync();

    if (result == DialogResult.OK)
    {
        // Password synced successfully
    }
}
```

### 11. Update SQLite UserDal

**Add New Methods:**

```csharp
/// <summary>
/// Gets all users with pending password changes.
/// </summary>
public List<UserDto> GetUsersWithPendingPasswordChanges()
{
    using var conn = new SQLiteConnection(DbPath);
    var users = conn.Table<User>()
        .Where(u => u.PendingPasswordChange == true)
        .ToList();

    return users.Select(u => ConvertToDto(u)).ToList();
}

/// <summary>
/// Clears pending password change after successful sync.
/// </summary>
public bool ClearPendingPasswordChange(int userId)
{
    using var conn = new SQLiteConnection(DbPath);
    var user = conn.Find<User>(userId);

    if (user != null)
    {
        user.PendingPasswordChange = false;
        user.OldPasswordHash = null;
        conn.Update(user);
        return true;
    }

    return false;
}
```

## Implementation Order

### Phase 1: Core Backend (Recommended to do first)
1. ✅ Database fields (DONE)
2. Add `PasswordChangeResult` class
3. Add Firebase password update methods
4. Add `ChangePasswordAsync()` to HybridUserDal
5. Add SQLite methods for password change queries
6. Update SyncService to detect password changes

### Phase 2: UI Components
7. Create `ChangePassword.xaml` view
8. Create `ChangePasswordViewModel`
9. Add "Change Password" button to Home view
10. Create `PasswordSyncDialog.xaml`
11. Create `PasswordSyncDialogViewModel`

### Phase 3: Integration
12. Wire up password change navigation
13. Add password sync detection on login
14. Add password sync detection on connectivity restore
15. Test offline/online flows

## Testing Scenarios

### Test 1: Online Password Change
1. User is online and logged in
2. User opens Change Password
3. Enters current, new, confirm passwords
4. Password changes immediately in Firebase
5. Password hash updated in SQLite
6. User can log in with new password

### Test 2: Offline Password Change
1. User is offline (disconnect WiFi)
2. User opens Change Password
3. Warning dialog appears
4. User proceeds with change
5. Password hash updated in SQLite
6. `PendingPasswordChange = true`
7. User can log in offline with new password

### Test 3: Sync After Offline Change
1. User changed password offline
2. User comes back online (reconnect WiFi)
3. User logs in
4. Password sync dialog appears
5. User enters OLD password correctly
6. Password syncs to Firebase
7. Old password hash cleared
8. User can log in with new password (online)

### Test 4: Wrong Old Password During Sync
1. User changed password offline
2. User comes back online
3. User logs in
4. Password sync dialog appears
5. User enters WRONG old password
6. Error shown: "Old password is incorrect"
7. `PendingPasswordChange` remains true
8. User can try again later

## Edge Cases to Handle

1. **User forgets old password:**
   - Option 1: Provide "Reset Password" link (requires email)
   - Option 2: Contact admin to manually sync
   - Option 3: Allow user to continue with offline-only access

2. **Multiple password changes offline:**
   - Only track the most recent change
   - Old password hash is from BEFORE the first change

3. **Password change while sync is in progress:**
   - Disable password change button during sync
   - Or queue password change for next sync

4. **User changes password online after offline change:**
   - Clear `PendingPasswordChange` flag
   - New password takes precedence

## Security Considerations

1. **Old password hash storage:**
   - Stored temporarily in SQLite
   - Cleared immediately after successful sync
   - Same SHA256 hashing as regular passwords

2. **Password transmission:**
   - Always sent over HTTPS to Firebase
   - Never logged or displayed in UI
   - Never stored in plain text

3. **Validation:**
   - Current password verified before change
   - New password meets Firebase requirements (6+ chars)
   - Confirm password must match new password

4. **Session security:**
   - Password change requires active session
   - Re-authentication with old password for sync

## UI/UX Considerations

1. **Warning Dialog (Offline):**
   - Clear explanation of implications
   - Emphasize need to remember old password
   - Recommend waiting until online

2. **Password Sync Dialog:**
   - Simple and clear
   - Show username/email for context
   - Allow cancel (user can sync later)

3. **Feedback Messages:**
   - Success: "Password changed successfully"
   - Offline success: "Password changed. Remember your old password for sync."
   - Sync success: "Password synced to Firebase successfully"
   - Error: Specific error messages

4. **Progress Indicators:**
   - Show spinner during Firebase operations
   - Disable buttons during processing
   - Connection status always visible

## Questions for Discussion / Refinement

### Architecture Questions:
1. Should password change be in Home view or a separate dialog/page?
2. Should we automatically show password sync dialog on login, or require manual sync?
3. How should we handle users who never sync their password?

### UX Questions:
4. Should we block offline password changes entirely for simplicity?
5. Should we auto-trigger sync on connectivity restore, or wait for user login?
6. What should happen if user changes password multiple times offline?

### Technical Questions:
7. Do we need password strength validation?
8. Should we add password change history/auditing?
9. Should admins be able to force password reset for users?

### Security Questions:
10. How long should we keep the old password hash?
11. Should we limit password change frequency?
12. Do we need additional authentication (e.g., security questions)?

## Next Steps

1. **Review this plan** - Identify any concerns or changes needed
2. **Discuss refinements** - Address the questions above
3. **Prioritize features** - Decide what's MVP vs. nice-to-have
4. **Begin implementation** - Start with Phase 1 (backend)
5. **Iterate based on feedback** - Adjust as we discover issues

## Estimated Effort

- **Phase 1 (Backend):** 2-3 hours
- **Phase 2 (UI):** 2-3 hours
- **Phase 3 (Integration):** 1-2 hours
- **Testing & Refinement:** 2-3 hours
- **Total:** ~8-11 hours

## Files That Will Be Created/Modified

### New Files:
- `src/myFlatLightLogin.Dal/PasswordChangeResult.cs`
- `src/myFlatLightLogin.UI.Wpf/MVVM/View/ChangePassword.xaml`
- `src/myFlatLightLogin.UI.Wpf/MVVM/View/ChangePassword.xaml.cs`
- `src/myFlatLightLogin.UI.Wpf/MVVM/ViewModel/ChangePasswordViewModel.cs`
- `src/myFlatLightLogin.UI.Wpf/MVVM/View/PasswordSyncDialog.xaml`
- `src/myFlatLightLogin.UI.Wpf/MVVM/View/PasswordSyncDialog.xaml.cs`
- `src/myFlatLightLogin.UI.Wpf/MVVM/ViewModel/PasswordSyncDialogViewModel.cs`

### Modified Files:
- `src/myFlatLightLogin.Core/Services/HybridUserDal.cs`
- `src/myFlatLightLogin.Core/Services/SyncService.cs`
- `src/myFlatLightLogin.DalFirebase/UserDal.cs`
- `src/myFlatLightLogin.DalSQLite/UserDal.cs`
- `src/myFlatLightLogin.UI.Wpf/MVVM/View/Home.xaml`
- `src/myFlatLightLogin.UI.Wpf/MVVM/ViewModel/LoginViewModel.cs`
- `src/myFlatLightLogin.UI.Wpf/App.xaml.cs`

---

**Ready for discussion!** Please review and let me know your thoughts, concerns, or suggested changes.
