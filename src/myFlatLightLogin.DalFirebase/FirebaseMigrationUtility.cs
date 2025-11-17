using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace myFlatLightLogin.DalFirebase
{
    /// <summary>
    /// Utility for migrating existing Firebase data to support new features.
    /// </summary>
    public class FirebaseMigrationUtility
    {
        private readonly FirebaseClient _dbClient;

        public FirebaseMigrationUtility()
        {
            // Initialize Firebase client for admin operations
            _dbClient = new FirebaseClient(FirebaseConfig.DatabaseUrl);
        }

        /// <summary>
        /// Migrates all existing Firebase users to add the Role field if missing.
        /// This is useful for users created before the Role system was implemented.
        /// </summary>
        /// <returns>Number of users updated</returns>
        public async Task<int> MigrateUsersWithRoleFieldAsync()
        {
            try
            {
                Console.WriteLine("Starting user migration to add Role field...");

                // Fetch all users from Firebase
                var users = await _dbClient
                    .Child("users")
                    .OnceAsync<FirebaseUserProfileMigration>();

                if (users == null || !users.Any())
                {
                    Console.WriteLine("No users found in Firebase.");
                    return 0;
                }

                int updatedCount = 0;

                foreach (var userEntry in users)
                {
                    var userId = userEntry.Key; // Firebase UID
                    var profile = userEntry.Object;

                    // Check if Role field needs to be added/updated
                    bool needsUpdate = false;

                    // If the profile doesn't have an UpdatedAt field or Role is default,
                    // it's likely a legacy user
                    if (string.IsNullOrEmpty(profile.UpdatedAt))
                    {
                        // Set default role (User = 0)
                        profile.Role = 0;
                        profile.UpdatedAt = DateTime.UtcNow.ToString("o");
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        // Update the user profile in Firebase
                        await _dbClient
                            .Child("users")
                            .Child(userId)
                            .PutAsync(profile);

                        updatedCount++;
                        Console.WriteLine($"Updated user: {profile.Email} (UID: {userId})");
                    }
                }

                Console.WriteLine($"Migration complete. {updatedCount} users updated.");
                return updatedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration failed: {ex.Message}");
                throw new Exception($"Failed to migrate users: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Displays a report of all users and their roles.
        /// </summary>
        public async Task DisplayUserRolesAsync()
        {
            try
            {
                var users = await _dbClient
                    .Child("users")
                    .OnceAsync<FirebaseUserProfileMigration>();

                if (users == null || !users.Any())
                {
                    Console.WriteLine("No users found in Firebase.");
                    return;
                }

                Console.WriteLine("\n=== Firebase Users Report ===");
                Console.WriteLine($"Total Users: {users.Count}");
                Console.WriteLine("\nUser Details:");
                Console.WriteLine(new string('-', 80));

                foreach (var userEntry in users)
                {
                    var profile = userEntry.Object;
                    string roleText = profile.Role == 1 ? "Admin" : "User";
                    Console.WriteLine($"Email: {profile.Email,-40} Role: {roleText,-10} (ID: {profile.Role})");
                }

                Console.WriteLine(new string('-', 80));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to display user roles: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Firebase user profile model for migration purposes.
    /// </summary>
    internal class FirebaseUserProfileMigration
    {
        public int LocalId { get; set; }
        public string FirebaseUid { get; set; }
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public int Role { get; set; } = 0; // Default to User role
    }
}
