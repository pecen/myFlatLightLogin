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
        /// Last modified timestamp in UTC for conflict resolution.
        /// Format: ISO 8601 (yyyy-MM-ddTHH:mm:ss.fffZ)
        /// </summary>
        public string LastModified { get; set; }

        /// <summary>
        /// Indicates if this record needs to be synced to Firebase.
        /// </summary>
        public bool NeedsSync { get; set; }
    }
}
