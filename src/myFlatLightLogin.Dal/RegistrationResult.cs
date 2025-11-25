namespace myFlatLightLogin.Dal
{
    /// <summary>
    /// Result of a user registration operation.
    /// Provides details about where the user was registered (Firebase, SQLite, or failed).
    /// </summary>
    public class RegistrationResult
    {
        /// <summary>
        /// Indicates whether the registration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Indicates where the user was registered.
        /// </summary>
        public RegistrationMode Mode { get; set; }

        /// <summary>
        /// User-friendly message describing the result.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Detailed error message (for logging/debugging).
        /// </summary>
        public string ErrorDetails { get; set; }

        public static RegistrationResult FirebaseSuccess()
        {
            return new RegistrationResult
            {
                Success = true,
                Mode = RegistrationMode.Firebase,
                Message = "Account created successfully with Firebase! You can sign in from any device."
            };
        }

        public static RegistrationResult SQLiteSuccess(bool wasFirebaseAttempted, string firebaseError = null)
        {
            var message = wasFirebaseAttempted
                ? "Account created locally! Firebase was unreachable, so your account will sync when you're back online."
                : "Account created locally! You were offline, so your account will sync when you're back online.";

            return new RegistrationResult
            {
                Success = true,
                Mode = RegistrationMode.SQLiteOffline,
                Message = message,
                ErrorDetails = firebaseError
            };
        }

        public static RegistrationResult Failure(string errorMessage)
        {
            return new RegistrationResult
            {
                Success = false,
                Mode = RegistrationMode.Failed,
                Message = errorMessage,
                ErrorDetails = errorMessage
            };
        }
    }

    /// <summary>
    /// Indicates where a user was registered.
    /// </summary>
    public enum RegistrationMode
    {
        /// <summary>
        /// Registered in Firebase (online).
        /// </summary>
        Firebase,

        /// <summary>
        /// Registered in SQLite only (offline or Firebase unreachable).
        /// </summary>
        SQLiteOffline,

        /// <summary>
        /// Registration failed completely.
        /// </summary>
        Failed
    }
}
