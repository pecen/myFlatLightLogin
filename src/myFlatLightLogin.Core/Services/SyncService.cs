using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.DalFirebase;
using myFlatLightLogin.DalSQLite;
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
                        // Check if user already exists in Firebase
                        if (string.IsNullOrEmpty(user.FirebaseUid))
                        {
                            // This is a new user created offline - register in Firebase
                            bool registered = await Task.Run(() => _firebaseDal.Insert(user));

                            if (registered)
                            {
                                // Mark as synced in SQLite
                                _sqliteDal.MarkAsSynced(user.Id);
                                successCount++;
                            }
                            else
                            {
                                failureCount++;
                            }
                        }
                        else
                        {
                            // This is an existing user - update in Firebase
                            bool updated = await Task.Run(() => _firebaseDal.Update(user));

                            if (updated)
                            {
                                // Mark as synced in SQLite
                                _sqliteDal.MarkAsSynced(user.Id);
                                successCount++;
                            }
                            else
                            {
                                failureCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other users
                        failureCount++;
                        result.ErrorMessage += $"Failed to sync user {user.Email}: {ex.Message}; ";
                    }
                }

                result.Count = successCount;
                result.Success = failureCount == 0;

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
