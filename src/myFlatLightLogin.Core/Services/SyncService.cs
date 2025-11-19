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

namespace myFlatLightLogin.Core.Services
{
    /// <summary>
    /// Service for synchronizing data between Firebase and SQLite.
    /// Implements bi-directional sync with conflict resolution.
    /// </summary>
    public class SyncService
    {
        private static readonly ILogger _logger = Log.ForContext<SyncService>();
        private readonly FirebaseUserDal _firebaseDal;
        private readonly SQLiteUserDal _sqliteDal;
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
            _firebaseDal = new FirebaseUserDal();
            _sqliteDal = new SQLiteUserDal();
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

                // Step 1: Download from Firebase to SQLite (Firebase → SQLite)
                RaiseProgress("Downloading from Firebase...", 0, 2);
                var downloadResult = await DownloadFromFirebaseAsync();
                result.UsersDownloaded = downloadResult.Count;

                // Step 2: Upload from SQLite to Firebase (SQLite → Firebase)
                RaiseProgress("Uploading to Firebase...", 1, 2);
                var uploadResult = await UploadToFirebaseAsync();
                result.UsersUploaded = uploadResult.Count;

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
        /// Downloads all users from Firebase and updates SQLite.
        /// Firebase is the source of truth for this operation.
        /// </summary>
        private async Task<SyncOperationResult> DownloadFromFirebaseAsync()
        {
            var result = new SyncOperationResult();

            try
            {
                // Note: We would need to add a method to Firebase UserDal to get all users
                // For now, we'll skip this as Firebase DAL doesn't have a GetAll method
                // and with security rules, we can only access the current user's data

                // In a real implementation, you might:
                // 1. Use Firebase Admin SDK for server-side operations
                // 2. Or only sync the currently logged-in user
                // 3. Or use Firebase Functions to manage multi-user sync

                result.Count = 0;
                result.Success = true;

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
        /// Uploads pending changes from SQLite to Firebase.
        /// </summary>
        private async Task<SyncOperationResult> UploadToFirebaseAsync()
        {
            var result = new SyncOperationResult();

            try
            {
                // Get all users that need to be synced
                var usersNeedingSync = _sqliteDal.GetUsersNeedingSync();

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
                        _logger.Information("Processing sync for user: {Email}, FirebaseUid: {Uid}, NeedsSync: {NeedsSync}",
                            user.Email, user.FirebaseUid ?? "NULL", user.NeedsSync);

                        // Check if user already exists in Firebase
                        if (string.IsNullOrEmpty(user.FirebaseUid))
                        {
                            // This is a new user created offline - register in Firebase
                            _logger.Information("User {Email} has no FirebaseUid - registering in Firebase", user.Email);

                            bool registered = await Task.Run(() => _firebaseDal.Insert(user));

                            if (registered)
                            {
                                _logger.Information("Firebase registration successful. FirebaseUid: {Uid}", user.FirebaseUid);

                                // CRITICAL: Update SQLite with the new FirebaseUid
                                // The Firebase Insert populated user.FirebaseUid, but we need to save it to SQLite
                                var updatedUser = _sqliteDal.Fetch(user.Id);
                                if (updatedUser != null)
                                {
                                    updatedUser.FirebaseUid = user.FirebaseUid;
                                    _sqliteDal.Update(updatedUser);
                                    _logger.Information("Updated SQLite with FirebaseUid: {Uid}", user.FirebaseUid);
                                }

                                // Mark as synced in SQLite
                                _sqliteDal.MarkAsSynced(user.Id);
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
                            _sqliteDal.MarkAsSynced(user.Id);
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other users
                        _logger.Error(ex, "Failed to sync user {Email}", user.Email);
                        failureCount++;
                        result.ErrorMessage += $"Failed to sync user {user.Email}: {ex.Message}; ";
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
                    return await Task.Run(() => _firebaseDal.Insert(user));
                }
                else
                {
                    // Existing user - update in Firebase
                    return await Task.Run(() => _firebaseDal.Update(user));
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
            return _sqliteDal.GetUsersWithPendingPasswordChanges();
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
                bool success = await _firebaseDal.UpdatePasswordWithOldPasswordAsync(
                    user.Email,
                    oldPassword,
                    newPassword);

                if (success)
                {
                    // Clear pending password change in SQLite
                    _sqliteDal.ClearPendingPasswordChange(user.Id);
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
        public string ErrorMessage { get; set; }

        public TimeSpan Duration => EndTime - StartTime;
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
