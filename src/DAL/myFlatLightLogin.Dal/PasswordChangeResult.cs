namespace myFlatLightLogin.Dal
{
    /// <summary>
    /// Result of a password change operation.
    /// Indicates success/failure and provides context about the operation.
    /// </summary>
    public class PasswordChangeResult
    {
        /// <summary>
        /// Indicates if the password change was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// User-friendly message describing the result.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Indicates if this was an offline password change that requires sync.
        /// </summary>
        public bool IsOfflineChange { get; set; }

        /// <summary>
        /// Creates a successful password change result (online).
        /// </summary>
        public static PasswordChangeResult OnlineSuccess(string message = "Password changed successfully") =>
            new PasswordChangeResult
            {
                Success = true,
                Message = message,
                IsOfflineChange = false
            };

        /// <summary>
        /// Creates a successful offline password change result.
        /// </summary>
        public static PasswordChangeResult OfflineSuccess(string message = "Password changed offline. You will need your OLD password to sync when online.") =>
            new PasswordChangeResult
            {
                Success = true,
                Message = message,
                IsOfflineChange = true
            };

        /// <summary>
        /// Creates a failed password change result.
        /// </summary>
        public static PasswordChangeResult Failure(string message) =>
            new PasswordChangeResult
            {
                Success = false,
                Message = message,
                IsOfflineChange = false
            };
    }
}
