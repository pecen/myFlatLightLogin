using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using Serilog;
using System;
using System.Threading.Tasks;
using FirebaseUserDal = myFlatLightLogin.DalFirebase.UserDal;
using SQLiteUserDal = myFlatLightLogin.DalSQLite.UserDal;

namespace myFlatLightLogin.Core.Services
{
    /// <summary>
    /// Hybrid DAL that intelligently routes between Firebase and SQLite based on connectivity.
    /// Provides seamless offline/online operation with automatic fallback.
    /// </summary>
    public class HybridUserDal : IUserDal
    {
        private static readonly ILogger _logger = Log.ForContext<HybridUserDal>();
        private readonly FirebaseUserDal _firebaseDal;
        private readonly SQLiteUserDal _sqliteDal;
        private readonly NetworkConnectivityService _connectivityService;
        private readonly SyncService _syncService;

        public HybridUserDal(NetworkConnectivityService connectivityService, SyncService syncService)
        {
            _firebaseDal = new FirebaseUserDal();
            _sqliteDal = new SQLiteUserDal();
            _connectivityService = connectivityService;
            _syncService = syncService;
        }

        /// <summary>
        /// Gets whether the system is currently online.
        /// </summary>
        public bool IsOnline => _connectivityService.IsOnline;

        #region IUserDal Implementation

        public UserDto Fetch(int id)
        {
            // Always fetch from SQLite (local cache)
            return _sqliteDal.Fetch(id);
        }

        public bool Insert(UserDto user)
        {
            // Strategy: Always insert to SQLite first, then sync to Firebase if online
            bool sqliteSuccess = _sqliteDal.Insert(user);

            if (!sqliteSuccess)
                return false;

            // If online, try to sync to Firebase immediately
            if (_connectivityService.IsOnline)
            {
                try
                {
                    bool firebaseSuccess = _firebaseDal.Insert(user);

                    if (firebaseSuccess)
                    {
                        // Mark as synced in SQLite
                        _sqliteDal.MarkAsSynced(user.Id);
                    }
                    // If Firebase fails, that's okay - it will sync later
                }
                catch
                {
                    // Firebase failed, but SQLite succeeded - will sync later
                }
            }

            return true;
        }

        public bool Update(UserDto user)
        {
            // Strategy: Always update SQLite first, then sync to Firebase if online
            bool sqliteSuccess = _sqliteDal.Update(user);

            if (!sqliteSuccess)
                return false;

            // If online, try to sync to Firebase immediately
            if (_connectivityService.IsOnline)
            {
                try
                {
                    bool firebaseSuccess = _firebaseDal.Update(user);

                    if (firebaseSuccess)
                    {
                        // Mark as synced in SQLite
                        _sqliteDal.MarkAsSynced(user.Id);
                    }
                    // If Firebase fails, that's okay - it will sync later
                }
                catch
                {
                    // Firebase failed, but SQLite succeeded - will sync later
                }
            }

            return true;
        }

        public bool Delete(int id)
        {
            // Strategy: Delete from SQLite first, then Firebase if online
            bool sqliteSuccess = _sqliteDal.Delete(id);

            if (!sqliteSuccess)
                return false;

            // If online, try to delete from Firebase
            if (_connectivityService.IsOnline)
            {
                try
                {
                    _firebaseDal.Delete(id);
                    // If Firebase delete fails, that's okay - orphaned record in Firebase
                }
                catch
                {
                    // Firebase failed, but SQLite succeeded
                }
            }

            return true;
        }

        #endregion

        #region Authentication Methods

        /// <summary>
        /// Signs in a user with email and password.
        /// Tries Firebase first, falls back to SQLite if offline.
        /// </summary>
        public async Task<UserDto> SignInAsync(string email, string password)
        {
            _logger.Information("SignInAsync called for: {Email}", email);
            _logger.Debug("Cached IsOnline property: {IsOnline}", _connectivityService.IsOnline);

            // Get FRESH connectivity status
            bool isOnline = _connectivityService.CheckConnectivity();
            _logger.Information("Fresh connectivity check: {IsOnline}", isOnline);

            UserDto user = null;

            // Try Firebase first if online
            if (isOnline)
            {
                _logger.Information("Attempting Firebase sign in...");
                try
                {
                    user = await _firebaseDal.SignInAsync(email, password);

                    if (user != null)
                    {
                        _logger.Information("Firebase sign in successful for {Email}", email);

                        // Success! Save/update in SQLite for offline use
                        var existingUser = _sqliteDal.FindByEmail(email);

                        if (existingUser == null)
                        {
                            _logger.Information("New user - caching to SQLite");
                            // New user - add to SQLite
                            user.Password = password; // Store for offline auth
                            _sqliteDal.Insert(user);
                            _logger.Information("User cached successfully");
                        }
                        else
                        {
                            _logger.Information("Existing user (ID: {UserId}) - updating SQLite cache", existingUser.Id);
                            // Existing user - update SQLite
                            existingUser.Name = user.Name;
                            existingUser.Lastname = user.Lastname;
                            existingUser.FirebaseUid = user.FirebaseUid;
                            existingUser.Password = password; // Update password
                            _sqliteDal.Update(existingUser);
                            _logger.Information("Cache updated successfully");
                        }

                        return user;
                    }
                    else
                    {
                        _logger.Warning("Firebase returned null - authentication failed");
                    }
                }
                catch (Exception ex)
                {
                    // Firebase failed - fall through to SQLite
                    _logger.Warning(ex, "Firebase sign in failed, trying SQLite fallback");
                }
            }
            else
            {
                _logger.Information("Offline mode detected - skipping Firebase");
            }

            // Firebase failed or offline - try SQLite
            _logger.Information("Attempting SQLite local sign in...");
            user = _sqliteDal.SignInLocally(email, password);

            if (user != null)
            {
                _logger.Information("SQLite sign in successful for {Email}", email);
                // Offline authentication successful
                return user;
            }
            else
            {
                _logger.Warning("SQLite sign in failed - user not found or password mismatch");
            }

            // Both failed
            _logger.Warning("Both Firebase and SQLite authentication failed for {Email}", email);
            return null;
        }

        /// <summary>
        /// Registers a new user.
        /// Tries Firebase first, falls back to SQLite if offline.
        /// </summary>
        public async Task<bool> RegisterAsync(UserDto user)
        {
            bool success = false;

            // Try Firebase first if online
            if (_connectivityService.IsOnline)
            {
                try
                {
                    success = await Task.Run(() => _firebaseDal.Insert(user));

                    if (success)
                    {
                        // Firebase registration successful - save to SQLite
                        _sqliteDal.Insert(user);
                        _sqliteDal.MarkAsSynced(user.Id); // Already in Firebase
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Firebase failed - fall through to offline registration
                    _logger.Warning(ex, "Firebase registration failed, registering offline");
                }
            }

            // Firebase failed or offline - register in SQLite only
            success = _sqliteDal.Insert(user);

            if (success)
            {
                // Mark for sync when online
                // (NeedsSync is automatically set to true in SQLite Insert)
                return true;
            }

            return false;
        }

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        public void SignOut()
        {
            if (_connectivityService.IsOnline)
            {
                try
                {
                    _firebaseDal.SignOut();
                }
                catch
                {
                    // Ignore Firebase sign out errors
                }
            }
        }

        #endregion

        #region Sync Methods

        /// <summary>
        /// Triggers a full sync between SQLite and Firebase.
        /// </summary>
        public async Task<SyncResult> SyncAsync()
        {
            return await _syncService.SyncAsync();
        }

        /// <summary>
        /// Gets the number of users pending sync to Firebase.
        /// </summary>
        public int GetPendingSyncCount()
        {
            var pending = _sqliteDal.GetUsersNeedingSync();
            return pending?.Count ?? 0;
        }

        #endregion
    }
}
