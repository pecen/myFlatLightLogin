using Firebase.Database;
using Firebase.Database.Query;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace myFlatLightLogin.DalFirebase
{
    /// <summary>
    /// Firebase implementation of IRoleDal using Firebase Realtime Database.
    /// Stores roles in the "roles" collection.
    /// </summary>
    public class RoleDal : IRoleDal
    {
        private FirebaseClient _dbClient;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _isInitialized = false;
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

            // Note: Roles are initialized lazily on first access to avoid blocking the constructor
            // You can also call InitializeAsync() explicitly from async context
        }

        /// <summary>
        /// Initializes Firebase roles. Call this method explicitly in async context if needed.
        /// </summary>
        public async Task InitializeAsync()
        {
            await EnsureInitializedAsync();
        }

        /// <summary>
        /// Ensures roles are initialized (lazy initialization with thread safety).
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (!_isInitialized)
                {
                    await InitializeRolesAsync();
                    _isInitialized = true;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Initializes the roles collection and seeds default roles if needed.
        /// </summary>
        private async Task InitializeRolesAsync()
        {
            try
            {
                // Check if roles exist
                var existingRoles = await _dbClient
                    .Child("roles")
                    .OnceAsync<FirebaseRole>();

                // If no roles exist, seed default roles
                if (existingRoles == null || !existingRoles.Any())
                {
                    await SeedDefaultRolesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log or handle initialization error
                Console.WriteLine($"Role initialization error: {ex.Message}");
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

                // Store roles using ID as the key
                await _dbClient
                    .Child("roles")
                    .Child("1")
                    .PutAsync(userRole);

                await _dbClient
                    .Child("roles")
                    .Child("2")
                    .PutAsync(adminRole);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to seed default roles: {ex.Message}", ex);
            }
        }

        #region IRoleDal Implementation

        /// <summary>
        /// Gets a role by its ID.
        /// </summary>
        public RoleDto GetRoleById(int id)
        {
            return GetRoleByIdAsync(id).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets a role by its name.
        /// </summary>
        public RoleDto GetRoleByName(string name)
        {
            return GetRoleByNameAsync(name).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets all roles.
        /// </summary>
        public List<RoleDto> GetAllRoles()
        {
            return GetAllRolesAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Adds a new role.
        /// </summary>
        public bool InsertRole(RoleDto role)
        {
            return InsertRoleAsync(role).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        public bool UpdateRole(RoleDto role)
        {
            return UpdateRoleAsync(role).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes a role by ID.
        /// </summary>
        public bool DeleteRole(int id)
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

                var roles = await _dbClient
                    .Child("roles")
                    .OnceAsync<FirebaseRole>();

                var role = roles.FirstOrDefault(r => r.Object.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

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

                var roles = await _dbClient
                    .Child("roles")
                    .OnceAsync<FirebaseRole>();

                return roles.Select(r => new RoleDto
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
