# Testing Guide - Authentication & Offline Features

## Overview
This guide covers testing user registration, authentication, and the complete offline password change feature, including automatic sync, password change UI, and password sync dialog.

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

Look for log entries with keywords: `Password`, `Sync`, `Firebase`, `Offline`, `Registration`

---

## User Registration Testing

### Test 1: Normal Registration (Happy Path)

**Goal**: Verify user registration works correctly when online and user doesn't exist

**Steps**:
1. Ensure internet connection is active
2. Navigate to Register User screen
3. Verify connection indicator shows 🟢 (online)
4. Enter user details:
   - Name: TestUser
   - Lastname: Test
   - Email: testuser@example.com (ensure this email doesn't exist in Firebase)
   - Password: test123456
   - Confirm Password: test123456
5. Click "Register"

**Expected Results**:
- ✅ Success message: "Registration Successful"
- ✅ Message shows user was created in Firebase (not offline)
- ✅ User is created in Firebase Authentication
- ✅ User profile is created in Firebase Realtime Database
- ✅ User is created in SQLite local database

**Verify in Firebase Console**:
- **Authentication → Users**: User should appear with the email
- **Realtime Database → users → {uid}**: User profile should exist with Name, Lastname, Email, Role

**Verify in SQLite Database**:
```sql
SELECT Id, Name, Email, FirebaseUid, NeedsSync, RegistrationDate
FROM User
WHERE Email = 'testuser@example.com';
```
- ✅ `FirebaseUid` should be populated (NOT NULL)
- ✅ `NeedsSync` should be `0` (false) - already synced to Firebase
- ✅ `RegistrationDate` should be set

---

### Test 2: EMAIL_EXISTS Error (Firebase Auth Cleanup Required)

**Goal**: Verify proper error handling when user exists in Firebase Authentication but not in Realtime Database

**Background**: This scenario occurs when someone manually deletes a user from Realtime Database but forgets to delete from Authentication.

**Setup**:
1. Create a test user (e.g., existinguser@example.com) through normal registration
2. In Firebase Console:
   - Go to **Realtime Database → users**
   - Delete the user's profile node
   - **DO NOT** delete from Authentication (leave it there)

**Steps**:
1. Ensure internet connection is active
2. Navigate to Register User screen
3. Try to register with the same email:
   - Email: existinguser@example.com
   - Password: test123456
   - Confirm Password: test123456
4. Click "Register"

**Expected Results**:
- ❌ Registration fails with clear error message
- ✅ Error message: "An account with this email already exists in Firebase. If you previously deleted this account, please also delete it from Firebase Authentication (not just Realtime Database)."
- ✅ **NO local SQLite account is created** (this is critical!)
- ✅ User remains on Registration screen

**Verify in SQLite Database**:
```sql
SELECT COUNT(*) FROM User WHERE Email = 'existinguser@example.com';
```
- ✅ Should return `0` - no orphaned local account created

**How to Fix** (for testing cleanup):
1. Go to Firebase Console → Authentication → Users
2. Find the user with the email
3. Delete the user from Authentication
4. Now you can register with that email again

---

### Test 3: Offline Registration (Fallback to SQLite)

**Goal**: Verify registration falls back to local SQLite when internet is unavailable

**Steps**:
1. **Disconnect from internet** (airplane mode or disable network)
2. Navigate to Register User screen
3. Verify connection indicator shows 🔴 (offline)
4. Enter user details:
   - Name: OfflineUser
   - Lastname: Test
   - Email: offlineuser@example.com
   - Password: test123456
   - Confirm Password: test123456
5. Click "Register"

**Expected Results**:
- ✅ Success message: "Registration Successful (Offline)"
- ✅ Message explains: "Your account was created locally and will sync to Firebase when you're back online."
- ✅ User can navigate to Login screen

**Verify in SQLite Database**:
```sql
SELECT Id, Name, Email, FirebaseUid, NeedsSync, RegistrationDate
FROM User
WHERE Email = 'offlineuser@example.com';
```
- ✅ `FirebaseUid` should be `NULL` (not yet in Firebase)
- ✅ `NeedsSync` should be `1` (true) - needs sync when online
- ✅ `RegistrationDate` should be set

**Verify Automatic Sync** (continue this test):
1. **Reconnect to internet**
2. Wait a few seconds (automatic sync should trigger)
3. Check application logs for "Sync completed" message
4. Re-query SQLite database:
```sql
SELECT FirebaseUid, NeedsSync FROM User WHERE Email = 'offlineuser@example.com';
```
- ✅ `FirebaseUid` should now be populated
- ✅ `NeedsSync` should now be `0` (false)

**Verify in Firebase**:
- **Authentication → Users**: offlineuser@example.com should now appear
- **Realtime Database → users**: Profile should exist

---

### Test 4: Other Validation Errors

**Goal**: Verify proper handling of various Firebase authentication errors

**Test Cases**:

**A. Weak Password**
- Enter password with less than 6 characters: "test"
- ✅ Error: "Password is too weak. Please use at least 6 characters."
- ✅ No SQLite account created

**B. Invalid Email Format**
- Enter invalid email: "notanemail"
- ✅ Error: "The email address is invalid."
- ✅ No SQLite account created

**C. Mismatched Confirm Password**
- Enter different passwords in Password and Confirm Password fields
- ✅ Error: "Passwords do not match. Please try again."
- ✅ Form validation catches this before attempting Firebase/SQLite

---

## Password Change Testing

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

**User Registration:**
- [ ] Test 1: Normal registration (happy path)
- [ ] Test 2: EMAIL_EXISTS error handling
- [ ] Test 3: Offline registration with automatic sync
- [ ] Test 4: Validation errors (weak password, invalid email)
- [ ] Verify FirebaseUid populated correctly
- [ ] Verify NeedsSync flag accuracy

**Password Change:**
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

**Verification:**
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

The features are working correctly if:

**User Registration:**
✅ Online registration creates users in both Firebase and SQLite
✅ FirebaseUid is correctly populated in SQLite after online registration
✅ NeedsSync = 0 (false) after successful online registration
✅ EMAIL_EXISTS error provides clear message and prevents SQLite account creation
✅ Other Firebase auth errors (weak password, invalid email) are handled properly
✅ Offline registration creates local SQLite account with NeedsSync = 1
✅ Automatic sync syncs offline-created users to Firebase when online
✅ RegistrationDate is set correctly for all users

**Password Change:**
✅ Online password changes update both Firebase and SQLite immediately
✅ Offline password changes are stored locally with proper flags
✅ Password sync dialog appears automatically on next online login
✅ Sync dialog validates both old and new passwords correctly
✅ Successful sync clears all pending flags
✅ Users can skip sync and retry later
✅ Automatic sync works on app startup and connectivity restore
✅ Automatic sync SKIPS password changes (requires user interaction)

**General:**
✅ All validation errors provide clear, helpful messages
✅ No orphaned accounts created due to error conditions
✅ No crashes, no data loss, no security vulnerabilities

Good luck with testing! Let me know what you find.
