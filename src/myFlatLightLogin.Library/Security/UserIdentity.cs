using Csla;
using Csla.Security;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Security.Principal;
using System.Threading.Tasks;

namespace myFlatLightLogin.Library.Security
{
    /// <summary>
    /// CSLA UserIdentity that represents an authenticated user.
    /// Contains user information and authentication state.
    /// </summary>
    [Serializable]
    public class UserIdentity : CslaIdentityBase<UserIdentity>
    {
        #region Properties

        public static readonly PropertyInfo<int> UserIdProperty = RegisterProperty<int>(c => c.UserId);
        /// <summary>
        /// User's database ID.
        /// </summary>
        public int UserId
        {
            get => GetProperty(UserIdProperty);
            private set => LoadProperty(UserIdProperty, value);
        }

        public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(c => c.Name);
        /// <summary>
        /// User's first name.
        /// </summary>
        public new string Name
        {
            get => GetProperty(NameProperty);
            private set => LoadProperty(NameProperty, value);
        }

        public static readonly PropertyInfo<string> LastNameProperty = RegisterProperty<string>(c => c.LastName);
        /// <summary>
        /// User's last name.
        /// </summary>
        public string LastName
        {
            get => GetProperty(LastNameProperty);
            private set => LoadProperty(LastNameProperty, value);
        }

        public static readonly PropertyInfo<string> EmailProperty = RegisterProperty<string>(c => c.Email);
        /// <summary>
        /// User's email address.
        /// </summary>
        public string Email
        {
            get => GetProperty(EmailProperty);
            private set => LoadProperty(EmailProperty, value);
        }

        public static readonly PropertyInfo<UserRole> RoleProperty = RegisterProperty<UserRole>(c => c.Role);
        /// <summary>
        /// User's role.
        /// </summary>
        public UserRole Role
        {
            get => GetProperty(RoleProperty);
            private set => LoadProperty(RoleProperty, value);
        }

        public static readonly PropertyInfo<string> FirebaseUidProperty = RegisterProperty<string>(c => c.FirebaseUid);
        /// <summary>
        /// Firebase User ID.
        /// </summary>
        public string FirebaseUid
        {
            get => GetProperty(FirebaseUidProperty);
            private set => LoadProperty(FirebaseUidProperty, value);
        }

        public static readonly PropertyInfo<bool> IsOnlineProperty = RegisterProperty<bool>(c => c.IsOnline);
        /// <summary>
        /// Indicates if user logged in while online or offline.
        /// </summary>
        public bool IsOnline
        {
            get => GetProperty(IsOnlineProperty);
            private set => LoadProperty(IsOnlineProperty, value);
        }

        /// <summary>
        /// Full name of the user.
        /// </summary>
        public string FullName => $"{Name} {LastName}".Trim();

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates an authenticated UserIdentity from login credentials.
        /// This is the main authentication entry point.
        /// </summary>
        public static async Task<UserIdentity> LoginAsync(string email, string password, NetworkConnectivityService connectivityService, SyncService syncService)
        {
            return await DataPortal.FetchAsync<UserIdentity>(new LoginCredentials
            {
                Email = email,
                Password = password,
                ConnectivityService = connectivityService,
                SyncService = syncService
            });
        }

        /// <summary>
        /// Creates a UserIdentity from a UserDto (internal use).
        /// </summary>
        internal static UserIdentity FromDto(UserDto dto, bool isOnline)
        {
            var identity = new UserIdentity();
            identity.LoadFromDto(dto, isOnline);
            return identity;
        }

        /// <summary>
        /// Returns an unauthenticated (anonymous) identity.
        /// </summary>
        public static UserIdentity UnauthenticatedIdentity()
        {
            return new UserIdentity();
        }

        #endregion

        #region Data Access

        [Serializable]
        private class LoginCredentials
        {
            public string? Email { get; set; }
            public string? Password { get; set; }
            public NetworkConnectivityService? ConnectivityService { get; set; }
            public SyncService? SyncService { get; set; }
        }

        [Fetch]
        private async Task Fetch(LoginCredentials credentials)
        {
            // Use HybridUserDal for authentication
            var hybridDal = new HybridUserDal(credentials.ConnectivityService, credentials.SyncService);

            var user = await hybridDal.SignInAsync(credentials.Email, credentials.Password);

            if (user == null)
            {
                // Authentication failed
                throw new System.Security.SecurityException("Invalid email or password");
            }

            // Load user data and mark as authenticated
            LoadFromDto(user, hybridDal.IsOnline);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Loads identity from a UserDto.
        /// </summary>
        private void LoadFromDto(UserDto dto, bool isOnline)
        {
            LoadProperty(UserIdProperty, dto.Id);
            LoadProperty(NameProperty, dto.Name ?? string.Empty);
            LoadProperty(LastNameProperty, dto.Lastname ?? string.Empty);
            LoadProperty(EmailProperty, dto.Email ?? string.Empty);
            LoadProperty(RoleProperty, dto.Role);
            LoadProperty(FirebaseUidProperty, dto.FirebaseUid ?? string.Empty);
            LoadProperty(IsOnlineProperty, isOnline);

            // Set the base class Name property for IIdentity interface
            base.Name = dto.Email ?? string.Empty;

            // Mark as authenticated
            IsAuthenticated = true;
        }

        #endregion
    }
}
