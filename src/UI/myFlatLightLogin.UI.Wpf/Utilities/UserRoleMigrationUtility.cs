using Firebase.Database;
using Firebase.Database.Query;
using myFlatLightLogin.DalFirebase;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace myFlatLightLogin.UI.Wpf.Utilities
{
    /// <summary>
    /// Utility to migrate existing Firebase users from old UserRole enum values to new values.
    /// OLD: User=0, Admin=1
    /// NEW: User=1, Admin=2
    /// </summary>
    public static class UserRoleMigrationUtility
    {
        /// <summary>
        /// Migrates all users in Firebase from old UserRole enum values to new values.
        /// This is a one-time migration needed after changing the enum.
        /// </summary>
        public static async Task<(bool success, string message)> MigrateUserRolesAsync()
        {
            try
            {
                Log.Information("Starting UserRole enum migration...");

                var dbClient = new FirebaseClient(FirebaseConfig.DatabaseUrl);

                // Fetch all users
                var users = await dbClient
                    .Child("users")
                    .OnceAsync<dynamic>();

                if (users == null || !users.Any())
                {
                    return (true, "No users found to migrate.");
                }

                int migratedCount = 0;
                int errorCount = 0;

                foreach (var user in users)
                {
                    try
                    {
                        var userId = user.Key;
                        var userData = user.Object;

                        // Get current role value
                        int currentRole = (int)userData.Role;

                        // Determine new role value
                        int newRole;
                        if (currentRole == 0) // Old User value
                        {
                            newRole = 1; // New User value
                        }
                        else if (currentRole == 1) // Old Admin value
                        {
                            newRole = 2; // New Admin value
                        }
                        else
                        {
                            // Already migrated or custom role
                            continue;
                        }

                        // Update only the Role field
                        await dbClient
                            .Child("users")
                            .Child(userId)
                            .Child("Role")
                            .PutAsync(newRole);

                        Log.Information($"Migrated user {userId}: Role {currentRole} -> {newRole}");
                        migratedCount++;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error migrating user {user.Key}: {ex.Message}");
                        errorCount++;
                    }
                }

                string message = $"Migration completed! Migrated {migratedCount} users. Errors: {errorCount}";
                Log.Information(message);
                return (true, message);
            }
            catch (Exception ex)
            {
                string errorMsg = $"Migration failed: {ex.Message}";
                Log.Error(errorMsg);
                return (false, errorMsg);
            }
        }
    }
}
