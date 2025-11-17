# Firebase Role Migration Guide

This guide explains how to migrate existing Firebase users to support the new Role system.

## Background

If you have users that were registered **before** the Role system was implemented, they may not have the `Role` field in their Firebase user profile. This guide shows you how to add the Role field to existing users.

## Migration Options

You have **three options** to migrate existing users:

### Option 1: Automatic Migration on Sign-In (Recommended)

**No action required!** The system will automatically add the Role field when existing users sign in.

- When a user without a Role field signs in, the system detects this
- The Role field is automatically added and set to `User` (role ID: 0)
- The user's Firebase profile is updated in the background
- This happens seamlessly without any user interaction

**Pros:**
- No manual intervention needed
- Safe and gradual migration
- Users are migrated as they use the app

**Cons:**
- Users who don't sign in won't be migrated immediately

### Option 2: One-Time Migration on App Startup

If you want to migrate **all** existing users at once, you can run a one-time migration on app startup.

**Steps:**

1. Open `src/myFlatLightLogin.UI.Wpf/App.xaml.cs`

2. Locate the commented migration code (around line 87-92):
   ```csharp
   /*
   Log.Information("Running Firebase user migration...");
   var migrationUtility = new FirebaseMigrationUtility();
   int usersUpdated = await migrationUtility.MigrateUsersWithRoleFieldAsync();
   Log.Information($"Migration complete. {usersUpdated} users updated with Role field.");
   */
   ```

3. **Uncomment** the migration code (remove `/*` and `*/`)

4. Run your application **once**

5. Check the logs to confirm migration completed successfully

6. **Re-comment** the code to prevent it from running again

**Pros:**
- All users migrated at once
- Immediate consistency across all user records

**Cons:**
- Requires manual code change
- Runs on every startup if you forget to re-comment it

### Option 3: Manual Migration Script

You can create a simple console app to run the migration script independently.

**Example code:**
```csharp
using myFlatLightLogin.DalFirebase;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Firebase User Migration Tool");
        Console.WriteLine("============================\n");

        try
        {
            var migrationUtility = new FirebaseMigrationUtility();

            // Display current users and their roles
            await migrationUtility.DisplayUserRolesAsync();

            Console.WriteLine("\nDo you want to migrate users? (y/n)");
            var response = Console.ReadLine();

            if (response?.ToLower() == "y")
            {
                int updated = await migrationUtility.MigrateUsersWithRoleFieldAsync();
                Console.WriteLine($"\nMigration complete! {updated} users updated.");

                // Display updated users
                await migrationUtility.DisplayUserRolesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
```

**Pros:**
- Complete control over when migration runs
- Can be run multiple times safely
- Shows detailed migration progress

**Cons:**
- Requires creating a separate console project

## What the Migration Does

The migration script:

1. Fetches all users from Firebase Realtime Database (`/users` node)
2. Checks each user for the `Role` field
3. If a user doesn't have a Role field (or `UpdatedAt` is empty):
   - Sets `Role = 0` (User role)
   - Sets `UpdatedAt` to current timestamp
   - Updates the user profile in Firebase
4. Reports how many users were updated

## Default Role Assignment

- All existing users are assigned the **User** role (ID: 0) by default
- If you need to make certain users **Admins**, you can:
  - Update them manually in Firebase Console
  - Create a custom migration script to assign Admin role based on criteria

## Verification

After migration, you can verify in Firebase Console:

1. Go to Firebase Console → Realtime Database
2. Navigate to `/users`
3. Check that each user now has a `Role` field with value `0` or `1`

## Role Values

- `0` = User (standard user)
- `1` = Admin (administrator)

## Need Help?

If you encounter issues during migration, check:

- Firebase configuration is correct in `appsettings.json` or User Secrets
- You have internet connectivity
- Firebase Realtime Database rules allow read/write access
- Check application logs for detailed error messages
