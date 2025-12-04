using Csla;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Threading.Tasks;

namespace myFlatLightLogin.Library
{
    /// <summary>
    /// Read-only business object for displaying User information.
    /// Used for read-only scenarios like displaying user details.
    /// </summary>
    [Serializable]
    public class UserInfo : ReadOnlyBase<UserInfo>
    {
        #region Properties

        /// <summary>
        /// Local database ID
        /// </summary>
        public static readonly PropertyInfo<int> IdProperty = RegisterProperty<int>(c => c.Id);
        public int Id
        {
            get => GetProperty(IdProperty);
            private set => LoadProperty(IdProperty, value);
        }

        public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(c => c.Name);
        public string Name
        {
            get => GetProperty(NameProperty);
            private set => LoadProperty(NameProperty, value);
        }

        public static readonly PropertyInfo<string> LastNameProperty = RegisterProperty<string>(c => c.LastName);
        public string LastName
        {
            get => GetProperty(LastNameProperty);
            private set => LoadProperty(LastNameProperty, value);
        }

        /// <summary>
        /// Computed property for full name.
        /// </summary>
        public string FullName => $"{Name} {LastName}".Trim();

        public static readonly PropertyInfo<string> UserNameProperty = RegisterProperty<string>(c => c.UserName);
        public string UserName
        {
            get => GetProperty(UserNameProperty);
            private set => LoadProperty(UserNameProperty, value);
        }

        public static readonly PropertyInfo<string> EmailProperty = RegisterProperty<string>(c => c.Email);
        public string Email
        {
            get => GetProperty(EmailProperty);
            private set => LoadProperty(EmailProperty, value);
        }

        public static readonly PropertyInfo<string> FirebaseUidProperty = RegisterProperty<string>(c => c.FirebaseUid);
        public string FirebaseUid
        {
            get => GetProperty(FirebaseUidProperty);
            private set => LoadProperty(FirebaseUidProperty, value);
        }

        public static readonly PropertyInfo<string> RegistrationDateProperty = RegisterProperty<string>(c => c.RegistrationDate);
        public string RegistrationDate
        {
            get => GetProperty(RegistrationDateProperty);
            private set => LoadProperty(RegistrationDateProperty, value);
        }

        public static readonly PropertyInfo<UserRole> RoleProperty = RegisterProperty<UserRole>(c => c.Role);
        public UserRole Role
        {
            get => GetProperty(RoleProperty);
            private set => LoadProperty(RoleProperty, value);
        }

        public static readonly PropertyInfo<bool> PendingPasswordChangeProperty = RegisterProperty<bool>(c => c.PendingPasswordChange);
        public bool PendingPasswordChange
        {
            get => GetProperty(PendingPasswordChangeProperty);
            private set => LoadProperty(PendingPasswordChangeProperty, value);
        }

        public static readonly PropertyInfo<string> PasswordChangedDateProperty = RegisterProperty<string>(c => c.PasswordChangedDate);
        public string PasswordChangedDate
        {
            get => GetProperty(PasswordChangedDateProperty);
            private set => LoadProperty(PasswordChangedDateProperty, value);
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Gets a read-only user by ID.
        /// </summary>
        public static async Task<UserInfo> GetUserAsync(int id)
        {
            return await DataPortal.FetchAsync<UserInfo>(id);
        }

        /// <summary>
        /// Gets a read-only user by email.
        /// </summary>
        public static async Task<UserInfo> GetUserByEmailAsync(string email)
        {
            return await DataPortal.FetchAsync<UserInfo>(new UserFetchCriteria { Email = email });
        }

        /// <summary>
        /// Creates a UserInfo from a UserDto (for authentication scenarios).
        /// </summary>
        internal static UserInfo FromDto(UserDto dto)
        {
            var userInfo = new UserInfo();
            userInfo.LoadFromDto(dto);
            return userInfo;
        }

        #endregion

        #region Data Access

        [Serializable]
        private class UserFetchCriteria
        {
            public string? Email { get; set; }
        }

        [Fetch]
        private void Fetch(int id)
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IUserDal>();
                var data = dal.Fetch(id);

                if (data == null)
                    throw new Exception($"User with ID {id} not found");

                LoadFromDto(data);
            }
        }

        [Fetch]
        private void Fetch(UserFetchCriteria criteria)
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IUserDal>();

                // Try to find user by email (we'll need to add this method to IUserDal)
                // For now, we'll throw a not implemented exception
                throw new NotImplementedException("Fetch by email not yet implemented in DAL");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Loads properties from a UserDto.
        /// </summary>
        private void LoadFromDto(UserDto dto)
        {
            LoadProperty(IdProperty, dto.Id);
            LoadProperty(NameProperty, dto.Name ?? string.Empty);
            LoadProperty(LastNameProperty, dto.Lastname ?? string.Empty);
            LoadProperty(UserNameProperty, dto.Username ?? string.Empty);
            LoadProperty(EmailProperty, dto.Email ?? string.Empty);
            LoadProperty(FirebaseUidProperty, dto.FirebaseUid ?? string.Empty);
            LoadProperty(RegistrationDateProperty, dto.RegistrationDate ?? string.Empty);
            LoadProperty(RoleProperty, dto.Role);
            LoadProperty(PendingPasswordChangeProperty, dto.PendingPasswordChange);
            LoadProperty(PasswordChangedDateProperty, dto.PasswordChangedDate ?? string.Empty);
        }

        #endregion
    }
}
