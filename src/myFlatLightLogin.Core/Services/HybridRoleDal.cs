using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirebaseRoleDal = myFlatLightLogin.DalFirebase.RoleDal;
using SQLiteRoleDal = myFlatLightLogin.DalSQLite.RoleDal;

namespace myFlatLightLogin.Core.Services
{
    /// <summary>
    /// Hybrid DAL for roles that intelligently routes between Firebase and SQLite based on connectivity.
    /// Provides seamless offline/online operation with automatic fallback and synchronization.
    /// </summary>
    public class HybridRoleDal : IRoleDal
    {
        private static readonly ILogger _logger = Log.ForContext<HybridRoleDal>();
        private readonly SQLiteRoleDal _sqliteDal;
        private readonly NetworkConnectivityService _connectivityService;

        /// <summary>
        /// Initializes a new instance of HybridRoleDal with the required dependencies.
        /// </summary>
        /// <param name="connectivityService">Service to check network connectivity</param>
        public HybridRoleDal(NetworkConnectivityService connectivityService)
        {
            _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
            _sqliteDal = new SQLiteRoleDal();
        }

        /// <summary>
        /// Gets a FirebaseRoleDal instance with the current user's auth token.
        /// Creates a new instance each time to ensure the latest auth token is used.
        /// </summary>
        private FirebaseRoleDal GetFirebaseDal()
        {
            var authToken = CurrentUserService.Instance.CurrentUserInfo?.FirebaseAuthToken;
            return new FirebaseRoleDal(authToken);
        }

        /// <summary>
        /// Gets whether the system is currently online.
        /// </summary>
        public bool IsOnline => _connectivityService.IsOnline;

        /// <summary>
        /// Initializes the role data stores asynchronously.
        /// Initializes SQLite (local cache) first, then Firebase (remote) if online.
        /// </summary>
        public async Task InitializeAsync()
        {
            _logger.Information("Initializing role providers...");

            // Always initialize SQLite first (local cache)
            await _sqliteDal.InitializeAsync();

            // If online, also initialize Firebase
            if (_connectivityService.IsOnline)
            {
                try
                {
                    _logger.Debug("Online - initializing remote role provider...");
                    var firebaseDal = GetFirebaseDal();
                    await firebaseDal.InitializeAsync();
                }
                catch (Exception ex)
                {
                    // Firebase initialization failed, but SQLite succeeded
                    // Log warning but don't fail - app can work offline
                    _logger.Warning("Remote role provider initialization failed: {ErrorMessage}", ex.Message);
                }
            }
            else
            {
                _logger.Debug("Offline - skipping remote role provider initialization");
            }

            _logger.Information("Role providers initialized successfully");
        }

        #region IRoleDal Implementation

        /// <summary>
        /// Fetches a role by ID from the local SQLite cache.
        /// </summary>
        public RoleDto Fetch(int id)
        {
            // Always fetch from SQLite (local cache)
            return _sqliteDal.Fetch(id);
        }

        /// <summary>
        /// Fetches a role by name from the local SQLite cache.
        /// </summary>
        public RoleDto Fetch(string name)
        {
            // Always fetch from SQLite (local cache)
            return _sqliteDal.Fetch(name);
        }

        /// <summary>
        /// Fetches all roles from the local SQLite cache.
        /// </summary>
        public List<RoleDto> Fetch()
        {
            // Always fetch from SQLite (local cache)
            return _sqliteDal.Fetch();
        }

        /// <summary>
        /// Inserts a new role into both SQLite and Firebase (if online).
        /// Strategy: Write to SQLite first for immediate availability, then sync to Firebase.
        /// </summary>
        public bool Insert(RoleDto role)
        {
            _logger.Information("HybridRoleDal.Insert: Inserting role '{RoleName}' (ID: {RoleId})", role.Name, role.Id);

            // Always insert to SQLite first (local cache)
            bool sqliteSuccess = _sqliteDal.Insert(role);

            if (!sqliteSuccess)
            {
                _logger.Warning("Failed to insert role '{RoleName}' into SQLite", role.Name);
                return false;
            }

            _logger.Information("Role '{RoleName}' inserted into SQLite successfully", role.Name);

            // If online, try to sync to Firebase immediately
            if (_connectivityService.IsOnline)
            {
                try
                {
                    _logger.Information("Online - attempting to insert role '{RoleName}' into Firebase", role.Name);
                    var firebaseDal = GetFirebaseDal();
                    bool firebaseSuccess = firebaseDal.Insert(role);

                    if (firebaseSuccess)
                    {
                        _logger.Information("Role '{RoleName}' inserted into Firebase successfully", role.Name);
                    }
                    else
                    {
                        _logger.Warning("Failed to insert role '{RoleName}' into Firebase - will sync later", role.Name);
                    }
                }
                catch (Exception ex)
                {
                    // Firebase failed, but SQLite succeeded - will sync later
                    _logger.Warning(ex, "Exception inserting role '{RoleName}' into Firebase - will sync later", role.Name);
                }
            }
            else
            {
                _logger.Information("Offline - role '{RoleName}' will be synced to Firebase when connection is restored", role.Name);
            }

            return true;
        }

        /// <summary>
        /// Updates an existing role in both SQLite and Firebase (if online).
        /// Strategy: Write to SQLite first for immediate availability, then sync to Firebase.
        /// </summary>
        public bool Update(RoleDto role)
        {
            _logger.Information("HybridRoleDal.Update: Updating role '{RoleName}' (ID: {RoleId})", role.Name, role.Id);

            // Always update SQLite first (local cache)
            bool sqliteSuccess = _sqliteDal.Update(role);

            if (!sqliteSuccess)
            {
                _logger.Warning("Failed to update role '{RoleName}' in SQLite", role.Name);
                return false;
            }

            _logger.Information("Role '{RoleName}' updated in SQLite successfully", role.Name);

            // If online, try to sync to Firebase immediately
            if (_connectivityService.IsOnline)
            {
                try
                {
                    _logger.Information("Online - attempting to update role '{RoleName}' in Firebase", role.Name);
                    var firebaseDal = GetFirebaseDal();
                    bool firebaseSuccess = firebaseDal.Update(role);

                    if (firebaseSuccess)
                    {
                        _logger.Information("Role '{RoleName}' updated in Firebase successfully", role.Name);
                    }
                    else
                    {
                        _logger.Warning("Failed to update role '{RoleName}' in Firebase - will sync later", role.Name);
                    }
                }
                catch (Exception ex)
                {
                    // Firebase failed, but SQLite succeeded - will sync later
                    _logger.Warning(ex, "Exception updating role '{RoleName}' in Firebase - will sync later", role.Name);
                }
            }
            else
            {
                _logger.Information("Offline - role '{RoleName}' changes will be synced to Firebase when connection is restored", role.Name);
            }

            return true;
        }

        /// <summary>
        /// Deletes a role from both SQLite and Firebase (if online).
        /// Strategy: Delete from SQLite first, then sync deletion to Firebase.
        /// </summary>
        public bool Delete(int id)
        {
            _logger.Information("HybridRoleDal.Delete: Deleting role ID: {RoleId}", id);

            // Get role name for logging before deletion
            var role = _sqliteDal.Fetch(id);
            var roleName = role?.Name ?? "Unknown";

            // Always delete from SQLite first (local cache)
            bool sqliteSuccess = _sqliteDal.Delete(id);

            if (!sqliteSuccess)
            {
                _logger.Warning("Failed to delete role '{RoleName}' (ID: {RoleId}) from SQLite", roleName, id);
                return false;
            }

            _logger.Information("Role '{RoleName}' (ID: {RoleId}) deleted from SQLite successfully", roleName, id);

            // If online, try to sync deletion to Firebase immediately
            if (_connectivityService.IsOnline)
            {
                try
                {
                    _logger.Information("Online - attempting to delete role '{RoleName}' (ID: {RoleId}) from Firebase", roleName, id);
                    var firebaseDal = GetFirebaseDal();
                    bool firebaseSuccess = firebaseDal.Delete(id);

                    if (firebaseSuccess)
                    {
                        _logger.Information("Role '{RoleName}' (ID: {RoleId}) deleted from Firebase successfully", roleName, id);
                    }
                    else
                    {
                        _logger.Warning("Failed to delete role '{RoleName}' (ID: {RoleId}) from Firebase - will sync later", roleName, id);
                    }
                }
                catch (Exception ex)
                {
                    // Firebase failed, but SQLite succeeded - will sync later
                    _logger.Warning(ex, "Exception deleting role '{RoleName}' (ID: {RoleId}) from Firebase - will sync later", roleName, id);
                }
            }
            else
            {
                _logger.Information("Offline - role '{RoleName}' (ID: {RoleId}) deletion will be synced to Firebase when connection is restored", roleName, id);
            }

            return true;
        }

        #endregion
    }
}
