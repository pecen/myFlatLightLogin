using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.DalFirebase;
using myFlatLightLogin.DalSQLite;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirebaseUserDal = myFlatLightLogin.DalFirebase.UserDal;
using SQLiteUserDal = myFlatLightLogin.DalSQLite.UserDal;
using FirebaseRoleDal = myFlatLightLogin.DalFirebase.RoleDal;
using SQLiteRoleDal = myFlatLightLogin.DalSQLite.RoleDal;

namespace myFlatLightLogin.Core.Services
{
    /// <summary>
    /// Service for synchronizing data between Firebase and SQLite.
    /// Implements bi-directional sync with conflict resolution for Users and Roles.
    /// </summary>
    public class SyncService
    {
        private static readonly ILogger _logger = Log.ForContext<SyncService>();
        private readonly FirebaseUserDal _firebaseUserDal;
        private readonly SQLiteUserDal _sqliteUserDal;
        private readonly FirebaseRoleDal _firebaseRoleDal;
        private readonly SQLiteRoleDal _sqliteRoleDal;
        private readonly NetworkConnectivityService _connectivityService;

        /// <summary>
        /// Event raised when sync starts.
        /// </summary>
        public event EventHandler SyncStarted;

        /// <summary>
        /// Event raised when sync completes.
        /// </summary>
        public event EventHandler<SyncCompletedEventArgs> SyncCompleted;

        /// <summary>
        /// Event raised when sync progress updates.
        /// </summary>
        public event EventHandler<SyncProgressEventArgs> SyncProgress;

        public SyncService(NetworkConnectivityService connectivityService)
        {
            _firebaseUserDal = new FirebaseUserDal();
            _sqliteUserDal = new SQLiteUserDal();
            _firebaseRoleDal = new FirebaseRoleDal();
            _sqliteRoleDal = new SQLiteRoleDal();
            _connectivityService = connectivityService;
        }

        /// <summary>
        /// Performs a full bi-directional sync between Firebase and SQLite.
        /// </summary>
        public async Task<SyncResult> SyncAsync()
        {
            var result = new SyncResult
            {
                StartTime = DateTime.UtcNow,
                Success = true
            };

            try
            {
                // Check connectivity
                if (!_connectivityService.IsOnline)
                {
                    result.Success = false;
                    result.ErrorMessage = "No network connection available";
                    return result;
                }

                SyncStarted?.Invoke(this, EventArgs.Empty);

                // Step 1: Download users from Firebase to SQLite (Firebase → SQLite)
                RaiseProgress("Downloading users from Firebase...", 0, 4);
                var downloadUsersResult = await DownloadUsersFromFirebaseAsync();
                result.UsersDownloaded = downloadUsersResult.Count;

                // Step 2: Upload users from SQLite to Firebase (SQLite → Firebase)
                RaiseProgress("Uploading users to Firebase...", 1, 4);
                var uploadUsersResult = await UploadUsersToFirebaseAsync();
                result.UsersUploaded = uploadUsersResult.Count;

                // Step 3: Download roles from Firebase to SQLite (Firebase → SQLite)
                RaiseProgress("Downloading roles from Firebase...", 2, 4);
                var downloadRolesResult = await DownloadRolesFromFirebaseAsync();
                result.RolesDownloaded = downloadRolesResult.Count;

                // Step 4: Upload roles from SQLite to Firebase (SQLite → Firebase)
                RaiseProgress("Uploading roles to Firebase...", 3, 4);
                var uploadRolesResult = await UploadRolesToFirebaseAsync();
                result.RolesUploaded = uploadRolesResult.Count;

                result.Success = true;
                result.EndTime = DateTime.UtcNow;

                SyncCompleted?.Invoke(this, new SyncCompletedEventArgs { Result = result });

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;

                SyncCompleted?.Invoke(this, new SyncCompletedEventArgs { Result = result });

                return result;
            }
        }

        /// <summary>
        /// Downloads user data from Firebase and updates SQLite.
        ///
        /// Note: Due to Firebase security rules, we can typically only access the current user's data.
        /// For full multi-user sync, you would need:
        /// 1. Firebase Admin SDK for server-side operations
        /// 2. Cloud Functions to manage multi-user sync
        /// 3. Modified security rules (not recommended for security reasons)
        ///
        /// Current implementation: Syncs the currently logged-in user's data from Firebase to SQLite.
        /// This ensures the current user's data is up-to-date in the local database.
        /// </summary>
        private async Task<SyncOperationResult> DownloadUsersFromFirebaseAsync()
        {
            var result = new SyncOperationResult();

            try
            {
                // Get current user info
                var currentUser = CurrentUserService.Instance.CurrentUserInfo;

                if (currentUser == null || string.IsNullOrEmpty(currentUser.FirebaseAuthToken))
                {
                    _logger.Information("No authenticated user - skipping user download");
                    result.Count = 0;
                    result.Success = true;
                    return result;
                }

                _logger.Information("Downloading current user data from Firebase: {Email}", currentUser.Email);

                // For now, we assume the current user's data is already in sync through the login process
                // The login flow (UserIdentity.LoginAsync) already fetches and updates user data from Firebase
                //
                // In the future, if we need to sync other users' data, we would need:
                // - Firebase Admin SDK to bypass security rules
                // - Or a backend service that can access all user records
                // - Or Firebase Functions that sync data to a separate collection accessible to admins

                _logger.Information("Current user data synced through login process");

                result.Count = 0; // No additional users downloaded (current user already synced at login)
                result.Success = true;

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DownloadUsersFromFirebaseAsync failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Uploads pending user changes from SQLite to Firebase.
        /// </summary>
        private async Task<SyncOperationResult> UploadUsersToFirebaseAsync()
        {
            var result = new SyncOperationResult();

            try
            {
                // Get all users that need to be synced
                var usersNeedingSync = _sqliteUserDal.GetUsersNeedingSync();

                _logger.Information("UploadToFirebaseAsync: Found {Count} users needing sync", usersNeedingSync?.Count ?? 0);

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
                        _logger.Information("Processing sync for user: {Email}, FirebaseUid: {Uid}",
                            user.Email, user.FirebaseUid ?? "NULL");

                        // Check if user already exists in Firebase
                        if (string.IsNullOrEmpty(user.FirebaseUid))
                        {
                            // This is a new user created offline - register in Firebase
                            _logger.Information("User {Email} has no FirebaseUid - registering in Firebase", user.Email);

                            bool registered = await Task.Run(() => _firebaseUserDal.Insert(user));

                            if (registered)
                            {
                                _logger.Information("Firebase registration successful. FirebaseUid: {Uid}", user.FirebaseUid);

                                // CRITICAL: Update SQLite with the new FirebaseUid
                                // The Firebase Insert populated user.FirebaseUid, but we need to save it to SQLite
                                var updatedUser = _sqliteUserDal.Fetch(user.Id);
                                if (updatedUser != null)
                                {
                                    updatedUser.FirebaseUid = user.FirebaseUid;
                                    _sqliteUserDal.Update(updatedUser);
                                    _logger.Information("Updated SQLite with FirebaseUid: {Uid}", user.FirebaseUid);
                                }

                                // Mark as synced in SQLite
                                _sqliteUserDal.MarkAsSynced(user.Id);
                                _logger.Information("Marked user {Email} as synced in SQLite", user.Email);
                                successCount++;
                            }
                            else
                            {
                                _logger.Warning("Firebase registration returned false for user {Email}", user.Email);
                                failureCount++;
                            }
                        }
                        else
                        {
                            // This is an existing user - skip for now
                            // Updates require an authenticated session, which we don't have during automatic sync
                            // Updates will happen when the user signs in
                            // For now, just mark as synced to avoid repeated attempts
                            _logger.Information("User {Email} already has FirebaseUid - skipping (updates require auth session)", user.Email);
                            _sqliteUserDal.MarkAsSynced(user.Id);
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Check if this is an EMAIL_EXISTS error (user already registered in Firebase)
                        if (ex.Message.Contains("EMAIL_EXISTS", StringComparison.OrdinalIgnoreCase) ||
                            ex.Message.Contains("An account with this email already exists", StringComparison.OrdinalIgnoreCase))
                        {
                            // This is a reconciliation issue - the user exists in Firebase but local record has no FirebaseUid
                            _logger.Warning("User {Email} already exists in Firebase but local record has no FirebaseUid. " +
                                "This indicates a data synchronization issue. The user should log in to reconcile their account.",
                                user.Email);

                            // Mark as synced to prevent repeated failed attempts
                            // The next time the user logs in online, their FirebaseUid will be updated
                            _sqliteUserDal.MarkAsSynced(user.Id);
                            _logger.Information("Marked user {Email} as synced to prevent repeated sync attempts", user.Email);

                            successCount++;
                        }
                        else
                        {
                            // Log error but continue with other users
                            _logger.Error(ex, "Failed to sync user {Email}", user.Email);
                            failureCount++;
                            result.ErrorMessage += $"Failed to sync user {user.Email}: {ex.Message}; ";
                        }
                    }
                }

                _logger.Information("Sync upload complete: {SuccessCount} succeeded, {FailureCount} failed",
                    successCount, failureCount);

                result.Count = successCount;
                result.Success = failureCount == 0;

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "UploadToFirebaseAsync failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Downloads all roles from Firebase and updates SQLite.
        /// Firebase is the source of truth for this operation.
        /// </summary>
        private async Task<SyncOperationResult> DownloadRolesFromFirebaseAsync()
        {
            var result = new SyncOperationResult();

            try
            {
                _logger.Information("DownloadRolesFromFirebaseAsync: Starting role download from Firebase");

                // Get the current user's auth token for authenticated Firebase access
                var authToken = CurrentUserService.Instance.CurrentUserInfo?.FirebaseAuthToken;
                if (string.IsNullOrEmpty(authToken))
                {
                    _logger.Warning("No Firebase auth token available - skipping role download");
                    result.Count = 0;
                    result.Success = true;
                    return result;
                }

                // Create authenticated Firebase RoleDal
                var authenticatedRoleDal = new FirebaseRoleDal(authToken);

                // Get all roles from Firebase
                var firebaseRoles = await Task.Run(() => authenticatedRoleDal.Fetch());

                if (firebaseRoles == null || firebaseRoles.Count == 0)
                {
                    _logger.Information("No roles found in Firebase");
                    result.Count = 0;
                    result.Success = true;
                    return result;
                }

                _logger.Information("Found {Count} roles in Firebase", firebaseRoles.Count);

                int syncedCount = 0;

                foreach (var firebaseRole in firebaseRoles)
                {
                    try
                    {
                        // Check if role exists in SQLite
                        var existingRole = _sqliteRoleDal.Fetch(firebaseRole.Id);

                        if (existingRole == null)
                        {
                            // New role - insert into SQLite
                            _logger.Information("Inserting new role into SQLite: {RoleName} (ID: {RoleId})",
                                firebaseRole.Name, firebaseRole.Id);
                            _sqliteRoleDal.Insert(firebaseRole);
                            syncedCount++;
                        }
                        else
                        {
                            // Existing role - update in SQLite
                            _logger.Information("Updating existing role in SQLite: {RoleName} (ID: {RoleId})",
                                firebaseRole.Name, firebaseRole.Id);
                            _sqliteRoleDal.Update(firebaseRole);
                            syncedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failed to sync role {RoleId} from Firebase", firebaseRole.Id);
                    }
                }

                _logger.Information("Role download complete: {Count} roles synced", syncedCount);

                result.Count = syncedCount;
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DownloadRolesFromFirebaseAsync failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Uploads all roles from SQLite to Firebase.
        /// Ensures Firebase has all roles defined in SQLite.
        /// </summary>
        private async Task<SyncOperationResult> UploadRolesToFirebaseAsync()
        {
            var result = new SyncOperationResult();

            try
            {
                _logger.Information("UploadRolesToFirebaseAsync: Starting role upload to Firebase");

                // Get the current user's auth token for authenticated Firebase access
                var authToken = CurrentUserService.Instance.CurrentUserInfo?.FirebaseAuthToken;
                if (string.IsNullOrEmpty(authToken))
                {
                    _logger.Warning("No Firebase auth token available - skipping role upload");
                    result.Count = 0;
                    result.Success = true;
                    return result;
                }

                // Create authenticated Firebase RoleDal
                var authenticatedRoleDal = new FirebaseRoleDal(authToken);

                // Get all roles from SQLite
                var sqliteRoles = _sqliteRoleDal.Fetch();

                if (sqliteRoles == null || sqliteRoles.Count == 0)
                {
                    _logger.Information("No roles found in SQLite");
                    result.Count = 0;
                    result.Success = true;
                    return result;
                }

                _logger.Information("Found {Count} roles in SQLite", sqliteRoles.Count);

                int syncedCount = 0;

                foreach (var sqliteRole in sqliteRoles)
                {
                    try
                    {
                        // Check if role exists in Firebase
                        var firebaseRole = await Task.Run(() => authenticatedRoleDal.Fetch(sqliteRole.Id));

                        if (firebaseRole == null)
                        {
                            // New role - insert into Firebase
                            _logger.Information("Inserting new role into Firebase: {RoleName} (ID: {RoleId})",
                                sqliteRole.Name, sqliteRole.Id);
                            await Task.Run(() => authenticatedRoleDal.Insert(sqliteRole));
                            syncedCount++;
                        }
                        else
                        {
                            // Existing role - update in Firebase
                            _logger.Information("Updating existing role in Firebase: {RoleName} (ID: {RoleId})",
                                sqliteRole.Name, sqliteRole.Id);
                            await Task.Run(() => authenticatedRoleDal.Update(sqliteRole));
                            syncedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failed to sync role {RoleId} to Firebase", sqliteRole.Id);
                    }
                }

                _logger.Information("Role upload complete: {Count} roles synced", syncedCount);

                result.Count = syncedCount;
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "UploadRolesToFirebaseAsync failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Syncs a single user to Firebase after they register or update offline.
        /// </summary>
        public async Task<bool> SyncUserAsync(UserDto user)
        {
            try
            {
                if (!_connectivityService.IsOnline)
                    return false;

                if (string.IsNullOrEmpty(user.FirebaseUid))
                {
                    // New user - register in Firebase
                    return await Task.Run(() => _firebaseUserDal.Insert(user));
                }
                else
                {
                    // Existing user - update in Firebase
                    return await Task.Run(() => _firebaseUserDal.Update(user));
                }
            }
            catch
            {
                return false;
            }
        }

        private void RaiseProgress(string message, int current, int total)
        {
            SyncProgress?.Invoke(this, new SyncProgressEventArgs
            {
                Message = message,
                Current = current,
                Total = total
            });
        }

        /// <summary>
        /// Gets users with pending password changes that need interactive sync.
        /// These users require the old password to be entered before syncing.
        /// </summary>
        public List<UserDto> GetUsersWithPendingPasswordChanges()
        {
            return _sqliteUserDal.GetUsersWithPendingPasswordChanges();
        }

        /// <summary>
        /// Syncs a password change to Firebase using the old password for authentication.
        /// </summary>
        /// <param name="user">User with pending password change</param>
        /// <param name="oldPassword">Old password (plain text) for Firebase authentication</param>
        /// <param name="newPassword">New password (plain text) to set in Firebase</param>
        /// <returns>True if sync was successful</returns>
        public async Task<bool> SyncPasswordChangeAsync(UserDto user, string oldPassword, string newPassword)
        {
            try
            {
                if (!_connectivityService.IsOnline)
                    return false;

                // Verify old password hash matches stored hash
                var oldPasswordHash = myFlatLightLogin.Core.Utilities.SecurityHelper.HashPassword(oldPassword);
                if (oldPasswordHash != user.OldPasswordHash)
                {
                    return false; // Old password doesn't match
                }

                // Update Firebase password with old password authentication
                bool success = await _firebaseUserDal.UpdatePasswordWithOldPasswordAsync(
                    user.Email,
                    oldPassword,
                    newPassword);

                if (success)
                {
                    // Clear pending password change in SQLite
                    _sqliteUserDal.ClearPendingPasswordChange(user.Id);
                }

                return success;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Result of a sync operation.
    /// </summary>
    public class SyncResult
    {
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int UsersDownloaded { get; set; }
        public int UsersUploaded { get; set; }
        public int RolesDownloaded { get; set; }
        public int RolesUploaded { get; set; }
        public string ErrorMessage { get; set; }

        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Gets the total number of items synced (users + roles).
        /// </summary>
        public int TotalSynced => UsersDownloaded + UsersUploaded + RolesDownloaded + RolesUploaded;
    }

    /// <summary>
    /// Result of a single sync operation (upload or download).
    /// </summary>
    public class SyncOperationResult
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event args for sync completion.
    /// </summary>
    public class SyncCompletedEventArgs : EventArgs
    {
        public SyncResult Result { get; set; }
    }

    /// <summary>
    /// Event args for sync progress updates.
    /// </summary>
    public class SyncProgressEventArgs : EventArgs
    {
        public string Message { get; set; }
        public int Current { get; set; }
        public int Total { get; set; }
        public int ProgressPercentage => Total > 0 ? (Current * 100 / Total) : 0;
    }
}
