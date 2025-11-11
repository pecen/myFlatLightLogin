using myFlatLightLogin.Dal.Dto;
using System;

namespace myFlatLightLogin.Core.Services
{
    /// <summary>
    /// Service to track the currently logged-in user throughout the application.
    /// Provides access to user information and role-based authorization.
    /// </summary>
    public class CurrentUserService
    {
        private static CurrentUserService _instance;
        private static readonly object _lock = new object();
        private UserDto _currentUser;

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
                        if (_instance == null)
                        {
                            _instance = new CurrentUserService();
                        }
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
        /// </summary>
        public UserDto CurrentUser
        {
            get => _currentUser;
            private set => _currentUser = value;
        }

        /// <summary>
        /// Gets whether a user is currently logged in.
        /// </summary>
        public bool IsLoggedIn => _currentUser != null;

        /// <summary>
        /// Gets whether the current user has Admin role.
        /// </summary>
        public bool IsAdmin => _currentUser?.Role == UserRole.Admin;

        /// <summary>
        /// Gets whether the current user has User role.
        /// </summary>
        public bool IsUser => _currentUser?.Role == UserRole.User;

        /// <summary>
        /// Sets the current logged-in user.
        /// </summary>
        /// <param name="user">The user who has logged in</param>
        public void SetCurrentUser(UserDto user)
        {
            _currentUser = user;
            OnUserChanged?.Invoke(this, user);
        }

        /// <summary>
        /// Clears the current user (logout).
        /// </summary>
        public void ClearCurrentUser()
        {
            _currentUser = null;
            OnUserChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Event raised when the current user changes (login/logout).
        /// </summary>
        public event EventHandler<UserDto> OnUserChanged;

        /// <summary>
        /// Gets the current user's display name.
        /// </summary>
        public string GetDisplayName()
        {
            if (_currentUser == null)
                return "Guest";

            return _currentUser.Name ?? _currentUser.Email ?? "Unknown User";
        }

        /// <summary>
        /// Gets the current user's role as a string.
        /// </summary>
        public string GetRoleDisplayName()
        {
            if (_currentUser == null)
                return "Guest";

            return _currentUser.Role == UserRole.Admin ? "Administrator" : "User";
        }
    }
}
