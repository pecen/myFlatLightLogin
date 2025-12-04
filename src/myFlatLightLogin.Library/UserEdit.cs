using Csla;
using Csla.Rules;
using Csla.Rules.CommonRules;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace myFlatLightLogin.Library
{
    /// <summary>
    /// Editable business object for User management.
    /// Handles user creation, updates, and business rules.
    /// </summary>
    [Serializable]
    public class UserEdit : BusinessBase<UserEdit>
    {
        #region Properties

        public static readonly PropertyInfo<int> IdProperty = RegisterProperty<int>(c => c.Id);
        /// <summary>
        /// Local database ID (SQLite).
        /// </summary>
        public int Id
        {
            get => GetProperty(IdProperty);
            private set => LoadProperty(IdProperty, value);
        }

        public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(c => c.Name);
        /// <summary>
        /// User's first name.
        /// </summary>
        public string Name
        {
            get => GetProperty(NameProperty);
            set => SetProperty(NameProperty, value);
        }

        public static readonly PropertyInfo<string> LastNameProperty = RegisterProperty<string>(c => c.LastName);
        /// <summary>
        /// User's last name.
        /// </summary>
        public string LastName
        {
            get => GetProperty(LastNameProperty);
            set => SetProperty(LastNameProperty, value);
        }

        public static readonly PropertyInfo<string> UserNameProperty = RegisterProperty<string>(c => c.UserName);
        /// <summary>
        /// Username.
        /// </summary>
        public string UserName
        {
            get => GetProperty(UserNameProperty);
            set => SetProperty(UserNameProperty, value);
        }

        public static readonly PropertyInfo<string> EmailProperty = RegisterProperty<string>(c => c.Email);
        /// <summary>
        /// Email address (required for authentication).
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email
        {
            get => GetProperty(EmailProperty);
            set => SetProperty(EmailProperty, value);
        }

        public static readonly PropertyInfo<string> PasswordProperty = RegisterProperty<string>(c => c.Password);
        /// <summary>
        /// User's password.
        /// </summary>
        public string Password
        {
            get => GetProperty(PasswordProperty);
            set => SetProperty(PasswordProperty, value);
        }

        public static readonly PropertyInfo<string> ConfirmPasswordProperty = RegisterProperty<string>(c => c.ConfirmPassword);
        /// <summary>
        /// Password confirmation (for registration/password changes).
        /// </summary>
        public string ConfirmPassword
        {
            get => GetProperty(ConfirmPasswordProperty);
            set => SetProperty(ConfirmPasswordProperty, value);
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

        public static readonly PropertyInfo<string> FirebaseAuthTokenProperty = RegisterProperty<string>(c => c.FirebaseAuthToken);
        /// <summary>
        /// Firebase authentication token.
        /// </summary>
        public string FirebaseAuthToken
        {
            get => GetProperty(FirebaseAuthTokenProperty);
            private set => LoadProperty(FirebaseAuthTokenProperty, value);
        }

        public static readonly PropertyInfo<string> RegistrationDateProperty = RegisterProperty<string>(c => c.RegistrationDate);
        /// <summary>
        /// Registration date timestamp (ISO 8601).
        /// </summary>
        public string RegistrationDate
        {
            get => GetProperty(RegistrationDateProperty);
            private set => LoadProperty(RegistrationDateProperty, value);
        }

        public static readonly PropertyInfo<UserRole> RoleProperty = RegisterProperty<UserRole>(c => c.Role, defaultValue: UserRole.User);
        /// <summary>
        /// User's role for authorization.
        /// </summary>
        public UserRole Role
        {
            get => GetProperty(RoleProperty);
            set => SetProperty(RoleProperty, value);
        }

        public static readonly PropertyInfo<bool> PendingPasswordChangeProperty = RegisterProperty<bool>(c => c.PendingPasswordChange);
        /// <summary>
        /// Indicates if password was changed offline and needs sync.
        /// </summary>
        public bool PendingPasswordChange
        {
            get => GetProperty(PendingPasswordChangeProperty);
            private set => LoadProperty(PendingPasswordChangeProperty, value);
        }

        public static readonly PropertyInfo<string> OldPasswordHashProperty = RegisterProperty<string>(c => c.OldPasswordHash);
        /// <summary>
        /// Hash of old password (for sync purposes).
        /// </summary>
        public string OldPasswordHash
        {
            get => GetProperty(OldPasswordHashProperty);
            private set => LoadProperty(OldPasswordHashProperty, value);
        }

        public static readonly PropertyInfo<string> PasswordChangedDateProperty = RegisterProperty<string>(c => c.PasswordChangedDate);
        /// <summary>
        /// Timestamp when password was last changed (ISO 8601).
        /// </summary>
        public string PasswordChangedDate
        {
            get => GetProperty(PasswordChangedDateProperty);
            private set => LoadProperty(PasswordChangedDateProperty, value);
        }

        #endregion

        #region Business Rules

        protected override void AddBusinessRules()
        {
            base.AddBusinessRules();

            // Email is required and must be valid
            BusinessRules.AddRule(new Required(EmailProperty));
            BusinessRules.AddRule(new RegExMatch(EmailProperty, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"));

            // Password is required for new users (when Id is 0)
            BusinessRules.AddRule(new Required(PasswordProperty) { PrimaryProperty = PasswordProperty });
            BusinessRules.AddRule(new MinLength(PasswordProperty, 6) { MessageDelegate = () => "Password must be at least 6 characters long" });

            // Password and ConfirmPassword must match
            BusinessRules.AddRule(new PasswordMatchRule(PasswordProperty, ConfirmPasswordProperty));

            // Name is required
            BusinessRules.AddRule(new Required(NameProperty));

            // LastName is required
            BusinessRules.AddRule(new Required(LastNameProperty));
        }

        /// <summary>
        /// Custom rule to validate that Password and ConfirmPassword match.
        /// </summary>
        private class PasswordMatchRule : BusinessRule
        {
            private IPropertyInfo ConfirmPasswordProperty { get; set; }

            public PasswordMatchRule(IPropertyInfo primaryProperty, IPropertyInfo confirmPasswordProperty)
                : base(primaryProperty)
            {
                ConfirmPasswordProperty = confirmPasswordProperty;
                InputProperties = new System.Collections.Generic.List<IPropertyInfo> { primaryProperty, confirmPasswordProperty };
            }

            protected override void Execute(IRuleContext context)
            {
                var password = (string)context.InputPropertyValues[PrimaryProperty];
                var confirmPassword = (string)context.InputPropertyValues[ConfirmPasswordProperty];

                if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(confirmPassword))
                {
                    if (password != confirmPassword)
                    {
                        context.AddErrorResult("Password and Confirm Password must match");
                    }
                }
            }
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a new user for registration.
        /// </summary>
        public static async Task<UserEdit> NewUserAsync()
        {
            return await DataPortal.CreateAsync<UserEdit>();
        }

        /// <summary>
        /// Gets an existing user by ID.
        /// </summary>
        public static async Task<UserEdit> GetUserAsync(int id)
        {
            return await DataPortal.FetchAsync<UserEdit>(id);
        }

        /// <summary>
        /// Gets an existing user by email.
        /// </summary>
        public static async Task<UserEdit> GetUserByEmailAsync(string email)
        {
            return await DataPortal.FetchAsync<UserEdit>(new UserFetchCriteria { Email = email });
        }

        /// <summary>
        /// Deletes a user by ID.
        /// </summary>
        public static async Task DeleteUserAsync(int id)
        {
            await DataPortal.DeleteAsync<UserEdit>(id);
        }

        #endregion

        #region Data Access

        [Serializable]
        private class UserFetchCriteria
        {
            public string Email { get; set; }
        }

        [Create]
        private void Create()
        {
            // Set defaults for new user
            Id = 0;
            Role = UserRole.User;
            RegistrationDate = DateTime.UtcNow.ToString("o"); // ISO 8601 format
            BusinessRules.CheckRules();
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

        [Insert]
        private void Insert()
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IUserDal>();
                var dto = ToDto();

                bool success = dal.Insert(dto);

                if (!success)
                    throw new Exception("Failed to insert user");

                // After insert, we need to fetch the user to get the assigned ID
                // This is a simplification - in a real scenario, Insert should return the new ID
            }
        }

        [Update]
        private void Update()
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IUserDal>();
                var dto = ToDto();

                bool success = dal.Update(dto);

                if (!success)
                    throw new Exception("Failed to update user");
            }
        }

        [DeleteSelf]
        private void DeleteSelf()
        {
            Delete(Id);
        }

        [Delete]
        private void Delete(int id)
        {
            using (var dalManager = DalFactory.GetManager())
            {
                var dal = dalManager.GetProvider<IUserDal>();
                bool success = dal.Delete(id);

                if (!success)
                    throw new Exception("Failed to delete user");
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
            LoadProperty(PasswordProperty, dto.Password ?? string.Empty);
            LoadProperty(FirebaseUidProperty, dto.FirebaseUid ?? string.Empty);
            LoadProperty(FirebaseAuthTokenProperty, dto.FirebaseAuthToken ?? string.Empty);
            LoadProperty(RegistrationDateProperty, dto.RegistrationDate ?? string.Empty);
            LoadProperty(RoleProperty, dto.Role);
            LoadProperty(PendingPasswordChangeProperty, dto.PendingPasswordChange);
            LoadProperty(OldPasswordHashProperty, dto.OldPasswordHash ?? string.Empty);
            LoadProperty(PasswordChangedDateProperty, dto.PasswordChangedDate ?? string.Empty);
        }

        /// <summary>
        /// Converts the business object to a UserDto.
        /// </summary>
        private UserDto ToDto()
        {
            return new UserDto
            {
                Id = Id,
                Name = Name,
                Lastname = LastName,
                Username = UserName,
                Email = Email,
                Password = Password,
                ConfirmPassword = ConfirmPassword,
                FirebaseUid = FirebaseUid,
                FirebaseAuthToken = FirebaseAuthToken,
                RegistrationDate = RegistrationDate,
                Role = Role,
                PendingPasswordChange = PendingPasswordChange,
                OldPasswordHash = OldPasswordHash,
                PasswordChangedDate = PasswordChangedDate
            };
        }

        #endregion
    }
}
