# Testing Guide - Offline Password Change Feature

## Overview
This guide covers testing the complete offline password change feature, including automatic sync, password change UI, and password sync dialog.

---

## Prerequisites

### Test Environment Setup
1. **Firebase Account**: Ensure you have access to your Firebase project
2. **Test User Account**: Create a test user account for testing (or use existing)
3. **Network Control**: Ability to toggle airplane mode or disconnect network
4. **Clean State**: Fresh SQLite database recommended (backup existing if needed)

### Logging
The application uses Serilog. Check logs at:
- Console output during runtime
- Log files (if configured in your Serilog settings)

Look for log entries with keywords: `Password`, `Sync`, `Firebase`, `Offline`

---

## Test Scenarios

### Scenario 1: Online Password Change (Happy Path)

**Goal**: Verify password changes work correctly when online

**Steps**:
1. Start the application with internet connected
2. Log in with a test user
3. Navigate to "Change Password" from Home screen
4. Verify connection indicator shows 🟢 (online)
5. Enter current password, new password, and confirm password
6. Click "Change Password"
7. Log out
8. Log back in with the **new password**

**Expected Results**:
- ✅ Success message appears: "Password changed successfully"
- ✅ No warning dialog about offline changes
- ✅ Logout successful
- ✅ Login with new password successful
- ✅ Login with old password fails
- ✅ No "pending password change" dialog appears

**Verify in Database**:
```sql
-- Check SQLite database
SELECT Id, Email, PendingPasswordChange, OldPasswordHash, PasswordChangedDate, NeedsSync
FROM User
WHERE Email = 'your-test-email@example.com';
```
- `PendingPasswordChange` should be `0` (false)
- `OldPasswordHash` should be `NULL`
- `PasswordChangedDate` should be updated
- `NeedsSync` should be `0` (false)

**Verify in Firebase**:
- Go to Firebase Console → Authentication → Users
- Find your test user
- Verify "Last sign-in" is recent (just logged in with new password)

---

### Scenario 2: Offline Password Change (Happy Path)

**Goal**: Verify password changes are stored locally when offline

**Steps**:
1. Start the application
2. Log in with a test user (while online)
3. **Disconnect from internet** (airplane mode or disable network adapter)
4. Navigate to "Change Password" from Home screen
5. Verify connection indicator shows 🔴 (offline)
6. Enter current password, new password, and confirm password
7. Click "Change Password"
8. **Read the offline warning dialog carefully**
9. Click "Yes" to proceed with offline password change
10. Verify success message appears

**Expected Results**:
- ✅ Warning dialog appears with message about needing OLD password to sync later
- ✅ Success message: "Password changed offline. You will need your OLD password to sync when online."
- ✅ User remains logged in

**Verify in Database**:
```sql
SELECT Id, Email, PendingPasswordChange, OldPasswordHash, PasswordChangedDate, NeedsSync
FROM User
WHERE Email = 'your-test-email@example.com';
```
- `PendingPasswordChange` should be `1` (true)
- `OldPasswordHash` should contain a hash (not NULL)
- `PasswordChangedDate` should be updated
- `NeedsSync` should be `1` (true)

**DO NOT LOG OUT YET** - Continue to Scenario 3

---

### Scenario 3: Password Sync Dialog (Happy Path)

**Goal**: Verify password sync dialog appears and syncs correctly

**Prerequisites**: Complete Scenario 2 first (user logged in with pending password change)

**Steps**:
1. With the application still running (user logged in, offline password change completed)
2. Log out
3. **Reconnect to internet**
4. Log in with the **NEW password** (the one you changed to while offline)
5. **Password Sync Dialog should appear automatically**
6. In the dialog:
   - Enter **OLD password** (the password BEFORE the offline change)
   - Enter **NEW password** (the password you changed to while offline)
   - Enter **NEW password** again in Confirm field
7. Click "Sync Password to Firebase"

**Expected Results**:
- ✅ Dialog appears automatically after login
- ✅ Dialog has yellow warning icon and clear instructions
- ✅ Success message: "Password synced to Firebase successfully!"
- ✅ Dialog closes
- ✅ User is logged in and on Home screen

**Verify in Database**:
```sql
SELECT Id, Email, PendingPasswordChange, OldPasswordHash, PasswordChangedDate, NeedsSync
FROM User
WHERE Email = 'your-test-email@example.com';
```
- `PendingPasswordChange` should now be `0` (false)
- `OldPasswordHash` should now be `NULL`
- `NeedsSync` should be `0` (false)

**Verify in Firebase**:
- Log out from the application
- Try to log in with the **OLD password** → Should FAIL
- Try to log in with the **NEW password** → Should SUCCESS
- This confirms Firebase password was synced correctly

---

### Scenario 4: Password Sync - Wrong Old Password

**Goal**: Verify validation when user enters wrong old password

**Prerequisites**: Have a user with pending password change (repeat Scenario 2 if needed)

**Steps**:
1. Log in online with user that has pending password change
2. Password Sync Dialog appears
3. Enter **WRONG old password**
4. Enter correct new password and confirm
5. Click "Sync Password to Firebase"

**Expected Results**:
- ✅ Error message: "Old password is incorrect. Please enter the password you used BEFORE the change."
- ✅ Dialog remains open
- ✅ User can try again

---

### Scenario 5: Password Sync - Wrong New Password

**Goal**: Verify validation when user enters wrong new password

**Prerequisites**: Have a user with pending password change

**Steps**:
1. Log in online with user that has pending password change
2. Password Sync Dialog appears
3. Enter correct old password
4. Enter **WRONG new password**
5. Click "Sync Password to Firebase"

**Expected Results**:
- ✅ Error message: "New password is incorrect. Please enter the password you changed to while offline."
- ✅ Dialog remains open
- ✅ User can try again

---

### Scenario 6: Password Sync - Mismatched Confirm Password

**Goal**: Verify validation when new password and confirm don't match

**Steps**:
1. Log in online with user that has pending password change
2. In Password Sync Dialog:
   - Enter correct old password
   - Enter correct new password
   - Enter **DIFFERENT password** in Confirm field
3. Click "Sync Password to Firebase"

**Expected Results**:
- ✅ Error message: "New password and confirm password do not match."
- ✅ Dialog remains open
- ✅ User can try again

---

### Scenario 7: Skip Password Sync

**Goal**: Verify user can skip sync and do it later

**Steps**:
1. Log in online with user that has pending password change
2. Password Sync Dialog appears
3. Click "Skip (Sync Later)"

**Expected Results**:
- ✅ Dialog closes
- ✅ User proceeds to Home screen
- ✅ User can use application normally
- ✅ Next time user logs in online, dialog appears again

**Verify in Database**:
```sql
SELECT PendingPasswordChange FROM User WHERE Email = 'your-test-email@example.com';
```
- `PendingPasswordChange` should still be `1` (true) - not cleared

---

### Scenario 8: Offline Password Change - Cancel Warning

**Goal**: Verify user can cancel offline password change

**Steps**:
1. Go offline
2. Navigate to Change Password
3. Enter passwords
4. Click "Change Password"
5. Offline warning dialog appears
6. Click "No" to cancel

**Expected Results**:
- ✅ Password change is NOT performed
- ✅ User returns to Change Password screen
- ✅ Form is still filled with entered values
- ✅ No database changes

---

### Scenario 9: Automatic Sync on App Startup

**Goal**: Verify automatic sync works on app startup

**Prerequisites**: Create a new user while offline (or have existing user with NeedsSync=1)

**Steps**:
1. Create a test user while offline (if needed)
2. Close the application
3. Reconnect to internet
4. Start the application
5. Check application logs

**Expected Results**:
- ✅ Log message: "Starting automatic sync on app startup..."
- ✅ Log message: "Sync completed: X users synced successfully..."
- ✅ User appears in Firebase Authentication console
- ✅ Check SQLite: `NeedsSync` should be `0` for synced users

**Note**: Password changes are NOT synced automatically (they require user interaction via dialog)

---

### Scenario 10: Automatic Sync on Connectivity Restore

**Goal**: Verify automatic sync works when connection is restored

**Prerequisites**: Have a user with NeedsSync=1 (created offline)

**Steps**:
1. Start application while offline
2. Log in (or create a new user if testing user creation sync)
3. With app running, **reconnect to internet**
4. Wait a few seconds
5. Check application logs

**Expected Results**:
- ✅ Log message: "Network connectivity restored. Starting automatic sync..."
- ✅ Log message: "Sync completed: X users synced successfully..."
- ✅ User synced to Firebase

---

### Scenario 11: Change Password - Validation Errors

**Goal**: Verify input validation on Change Password screen

**Test Cases**:

**A. Empty Current Password**
- Leave current password blank
- Enter new password and confirm
- Click "Change Password"
- ✅ Error: "Please enter your current password"

**B. Empty New Password**
- Enter current password
- Leave new password blank
- Click "Change Password"
- ✅ Error: "Please enter a new password"

**C. Mismatched Confirm Password**
- Enter current password
- Enter new password
- Enter different confirm password
- Click "Change Password"
- ✅ Error: "New password and confirm password do not match"

**D. Wrong Current Password**
- Enter WRONG current password
- Enter new password and confirm
- Click "Change Password"
- ✅ Error: "Current password is incorrect"

---

## Edge Cases & Error Handling

### Edge Case 1: Network Drops During Password Change
**Steps**:
1. Start password change while online
2. Disconnect network during the operation
3. Observe behavior

**Expected**: Should show error message, password not changed

### Edge Case 2: Multiple Offline Password Changes
**Steps**:
1. Change password offline (Password A → Password B)
2. Change password offline again (Password B → Password C)
3. Go online and log in

**Expected**:
- Dialog should ask for the OLDEST password (A) and NEWEST password (C)
- This might fail if OldPasswordHash gets overwritten
- **This is a known limitation** - test and document behavior

### Edge Case 3: Firebase Connection Timeout
**Steps**:
1. Use very slow/unstable internet connection
2. Attempt password sync

**Expected**: Should show error message with timeout, allow retry

---

## Admin-Only Features Testing

### Manual "Sync Now" Button
**Note**: These features (#3 and #4 from requirements) were planned for admin users. Check if they're implemented.

**Steps**:
1. Log in as Admin user
2. Look for "Sync Now" button or sync status display
3. Click to trigger manual sync

**Expected**: Manual sync initiates, shows results

---

## Troubleshooting

### Common Issues

**Issue**: "No authenticated user" error during sync
- **Cause**: Trying to update existing Firebase user without valid session
- **Check**: SyncService should skip updates for existing users during automatic sync
- **Log**: Look for "Existing user - skip during automatic sync" in logs

**Issue**: Password sync dialog doesn't appear
- **Check**:
  - User has `PendingPasswordChange = 1` in database
  - User is logging in while online (`IsOnline = true`)
  - Check LoginViewModel logs for "Pending password change detected"

**Issue**: Offline warning dialog doesn't appear
- **Check**:
  - Connection indicator shows 🔴 (offline)
  - NetworkConnectivityService.IsOnline = false
  - Check ChangePasswordViewModel logs

**Issue**: Password validation fails in sync dialog
- **Check**:
  - Verify you're entering the exact passwords used
  - Check password hashes in database match
  - Look for "Old password is incorrect" or "New password is incorrect" logs

---

## Database Inspection Queries

Useful SQL queries for testing:

```sql
-- View all users with sync status
SELECT Id, Email, FirebaseUid, PendingPasswordChange, NeedsSync, PasswordChangedDate
FROM User;

-- View users needing sync
SELECT Id, Email, NeedsSync, PendingPasswordChange
FROM User
WHERE NeedsSync = 1;

-- View users with pending password changes
SELECT Id, Email, PendingPasswordChange, OldPasswordHash, PasswordChangedDate
FROM User
WHERE PendingPasswordChange = 1;

-- Reset password change flags (for re-testing)
UPDATE User
SET PendingPasswordChange = 0, OldPasswordHash = NULL, NeedsSync = 0
WHERE Email = 'your-test-email@example.com';

-- Manually set pending password change (for testing sync dialog)
UPDATE User
SET PendingPasswordChange = 1, OldPasswordHash = 'some_hash', NeedsSync = 1
WHERE Email = 'your-test-email@example.com';
```

---

## Test Checklist

Use this checklist to track your testing progress:

- [ ] Scenario 1: Online password change
- [ ] Scenario 2: Offline password change
- [ ] Scenario 3: Password sync dialog (happy path)
- [ ] Scenario 4: Wrong old password validation
- [ ] Scenario 5: Wrong new password validation
- [ ] Scenario 6: Mismatched confirm password
- [ ] Scenario 7: Skip password sync
- [ ] Scenario 8: Cancel offline warning
- [ ] Scenario 9: Automatic sync on app startup
- [ ] Scenario 10: Automatic sync on connectivity restore
- [ ] Scenario 11: All validation errors
- [ ] Edge Case 1: Network drops during change
- [ ] Edge Case 2: Multiple offline password changes
- [ ] Database verification for all scenarios
- [ ] Firebase verification for all scenarios

---

## Reporting Issues

If you find bugs during testing, please note:

1. **Scenario number** where issue occurred
2. **Steps to reproduce**
3. **Expected behavior**
4. **Actual behavior**
5. **Log output** (if available)
6. **Database state** (SQL query results)
7. **Network state** (online/offline)

I'm ready to fix any issues you discover!

---

## Success Criteria

The feature is working correctly if:

✅ Online password changes update both Firebase and SQLite immediately
✅ Offline password changes are stored locally with proper flags
✅ Password sync dialog appears automatically on next online login
✅ Sync dialog validates both old and new passwords correctly
✅ Successful sync clears all pending flags
✅ Users can skip sync and retry later
✅ Automatic sync works on app startup and connectivity restore
✅ Automatic sync SKIPS password changes (requires user interaction)
✅ All validation errors provide clear, helpful messages
✅ No crashes, no data loss, no security vulnerabilities

Good luck with testing! Let me know what you find.
