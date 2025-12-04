using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.Library.Security;
using System;

namespace myFlatLightLogin.Core.Services
{
    /// <summary>
    /// Service to track the currently logged-in user throughout the application.
    /// Provides access to user information and role-based authorization.
    /// Now supports both legacy UserDto and BLL's UserPrincipal.
    /// </summary>
    public class CurrentUserService
    {
        private static CurrentUserService? _instance;
        private static readonly object _lock = new();
        private UserDto? _currentUser; // Legacy support
        private UserPrincipal? _currentPrincipal; // BLL principal

        /// <summary>
        /// Gets the singleton instance of CurrentUserService.
        /// </summary>
        public static CurrentUserService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new CurrentUserService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private CurrentUserService()
        {
        }

        /// <summary>
        /// Gets the currently logged-in user, or null if no user is logged in.
        /// (Legacy - for backward compatibility)
        /// </summary>
        public UserDto? CurrentUser
        {
            get => _currentUser;
            private set => _currentUser = value;
        }

        /// <summary>
        /// Gets the current UserPrincipal (BLL).
        /// </summary>
        public UserPrincipal? CurrentPrincipal
        {
            get => _currentPrincipal;
            private set => _currentPrincipal = value;
        }

        /// <summary>
        /// Gets whether a user is currently logged in.
        /// </summary>
        public bool IsLoggedIn => _currentPrincipal?.Identity?.IsAuthenticated == true || _currentUser != null;

        /// <summary>
        /// Gets whether the current user has Admin role.
        /// </summary>
        public bool IsAdmin =>
            _currentPrincipal?.IsAdmin == true ||
            _currentUser?.Role == UserRole.Admin;

        /// <summary>
        /// Gets whether the current user has User role.
        /// </summary>
        public bool IsUser =>
            _currentPrincipal?.IsUser == true ||
            _currentUser?.Role == UserRole.User;

        /// <summary>
        /// Sets the current logged-in user (legacy method for backward compatibility).
        /// </summary>
        /// <param name="user">The user who has logged in</param>
        [Obsolete("Use SetCurrentPrincipal instead")]
        public void SetCurrentUser(UserDto user)
        {
            _currentUser = user;
            _currentPrincipal = null; // Clear principal when using legacy method
            OnUserChanged?.Invoke(this, user);
        }

        /// <summary>
        /// Sets the current UserPrincipal (BLL method).
        /// </summary>
        /// <param name="principal">The authenticated principal</param>
        public void SetCurrentPrincipal(UserPrincipal principal)
        {
            _currentPrincipal = principal;
            _currentUser = null; // Clear legacy user when using BLL
            OnPrincipalChanged?.Invoke(this, principal);
        }

        /// <summary>
        /// Clears the current user (logout).
        /// </summary>
        public void ClearCurrentUser()
        {
            _currentUser = null;
            _currentPrincipal = null;
            OnUserChanged?.Invoke(this, null);
            OnPrincipalChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Event raised when the current user changes (login/logout) - Legacy.
        /// </summary>
        [Obsolete("Use OnPrincipalChanged instead")]
        public event EventHandler<UserDto?>? OnUserChanged;

        /// <summary>
        /// Event raised when the current principal changes (login/logout).
        /// </summary>
        public event EventHandler<UserPrincipal?>? OnPrincipalChanged;

        /// <summary>
        /// Gets the current user's display name.
        /// </summary>
        public string GetDisplayName()
        {
            // Prefer BLL principal
            if (_currentPrincipal?.Identity?.IsAuthenticated == true)
            {
                var identity = _currentPrincipal.Identity;
                return identity.Name ?? identity.Email ?? "Unknown User";
            }

            // Fallback to legacy
            if (_currentUser != null)
                return _currentUser.Name ?? _currentUser.Email ?? "Unknown User";

            return "Guest";
        }

        /// <summary>
        /// Gets the current user's role as a string.
        /// </summary>
        public string GetRoleDisplayName()
        {
            // Prefer BLL principal
            if (_currentPrincipal?.Identity?.IsAuthenticated == true)
            {
                return _currentPrincipal.Identity.Role == UserRole.Admin ? "Administrator" : "User";
            }

            // Fallback to legacy
            if (_currentUser != null)
                return _currentUser.Role == UserRole.Admin ? "Administrator" : "User";

            return "Guest";
        }

        /// <summary>
        /// Gets the current user's ID.
        /// </summary>
        public int GetUserId()
        {
            // Prefer BLL principal
            if (_currentPrincipal?.Identity?.IsAuthenticated == true)
            {
                return _currentPrincipal.Identity.UserId;
            }

            // Fallback to legacy
            return _currentUser?.Id ?? 0;
        }

        /// <summary>
        /// Gets the current user's email.
        /// </summary>
        public string GetUserEmail()
        {
            // Prefer BLL principal
            if (_currentPrincipal?.Identity?.IsAuthenticated == true)
            {
                return _currentPrincipal.Identity.Email ?? string.Empty;
            }

            // Fallback to legacy
            return _currentUser?.Email ?? string.Empty;
        }
    }
}
