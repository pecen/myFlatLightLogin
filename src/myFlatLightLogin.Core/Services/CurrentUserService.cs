using myFlatLightLogin.Dal.Dto;
using System;

namespace myFlatLightLogin.Core.Services
{
    /// <summary>
    /// Service to track the currently logged-in user throughout the application.
    /// Provides access to user information and role-based authorization.
    /// Stores basic user info to avoid circular dependencies with BLL.
    /// </summary>
    public class CurrentUserService
    {
        private static CurrentUserService? _instance;
        private static readonly object _lock = new();
        private UserDto? _currentUser; // Legacy support
        private CurrentUserInfo? _currentUserInfo; // Lightweight user info from BLL

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
        /// Gets the current user info from BLL.
        /// </summary>
        public CurrentUserInfo? CurrentUserInfo
        {
            get => _currentUserInfo;
            private set => _currentUserInfo = value;
        }

        /// <summary>
        /// Gets whether a user is currently logged in.
        /// </summary>
        public bool IsLoggedIn => _currentUserInfo != null || _currentUser != null;

        /// <summary>
        /// Gets whether the current user has Admin role.
        /// </summary>
        public bool IsAdmin =>
            _currentUserInfo?.Role == UserRole.Admin ||
            _currentUser?.Role == UserRole.Admin;

        /// <summary>
        /// Gets whether the current user has User role.
        /// </summary>
        public bool IsUser =>
            _currentUserInfo?.Role == UserRole.User ||
            _currentUser?.Role == UserRole.User;

        /// <summary>
        /// Sets the current logged-in user (legacy method for backward compatibility).
        /// </summary>
        /// <param name="user">The user who has logged in</param>
        [Obsolete("Use SetCurrentUserInfo instead")]
        public void SetCurrentUser(UserDto user)
        {
            _currentUser = user;
            _currentUserInfo = null; // Clear user info when using legacy method
            OnUserChanged?.Invoke(this, user);
        }

        /// <summary>
        /// Sets the current user info from BLL (avoids circular dependency with Library).
        /// </summary>
        /// <param name="userInfo">The authenticated user info</param>
        public void SetCurrentUserInfo(CurrentUserInfo userInfo)
        {
            _currentUserInfo = userInfo;
            _currentUser = null; // Clear legacy user when using BLL
            OnUserInfoChanged?.Invoke(this, userInfo);
        }

        /// <summary>
        /// Clears the current user (logout).
        /// </summary>
        public void ClearCurrentUser()
        {
            _currentUser = null;
            _currentUserInfo = null;
            OnUserChanged?.Invoke(this, null);
            OnUserInfoChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Event raised when the current user changes (login/logout) - Legacy.
        /// </summary>
        [Obsolete("Use OnUserInfoChanged instead")]
        public event EventHandler<UserDto?>? OnUserChanged;

        /// <summary>
        /// Event raised when the current user info changes (login/logout).
        /// </summary>
        public event EventHandler<CurrentUserInfo?>? OnUserInfoChanged;

        /// <summary>
        /// Gets the current user's display name.
        /// </summary>
        public string GetDisplayName()
        {
            // Prefer BLL user info
            if (_currentUserInfo != null)
            {
                return _currentUserInfo.FirstName ?? _currentUserInfo.Email ?? "Unknown User";
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
            // Prefer BLL user info
            if (_currentUserInfo != null)
            {
                return _currentUserInfo.Role == UserRole.Admin ? "Administrator" : "User";
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
            // Prefer BLL user info
            if (_currentUserInfo != null)
            {
                return _currentUserInfo.UserId;
            }

            // Fallback to legacy
            return _currentUser?.Id ?? 0;
        }

        /// <summary>
        /// Gets the current user's email.
        /// </summary>
        public string GetUserEmail()
        {
            // Prefer BLL user info
            if (_currentUserInfo != null)
            {
                return _currentUserInfo.Email ?? string.Empty;
            }

            // Fallback to legacy
            return _currentUser?.Email ?? string.Empty;
        }
    }

    /// <summary>
    /// Lightweight user info class to avoid circular dependency with BLL.
    /// Contains essential user information extracted from UserPrincipal.
    /// </summary>
    public class CurrentUserInfo
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? Email { get; set; }
        public UserRole Role { get; set; }
        public bool IsOnline { get; set; }
    }
}
