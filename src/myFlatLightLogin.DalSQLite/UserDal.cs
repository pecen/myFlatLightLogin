using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.DalSQLite.Model;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace myFlatLightLogin.DalSQLite
{
    /// <summary>
    /// SQLite implementation of IUserDal with offline support and sync capabilities.
    /// </summary>
    public class UserDal : IUserDal
    {
        private static readonly ILogger _logger = Log.ForContext<UserDal>();
        private static string dbFile = Path.Combine(Environment.CurrentDirectory, "security.db3");
        private readonly RoleDal _roleDal;

        public UserDal()
        {
            // RoleDal available for querying role details if needed
            _roleDal = new RoleDal();
        }

        public UserDto Fetch(int id)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var user = conn.Table<User>().FirstOrDefault(u => u.Id == id);

                if (user == null)
                    return null;

                return new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Lastname = user.Lastname,
                    Username = user.Username,
                    Email = user.Email,
                    FirebaseUid = user.FirebaseUid,
                    RegistrationDate = user.RegistrationDate,
                    Role = (UserRole)user.RoleId // Direct cast: role IDs match enum values
                };
            }
        }

        public bool Insert(UserDto userDto)
        {
            _logger.Information("Insert called for email: {Email}", userDto.Email);
            _logger.Debug("Password provided: {PasswordProvided}", !string.IsNullOrEmpty(userDto.Password));

            var user = new User
            {
                Name = userDto.Name,
                Lastname = userDto.Lastname,
                Username = userDto.Username ?? userDto.Email,
                Email = userDto.Email,
                Password = HashPassword(userDto.Password),
                FirebaseUid = userDto.FirebaseUid,
                RegistrationDate = DateTime.UtcNow.ToString("o"), // Set once during registration
                LastModified = DateTime.UtcNow.ToString("o"),
                NeedsSync = true, // Mark for sync to Firebase
                RoleId = (int)userDto.Role, // Direct cast: enum values match role IDs

                // Store plain-text password temporarily for users without FirebaseUid
                // This allows sync to create Firebase Auth account later
                PendingPassword = string.IsNullOrEmpty(userDto.FirebaseUid) ? userDto.Password : null
            };

            bool result = DbCore.Insert(user);
            _logger.Information("Insert result: {Result}, User ID: {UserId}, PendingPassword stored: {HasPendingPassword}",
                result, user.Id, !string.IsNullOrEmpty(user.PendingPassword));
            return result;
        }

        public bool Update(UserDto userDto)
        {
            _logger.Information("Update called for ID: {UserId}, Email: {Email}", userDto.Id, userDto.Email);
            _logger.Debug("Password provided: {PasswordProvided}", !string.IsNullOrEmpty(userDto.Password));

            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();

                var user = conn.Table<User>().FirstOrDefault(u => u.Id == userDto.Id);
                if (user == null)
                {
                    _logger.Warning("User with ID {UserId} not found for update", userDto.Id);
                    return false;
                }

                user.Name = userDto.Name;
                user.Lastname = userDto.Lastname;
                user.Username = userDto.Username ?? userDto.Email;
                user.Email = userDto.Email;
                user.FirebaseUid = userDto.FirebaseUid;
                user.LastModified = DateTime.UtcNow.ToString("o");
                user.NeedsSync = true; // Mark for sync to Firebase
                user.RoleId = (int)userDto.Role; // Direct cast: enum values match role IDs

                // Only update password if provided
                if (!string.IsNullOrEmpty(userDto.Password))
                {
                    user.Password = HashPassword(userDto.Password);
                    _logger.Debug("Password updated and hashed");
                }

                bool result = conn.Update(user) > 0;
                _logger.Information("Update result: {Result}", result);
                return result;
            }
        }

        public bool Delete(int id)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var user = conn.Table<User>().FirstOrDefault(u => u.Id == id);

                if (user == null)
                    return false;

                return conn.Delete(user) > 0;
            }
        }

        /// <summary>
        /// Authenticates a user with email and password against the local SQLite database.
        /// </summary>
        public UserDto SignInLocally(string email, string password)
        {
            _logger.Information("SignInLocally called for: {Email}", email);
            _logger.Debug("Database file: {DatabaseFile}", dbFile);

            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();

                var allUsers = conn.Table<User>().ToList();
                _logger.Information("Total users in database: {UserCount}", allUsers.Count);

                var user = conn.Table<User>().FirstOrDefault(u => u.Email == email || u.Username == email);

                if (user == null)
                {
                    _logger.Warning("User not found for email: {Email}", email);
                    return null;
                }

                _logger.Information("User found: ID={UserId}, Email={Email}, HasPassword={HasPassword}",
                    user.Id, user.Email, !string.IsNullOrEmpty(user.Password));

                // Verify password
                if (!VerifyPassword(password, user.Password))
                {
                    _logger.Warning("Password verification failed for email: {Email}", email);
                    return null;
                }

                _logger.Information("Password verified successfully for email: {Email}", email);

                return new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Lastname = user.Lastname,
                    Username = user.Username,
                    Email = user.Email,
                    FirebaseUid = user.FirebaseUid,
                    RegistrationDate = user.RegistrationDate,
                    Role = (UserRole)user.RoleId // Direct cast: role IDs match enum values
                };
            }
        }

        /// <summary>
        /// Gets all users that need to be synced to Firebase.
        /// For users without FirebaseUid, includes the PendingPassword for Firebase Auth account creation.
        /// </summary>
        public List<UserDto> GetUsersNeedingSync()
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var users = conn.Table<User>().Where(u => u.NeedsSync).ToList();

                return users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Lastname = u.Lastname,
                    Username = u.Username,
                    Email = u.Email,
                    FirebaseUid = u.FirebaseUid,
                    Role = (UserRole)u.RoleId, // Direct cast: role IDs match enum values

                    // Include PendingPassword for users without FirebaseUid (needed for Firebase Auth creation)
                    Password = string.IsNullOrEmpty(u.FirebaseUid) ? u.PendingPassword : null
                }).ToList();
            }
        }

        /// <summary>
        /// Marks a user as synced with Firebase and clears the pending password.
        /// </summary>
        public bool MarkAsSynced(int id)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var user = conn.Table<User>().FirstOrDefault(u => u.Id == id);

                if (user == null)
                    return false;

                user.NeedsSync = false;

                // Clear pending password after successful sync (security)
                if (!string.IsNullOrEmpty(user.PendingPassword))
                {
                    _logger.Information("Clearing PendingPassword for user {UserId} after successful sync", id);
                    user.PendingPassword = null;
                }

                return conn.Update(user) > 0;
            }
        }

        /// <summary>
        /// Finds a user by Firebase UID for sync operations.
        /// </summary>
        public UserDto FindByFirebaseUid(string firebaseUid)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var user = conn.Table<User>().FirstOrDefault(u => u.FirebaseUid == firebaseUid);

                if (user == null)
                    return null;

                return new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Lastname = user.Lastname,
                    Username = user.Username,
                    Email = user.Email,
                    FirebaseUid = user.FirebaseUid,
                    RegistrationDate = user.RegistrationDate,
                    Role = (UserRole)user.RoleId // Direct cast: role IDs match enum values
                };
            }
        }

        /// <summary>
        /// Finds a user by email address.
        /// </summary>
        public UserDto FindByEmail(string email)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var user = conn.Table<User>().FirstOrDefault(u => u.Email == email || u.Username == email);

                if (user == null)
                    return null;

                return new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Lastname = user.Lastname,
                    Username = user.Username,
                    Email = user.Email,
                    FirebaseUid = user.FirebaseUid,
                    RegistrationDate = user.RegistrationDate,
                    Role = (UserRole)user.RoleId // Direct cast: role IDs match enum values
                };
            }
        }

        /// <summary>
        /// Updates user from Firebase sync (doesn't mark as needing sync).
        /// </summary>
        public bool UpdateFromSync(UserDto userDto, string firebaseUid, string lastModified)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();

                // Find existing user by FirebaseUid or Email
                var user = conn.Table<User>().FirstOrDefault(u => u.FirebaseUid == firebaseUid);
                if (user == null)
                {
                    user = conn.Table<User>().FirstOrDefault(u => u.Email == userDto.Email);
                }

                if (user == null)
                {
                    // Insert new user from Firebase
                    user = new User
                    {
                        Name = userDto.Name,
                        Lastname = userDto.Lastname,
                        Username = userDto.Username ?? userDto.Email,
                        Email = userDto.Email,
                        Password = string.Empty, // No password for Firebase-synced users
                        FirebaseUid = firebaseUid,
                        LastModified = lastModified,
                        NeedsSync = false,
                        RoleId = (int)userDto.Role // Direct cast: enum values match role IDs
                    };
                    return conn.Insert(user) > 0;
                }
                else
                {
                    // Update existing user
                    user.Name = userDto.Name;
                    user.Lastname = userDto.Lastname;
                    user.Username = userDto.Username ?? userDto.Email;
                    user.Email = userDto.Email;
                    user.FirebaseUid = firebaseUid;
                    user.LastModified = lastModified;
                    user.NeedsSync = false; // Don't sync back
                    user.RoleId = (int)userDto.Role; // Direct cast: enum values match role IDs from Firebase

                    return conn.Update(user) > 0;
                }
            }
        }

        /// <summary>
        /// Gets all users with pending password changes that need interactive sync.
        /// </summary>
        public List<UserDto> GetUsersWithPendingPasswordChanges()
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var users = conn.Table<User>()
                    .Where(u => u.PendingPasswordChange == true)
                    .ToList();

                return users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Lastname = u.Lastname,
                    Username = u.Username,
                    Email = u.Email,
                    FirebaseUid = u.FirebaseUid,
                    PendingPasswordChange = u.PendingPasswordChange,
                    OldPasswordHash = u.OldPasswordHash,
                    PasswordChangedDate = u.PasswordChangedDate,
                    Role = (UserRole)u.RoleId // Direct cast: role IDs match enum values
                }).ToList();
            }
        }

        /// <summary>
        /// Clears pending password change flag after successful sync.
        /// </summary>
        public bool ClearPendingPasswordChange(int userId)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var user = conn.Find<User>(userId);

                if (user != null)
                {
                    user.PendingPasswordChange = false;
                    user.OldPasswordHash = null;
                    user.NeedsSync = false; // Also clear needs sync
                    return conn.Update(user) > 0;
                }

                return false;
            }
        }

        /// <summary>
        /// Changes user password offline, storing old password hash for later sync.
        /// </summary>
        public bool ChangePasswordOffline(int userId, string currentPassword, string newPassword)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var user = conn.Find<User>(userId);

                if (user == null)
                    return false;

                // Verify current password
                if (!VerifyPassword(currentPassword, user.Password))
                    return false;

                // Store old password hash for sync verification
                user.OldPasswordHash = user.Password;

                // Update to new password hash
                user.Password = HashPassword(newPassword);
                user.PendingPasswordChange = true;
                user.PasswordChangedDate = DateTime.UtcNow.ToString("o");
                user.LastModified = DateTime.UtcNow.ToString("o");
                user.NeedsSync = true;

                return conn.Update(user) > 0;
            }
        }

        /// <summary>
        /// Changes user password online (updates password hash only).
        /// </summary>
        public bool ChangePasswordOnline(int userId, string newPassword)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var user = conn.Find<User>(userId);

                if (user == null)
                    return false;

                // Update to new password hash
                user.Password = HashPassword(newPassword);
                user.PasswordChangedDate = DateTime.UtcNow.ToString("o");
                user.LastModified = DateTime.UtcNow.ToString("o");

                // Don't set NeedsSync - password was already changed in Firebase

                return conn.Update(user) > 0;
            }
        }

        /// <summary>
        /// Verifies a user's password against the stored hash.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="password">Plain text password to verify</param>
        /// <returns>True if password matches, false otherwise</returns>
        public bool VerifyUserPassword(int userId, string password)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var user = conn.Find<User>(userId);

                if (user == null)
                    return false;

                return VerifyPassword(password, user.Password);
            }
        }

        /// <summary>
        /// Hashes a password using SHA256.
        /// </summary>
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Verifies a password against a hash.
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            return HashPassword(password) == hash;
        }
    }
}
