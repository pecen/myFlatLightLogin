using Firebase.Database;
using Firebase.Database.Query;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace myFlatLightLogin.DalFirebase
{
    /// <summary>
    /// Firebase implementation of IRoleDal using Firebase Realtime Database.
    /// Stores roles in the "roles" collection.
    /// </summary>
    public class RoleDal : IRoleDal
    {
        private static readonly ILogger _logger = Log.ForContext<RoleDal>();
        private FirebaseClient _dbClient;
        private readonly Lazy<Task> _initializationTask;
        private readonly string _authToken;

        /// <summary>
        /// Initializes a new instance of RoleDal.
        /// </summary>
        /// <param name="authToken">Optional authentication token for Firebase access.</param>
        public RoleDal(string authToken = null)
        {
            _authToken = authToken;

            // Initialize Firebase client with or without authentication
            if (!string.IsNullOrEmpty(_authToken))
            {
                var options = new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(_authToken)
                };
                _dbClient = new FirebaseClient(FirebaseConfig.DatabaseUrl, options);
            }
            else
            {
                // Unauthenticated client (will only work if Firebase rules allow public access)
                _dbClient = new FirebaseClient(FirebaseConfig.DatabaseUrl);
            }

            // Lazy initialization of roles on first access.
            // The constructor remains non-blocking; the roles are seeded/checked on first awaited call to EnsureInitializedAsync().
            // You can also call InitializeAsync() explicitly for eager initialization at startup.
            _initializationTask = new Lazy<Task>(InitializeRolesAsync, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Initializes Firebase roles. Call this method explicitly in async context if needed.
        /// </summary>
        public async Task InitializeAsync()
        {
            _logger.Information("Initializing Firebase Realtime Database roles...");
            await EnsureInitializedAsync();
            _logger.Information("Firebase Realtime Database roles initialized successfully");
        }

        /// <summary>
        /// Ensures roles are initialized (lazy initialization with thread safety).
        /// The first caller will start InitializeRolesAsync(); subsequent callers await the same task.
        /// </summary>
        private Task EnsureInitializedAsync()
        {
            // Return the Lazy task; the first caller will start InitializeRolesAsync().
            return _initializationTask.Value;
        }

        /// <summary>
        /// Initializes the roles collection and seeds default roles if needed.
        /// </summary>
        private async Task InitializeRolesAsync()
        {
            try
            {
                _logger.Debug("Checking Firebase for existing roles...");

                // Check if roles exist
                var existingRoles = await _dbClient
                    .Child("roles")
                    .OnceAsync<FirebaseRole>();

                // If no roles exist, seed default roles
                if (existingRoles == null || !existingRoles.Any())
                {
                    _logger.Information("No roles found in Firebase, seeding default roles...");
                    await SeedDefaultRolesAsync();
                }
                else
                {
                    _logger.Debug($"Found {existingRoles.Count()} existing roles in Firebase");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Firebase role initialization error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Seeds the default roles (User and Admin) in Firebase.
        /// </summary>
        private async Task SeedDefaultRolesAsync()
        {
            try
            {
                var userRole = new FirebaseRole
                {
                    Id = 1,
                    Name = "User",
                    Description = "Standard user with basic permissions"
                };

                var adminRole = new FirebaseRole
                {
                    Id = 2,
                    Name = "Admin",
                    Description = "Administrator with elevated permissions (e.g., view logs, manage users)"
                };

                _logger.Debug("Seeding 'User' role to Firebase...");
                // Store roles using ID as the key
                await _dbClient
                    .Child("roles")
                    .Child("1")
                    .PutAsync(userRole);

                _logger.Debug("Seeding 'Admin' role to Firebase...");
                await _dbClient
                    .Child("roles")
                    .Child("2")
                    .PutAsync(adminRole);

                _logger.Information("Default roles seeded to Firebase successfully");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to seed default roles to Firebase: {ErrorMessage}", ex.Message);
                throw new Exception($"Failed to seed default roles: {ex.Message}", ex);
            }
        }

        #region IRoleDal Implementation

        /// <summary>
        /// Gets a role by its ID.
        /// </summary>
        public RoleDto Fetch(int id)
        {
            return GetRoleByIdAsync(id).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets a role by its name.
        /// </summary>
        public RoleDto Fetch(string name)
        {
            return GetRoleByNameAsync(name).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets all roles.
        /// </summary>
        public List<RoleDto> Fetch()
        {
            return GetAllRolesAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Adds a new role.
        /// </summary>
        public bool Insert(RoleDto role)
        {
            return InsertRoleAsync(role).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        public bool Update(RoleDto role)
        {
            return UpdateRoleAsync(role).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes a role by ID.
        /// </summary>
        public bool Delete(int id)
        {
            return DeleteRoleAsync(id).GetAwaiter().GetResult();
        }

        #endregion

        #region Async Methods (Recommended for Firebase)

        /// <summary>
        /// Gets a role by its ID asynchronously.
        /// </summary>
        private async Task<RoleDto> GetRoleByIdAsync(int id)
        {
            try
            {
                await EnsureInitializedAsync();

                var role = await _dbClient
                    .Child("roles")
                    .Child(id.ToString())
                    .OnceSingleAsync<FirebaseRole>();

                if (role != null)
                {
                    return new RoleDto
                    {
                        Id = role.Id,
                        Name = role.Name,
                        Description = role.Description
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch role by ID: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a role by its name asynchronously.
        /// </summary>
        private async Task<RoleDto> GetRoleByNameAsync(string name)
        {
            try
            {
                await EnsureInitializedAsync();

                // Use OrderByKey and StartAt to skip the null entry at index 0
                var roles = await _dbClient
                    .Child("roles")
                    .OrderByKey()
                    .StartAt("1") // Start at key "1" to skip null at index 0
                    .OnceAsync<FirebaseRole>();

                // Filter out null entries and find by name
                var role = roles
                    .Where(r => r.Object != null)
                    .FirstOrDefault(r => r.Object.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (role != null)
                {
                    return new RoleDto
                    {
                        Id = role.Object.Id,
                        Name = role.Object.Name,
                        Description = role.Object.Description
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch role by name: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets all roles asynchronously.
        /// </summary>
        private async Task<List<RoleDto>> GetAllRolesAsync()
        {
            try
            {
                await EnsureInitializedAsync();

                // Use OrderByKey and StartAt to skip the null entry at index 0
                // This prevents JSON deserialization errors when Firebase returns array format
                var roles = await _dbClient
                    .Child("roles")
                    .OrderByKey()
                    .StartAt("1") // Start at key "1" to skip null at index 0
                    .OnceAsync<FirebaseRole>();

                // Filter out any remaining null entries and convert to DTOs
                return roles
                    .Where(r => r.Object != null)
                    .Select(r => new RoleDto
                    {
                        Id = r.Object.Id,
                        Name = r.Object.Name,
                        Description = r.Object.Description
                    }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch all roles: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Adds a new role asynchronously.
        /// </summary>
        private async Task<bool> InsertRoleAsync(RoleDto role)
        {
            try
            {
                var firebaseRole = new FirebaseRole
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description
                };

                await _dbClient
                    .Child("roles")
                    .Child(role.Id.ToString())
                    .PutAsync(firebaseRole);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to insert role: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates an existing role asynchronously.
        /// </summary>
        private async Task<bool> UpdateRoleAsync(RoleDto role)
        {
            try
            {
                var firebaseRole = new FirebaseRole
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description
                };

                await _dbClient
                    .Child("roles")
                    .Child(role.Id.ToString())
                    .PutAsync(firebaseRole);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update role: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a role by ID asynchronously.
        /// </summary>
        private async Task<bool> DeleteRoleAsync(int id)
        {
            try
            {
                await _dbClient
                    .Child("roles")
                    .Child(id.ToString())
                    .DeleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete role: {ex.Message}", ex);
            }
        }

        #endregion
    }

    /// <summary>
    /// Firebase role model for Realtime Database.
    /// </summary>
    internal class FirebaseRole
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
