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

            // Additional check: Can we actually reach Firebase servers?
            // This is critical for VM scenarios where network adapters appear "up" but have no real connectivity
            bool canReachFirebase = false;
            if (isOnline)
            {
                _logger.Information("Network appears online, testing Firebase reachability...");
                canReachFirebase = await _connectivityService.CanReachFirebaseAsync();
                _logger.Information("Firebase reachability test result: {CanReach}", canReachFirebase);
            }

            UserDto user = null;

            // Try Firebase first if we can actually reach it
            if (isOnline && canReachFirebase)
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
#if DEBUG
                    // DEBUG MODE: Log full exception details for development/troubleshooting
                    // WARNING: This includes passwords in plain text! Only use in development.
                    _logger.Warning(ex, "Firebase sign in failed, trying SQLite fallback");
#else
                    // RELEASE MODE: Do NOT log ex.Message - it contains passwords!
                    // Log generic error only for production security
                    _logger.Warning("Firebase sign in failed (invalid credentials or network error), trying SQLite fallback");
#endif
                }
            }
            else
            {
                if (!isOnline)
                {
                    _logger.Information("Offline mode detected - skipping Firebase");
                }
                else
                {
                    _logger.Information("Firebase unreachable - skipping Firebase, using SQLite only");
                }
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
        /// First registered user automatically becomes Admin.
        /// </summary>
        public async Task<RegistrationResult> RegisterAsync(UserDto user)
        {
            _logger.Information("RegisterAsync called for: {Email}", user.Email);

            // Check if email already exists
            var existingUser = FindByEmail(user.Email);
            if (existingUser != null)
            {
                _logger.Warning("Registration failed - email already exists: {Email}", user.Email);
                return RegistrationResult.Failure($"An account with email '{user.Email}' already exists.");
            }

            // Check if this is the first user - make them Admin
            if (IsFirstUser())
            {
                user.Role = UserRole.Admin;
                _logger.Information("First user registration detected - assigning Admin role to {Email}", user.Email);
            }
            else
            {
                // Default role is User (already set in UserDto, but being explicit)
                user.Role = UserRole.User;
                _logger.Information("Registering user with standard User role: {Email}", user.Email);
            }

            // Get FRESH connectivity status (don't trust cached value)
            bool isOnline = _connectivityService.CheckConnectivity();
            _logger.Information("Fresh connectivity check for registration: {IsOnline}", isOnline);

            // Additional check: Can we actually reach Firebase servers?
            // This is critical for VM scenarios where network adapters appear "up" but have no real connectivity
            bool canReachFirebase = false;
            if (isOnline)
            {
                _logger.Information("Network appears online, testing Firebase reachability...");
                canReachFirebase = await _connectivityService.CanReachFirebaseAsync();
                _logger.Information("Firebase reachability test result: {CanReach}", canReachFirebase);
            }

            bool firebaseAttempted = false;
            string firebaseErrorDetails = null;

            // Try Firebase first if we can actually reach it
            if (isOnline && canReachFirebase)
            {
                _logger.Information("Attempting Firebase registration...");
                firebaseAttempted = true;

                try
                {
                    bool success = await Task.Run(() => _firebaseDal.Insert(user));

                    if (success)
                    {
                        _logger.Information("Firebase registration successful for {Email}", user.Email);
                        // Firebase registration successful - save to SQLite
                        _sqliteDal.Insert(user);
                        _sqliteDal.MarkAsSynced(user.Id); // Already in Firebase
                        return RegistrationResult.FirebaseSuccess();
                    }
                    else
                    {
                        _logger.Warning("Firebase registration returned false for {Email}", user.Email);
                        firebaseErrorDetails = "Firebase registration returned false";
                    }
                }
                catch (Exception ex)
                {
                    // Catch ALL exceptions from Firebase - do NOT let them propagate to VS debugger
                    // This prevents VS from breaking on FirebaseAuthException
                    firebaseErrorDetails = ex.Message;

#if DEBUG
                    // DEBUG MODE: Log full exception details for development/troubleshooting
                    _logger.Warning(ex, "Firebase registration failed with exception, falling back to offline registration");
#else
                    // RELEASE MODE: Do NOT log ex.Message - it contains passwords!
                    _logger.Warning("Firebase registration failed (network error or invalid data), falling back to offline registration");
#endif
                }
            }
            else
            {
                if (!isOnline)
                {
                    _logger.Information("Offline mode detected - registering locally to SQLite only");
                }
                else
                {
                    _logger.Information("Firebase unreachable - registering locally to SQLite only");
                    firebaseAttempted = true;
                    firebaseErrorDetails = "Firebase server unreachable (ping failed)";
                }
            }

            // Firebase failed or offline - register in SQLite only
            _logger.Information("Attempting SQLite registration...");
            bool sqliteSuccess = _sqliteDal.Insert(user);

            if (sqliteSuccess)
            {
                _logger.Information("SQLite registration successful for {Email}", user.Email);
                // Mark for sync when online
                // (NeedsSync is automatically set to true in SQLite Insert)
                return RegistrationResult.SQLiteSuccess(firebaseAttempted, firebaseErrorDetails);
            }

            _logger.Error("Both Firebase and SQLite registration failed for {Email}", user.Email);
            return RegistrationResult.Failure("Registration failed in both Firebase and local database. Please try again.");
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

        #region Helper Methods

        /// <summary>
        /// Finds a user by email address.
        /// Checks SQLite local cache first (faster and works offline).
        /// </summary>
        public UserDto FindByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            // Check SQLite local cache (works offline and is faster)
            return _sqliteDal.FindByEmail(email);
        }

        /// <summary>
        /// Checks if this is the first user to be registered in the system.
        /// Used to automatically assign Admin role to the first user.
        /// </summary>
        private bool IsFirstUser()
        {
            try
            {
                // Check SQLite database for any existing users
                using (var conn = new SQLite.SQLiteConnection(
                    System.IO.Path.Combine(Environment.CurrentDirectory, "security.db3")))
                {
                    conn.CreateTable<myFlatLightLogin.DalSQLite.Model.User>();
                    int userCount = conn.Table<myFlatLightLogin.DalSQLite.Model.User>().Count();

                    _logger.Debug("Current user count in database: {UserCount}", userCount);
                    return userCount == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking if first user");
                // If we can't determine, assume not first user (safer default)
                return false;
            }
        }

        #endregion
    }
}
