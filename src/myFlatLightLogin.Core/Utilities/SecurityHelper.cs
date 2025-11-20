using System;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace myFlatLightLogin.Core.Utilities
{
    /// <summary>
    /// Helper class for security-related operations
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>
        /// Checks if the current user is a member of the Windows Administrators group
        /// </summary>
        /// <returns>True if user is an administrator, false otherwise</returns>
        public static bool IsUserAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception)
            {
                // If we can't determine, assume not an admin (fail-safe)
                return false;
            }
        }

        /// <summary>
        /// Gets the current Windows username
        /// </summary>
        /// <returns>The current username (DOMAIN\Username format)</returns>
        public static string GetCurrentUsername()
        {
            try
            {
                return WindowsIdentity.GetCurrent()?.Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Hashes a password using SHA256.
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <returns>Base64-encoded hash of the password</returns>
        public static string HashPassword(string password)
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
        /// <param name="password">The password to verify</param>
        /// <param name="hash">The hash to verify against</param>
        /// <returns>True if the password matches the hash, false otherwise</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            return HashPassword(password) == hash;
        }
    }
}
