using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.DalSQLite.Model;
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
        private static string dbFile = Path.Combine(Environment.CurrentDirectory, "security.db3");

        public UserDto Fetch(int id)
        {
            List<UserDto> items = DbCore.Fetch<UserDto>();
            return items.FirstOrDefault(i => i.Id == id);
        }

        public bool Insert(UserDto userDto)
        {
            var user = new User
            {
                Name = userDto.Name,
                Lastname = userDto.Lastname,
                Username = userDto.Username ?? userDto.Email,
                Email = userDto.Email,
                Password = HashPassword(userDto.Password),
                FirebaseUid = userDto.FirebaseUid,
                LastModified = DateTime.UtcNow.ToString("o"),
                NeedsSync = true // Mark for sync to Firebase
            };

            return DbCore.Insert(user);
        }

        public bool Update(UserDto userDto)
        {
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();

                var user = conn.Table<User>().FirstOrDefault(u => u.Id == userDto.Id);
                if (user == null)
                    return false;

                user.Name = userDto.Name;
                user.Lastname = userDto.Lastname;
                user.Username = userDto.Username ?? userDto.Email;
                user.Email = userDto.Email;
                user.FirebaseUid = userDto.FirebaseUid;
                user.LastModified = DateTime.UtcNow.ToString("o");
                user.NeedsSync = true; // Mark for sync to Firebase

                // Only update password if provided
                if (!string.IsNullOrEmpty(userDto.Password))
                {
                    user.Password = HashPassword(userDto.Password);
                }

                return conn.Update(user) > 0;
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
            using (var conn = new SQLiteConnection(dbFile))
            {
                conn.CreateTable<User>();
                var user = conn.Table<User>().FirstOrDefault(u => u.Email == email || u.Username == email);

                if (user == null)
                    return null;

                // Verify password
                if (!VerifyPassword(password, user.Password))
                    return null;

                return new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Lastname = user.Lastname,
                    Username = user.Username,
                    Email = user.Email,
                    FirebaseUid = user.FirebaseUid
                };
            }
        }

        /// <summary>
        /// Gets all users that need to be synced to Firebase.
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
                    FirebaseUid = u.FirebaseUid
                }).ToList();
            }
        }

        /// <summary>
        /// Marks a user as synced with Firebase.
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
                    FirebaseUid = user.FirebaseUid
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
                    FirebaseUid = user.FirebaseUid
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
                        NeedsSync = false
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

                    return conn.Update(user) > 0;
                }
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
