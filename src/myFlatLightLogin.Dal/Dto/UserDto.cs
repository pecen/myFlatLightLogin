namespace myFlatLightLogin.Dal.Dto
{
    /// <summary>
    /// Data Transfer Object for User entity.
    /// Used to transfer user data between application layers.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Local database ID (for SQLite).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// User's first name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// User's last name.
        /// </summary>
        public string? Lastname { get; set; }

        /// <summary>
        /// Username (can be same as Email for Firebase).
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Email address (required for Firebase authentication).
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// User's password (plain text for input, hashed for storage).
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Password confirmation (for registration).
        /// </summary>
        public string? ConfirmPassword { get; set; }

        /// <summary>
        /// Firebase User ID (UID) - used for Firebase implementations.
        /// </summary>
        public string? FirebaseUid { get; set; }

        /// <summary>
        /// Firebase authentication token for authenticated API calls.
        /// </summary>
        public string? FirebaseAuthToken { get; set; }

        /// <summary>
        /// User's role for application-level authorization.
        /// Default is User (0). First registered user becomes Admin (1).
        /// </summary>
        public UserRole Role { get; set; } = UserRole.User;

        /// <summary>
        /// Indicates if the user's password was changed while offline.
        /// When true, sync requires old password for Firebase authentication.
        /// </summary>
        public bool PendingPasswordChange { get; set; }

        /// <summary>
        /// Hash of the old password (before offline change).
        /// Used to verify old password during sync. Cleared after successful sync.
        /// </summary>
        public string? OldPasswordHash { get; set; }

        /// <summary>
        /// Timestamp when password was last changed (UTC, ISO 8601).
        /// </summary>
        public string? PasswordChangedDate { get; set; }
    }
}
