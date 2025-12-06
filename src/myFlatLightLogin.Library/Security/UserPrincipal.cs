using Csla;
using Csla.Security;
using System;
using System.Security.Principal;
using System.Threading.Tasks;

namespace myFlatLightLogin.Library.Security
{
    /// <summary>
    /// CSLA UserPrincipal that represents the current security context.
    /// Provides authentication and role-based authorization.
    /// </summary>
    [Serializable]
    public class UserPrincipal : CslaPrincipal
    {
        /// <summary>
        /// Private constructor - use factory methods to create instances.
        /// </summary>
        private UserPrincipal(IIdentity identity) : base(identity)
        {
        }

        /// <summary>
        /// Gets the UserIdentity for the current user.
        /// </summary>
        public new UserIdentity Identity => (UserIdentity)base.Identity;

        #region Factory Methods

        /// <summary>
        /// Authenticates a user and returns a UserPrincipal.
        /// This is the main entry point for login.
        /// Infrastructure services are resolved internally - UI should NOT pass them.
        /// </summary>
        public static async Task<UserPrincipal> LoginAsync(string email, string password)
        {
            var identity = await UserIdentity.LoginAsync(email, password);
            var principal = new UserPrincipal(identity);

            return principal;
        }

        /// <summary>
        /// Logs out the current user and returns an unauthenticated principal.
        /// </summary>
        public static UserPrincipal Logout()
        {
            var identity = UserIdentity.UnauthenticatedIdentity();
            return new UserPrincipal(identity);
        }

        /// <summary>
        /// Returns an unauthenticated (anonymous) principal.
        /// </summary>
        public static UserPrincipal UnauthenticatedPrincipal()
        {
            return new UserPrincipal(UserIdentity.UnauthenticatedIdentity());
        }

        #endregion

        #region Authorization

        /// <summary>
        /// Checks if the user is in a specific role.
        /// </summary>
        public override bool IsInRole(string role)
        {
            if (!Identity.IsAuthenticated)
                return false;

            // Check if the user's role matches the requested role
            var userIdentity = Identity as UserIdentity;
            if (userIdentity == null)
                return false;

            // Parse the requested role
            if (Enum.TryParse<Dal.Dto.UserRole>(role, true, out var requestedRole))
            {
                // Admin has access to everything
                if (userIdentity.Role == Dal.Dto.UserRole.Admin)
                    return true;

                // Otherwise, check exact match
                return userIdentity.Role == requestedRole;
            }

            return false;
        }

        /// <summary>
        /// Checks if the current user is an Admin.
        /// </summary>
        public bool IsAdmin => IsInRole("Admin");

        /// <summary>
        /// Checks if the current user is a regular User.
        /// </summary>
        public bool IsUser => IsInRole("User");

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the current user's ID.
        /// Returns 0 if not authenticated.
        /// </summary>
        public int GetUserId()
        {
            return Identity.IsAuthenticated ? Identity.UserId : 0;
        }

        /// <summary>
        /// Gets the current user's email.
        /// Returns empty string if not authenticated.
        /// </summary>
        public string GetUserEmail()
        {
            return Identity.IsAuthenticated ? Identity.Email : string.Empty;
        }

        /// <summary>
        /// Gets the current user's full name.
        /// Returns empty string if not authenticated.
        /// </summary>
        public string GetUserFullName()
        {
            return Identity.IsAuthenticated ? Identity.FullName : string.Empty;
        }

        #endregion
    }
}
