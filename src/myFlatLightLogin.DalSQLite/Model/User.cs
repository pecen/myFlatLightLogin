using myFlatLightLogin.Dal.Dto;
using SQLite;
using System;

namespace myFlatLightLogin.DalSQLite.Model
{
    /// <summary>
    /// SQLite User model with sync support for Firebase integration.
    /// </summary>
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Lastname { get; set; }

        /// <summary>
        /// Username (also used as email for Firebase authentication).
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Hashed password for local authentication.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Email address for Firebase authentication.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Firebase User ID for synchronization.
        /// </summary>
        public string FirebaseUid { get; set; }

        /// <summary>
        /// Registration date timestamp in UTC when the account was created.
        /// Format: ISO 8601 (yyyy-MM-ddTHH:mm:ss.fffZ)
        /// This is set once during registration and never changed.
        /// </summary>
        public string RegistrationDate { get; set; }

        /// <summary>
        /// Last modified timestamp in UTC for conflict resolution.
        /// Format: ISO 8601 (yyyy-MM-ddTHH:mm:ss.fffZ)
        /// </summary>
        public string LastModified { get; set; }

        /// <summary>
        /// Indicates if this record needs to be synced to Firebase.
        /// </summary>
        public bool NeedsSync { get; set; }

        /// <summary>
        /// Foreign key to the Role table.
        /// References the user's role for application-level authorization.
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// Indicates if the user's password was changed while offline and needs special sync handling.
        /// When true, the user will be prompted for their old password during sync.
        /// </summary>
        public bool PendingPasswordChange { get; set; }

        /// <summary>
        /// Temporarily stores the hash of the old password before an offline password change.
        /// Used to verify the old password when syncing with Firebase.
        /// Cleared after successful sync.
        /// </summary>
        public string OldPasswordHash { get; set; }

        /// <summary>
        /// Timestamp when password was last changed (UTC, ISO 8601 format).
        /// </summary>
        public string PasswordChangedDate { get; set; }
    }
}
