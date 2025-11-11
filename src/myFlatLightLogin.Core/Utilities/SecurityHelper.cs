using System;
using System.Security.Principal;

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
    }
}
