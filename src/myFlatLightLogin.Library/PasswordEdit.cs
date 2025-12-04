using Csla;
using Csla.Core;
using Csla.Rules;
using Csla.Rules.CommonRules;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Threading.Tasks;

namespace myFlatLightLogin.Library
{
    /// <summary>
    /// Business object for changing user password.
    /// Handles password change business logic and validation.
    /// </summary>
    [Serializable]
    public class PasswordEdit : BusinessBase<PasswordEdit>
    {
        #region Properties

        public static readonly PropertyInfo<int> UserIdProperty = RegisterProperty<int>(c => c.UserId);
        /// <summary>
        /// User ID for which password is being changed.
        /// </summary>
        public int UserId
        {
            get => GetProperty(UserIdProperty);
            private set => LoadProperty(UserIdProperty, value);
        }

        public static readonly PropertyInfo<string> EmailProperty = RegisterProperty<string>(c => c.Email);
        /// <summary>
        /// User's email (for authentication).
        /// </summary>
        public string Email
        {
            get => GetProperty(EmailProperty);
            set => SetProperty(EmailProperty, value);
        }

        public static readonly PropertyInfo<string> OldPasswordProperty = RegisterProperty<string>(c => c.OldPassword);
        /// <summary>
        /// Current/old password (for verification).
        /// </summary>
        public string OldPassword
        {
            get => GetProperty(OldPasswordProperty);
            set => SetProperty(OldPasswordProperty, value);
        }

        public static readonly PropertyInfo<string> NewPasswordProperty = RegisterProperty<string>(c => c.NewPassword);
        /// <summary>
        /// New password.
        /// </summary>
        public string NewPassword
        {
            get => GetProperty(NewPasswordProperty);
            set => SetProperty(NewPasswordProperty, value);
        }

        public static readonly PropertyInfo<string> ConfirmPasswordProperty = RegisterProperty<string>(c => c.ConfirmPassword);
        /// <summary>
        /// Confirmation of new password.
        /// </summary>
        public string ConfirmPassword
        {
            get => GetProperty(ConfirmPasswordProperty);
            set => SetProperty(ConfirmPasswordProperty, value);
        }

        #endregion

        #region Business Rules

        protected override void AddBusinessRules()
        {
            base.AddBusinessRules();

            // Email is required
            BusinessRules.AddRule(new Required(EmailProperty));

            // Old password is required
            BusinessRules.AddRule(new Required(OldPasswordProperty));

            // New password is required and must be at least 6 characters
            BusinessRules.AddRule(new Required(NewPasswordProperty));
            BusinessRules.AddRule(new MinLength(NewPasswordProperty, 6)
            {
                MessageDelegate = () => "New password must be at least 6 characters long"
            });

            // Confirm password is required
            BusinessRules.AddRule(new Required(ConfirmPasswordProperty));

            // New password and confirm password must match
            BusinessRules.AddRule(new PasswordMatchRule(NewPasswordProperty, ConfirmPasswordProperty));

            // New password must be different from old password
            BusinessRules.AddRule(new PasswordDifferentRule(OldPasswordProperty, NewPasswordProperty));
        }

        /// <summary>
        /// Custom rule to validate that NewPassword and ConfirmPassword match.
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
                var newPassword = (string)context.InputPropertyValues[PrimaryProperty];
                var confirmPassword = (string)context.InputPropertyValues[ConfirmPasswordProperty];

                if (!string.IsNullOrEmpty(newPassword) && !string.IsNullOrEmpty(confirmPassword))
                {
                    if (newPassword != confirmPassword)
                    {
                        context.AddErrorResult("New Password and Confirm Password must match");
                    }
                }
            }
        }

        /// <summary>
        /// Custom rule to validate that new password is different from old password.
        /// </summary>
        private class PasswordDifferentRule : BusinessRule
        {
            private IPropertyInfo NewPasswordProperty { get; set; }

            public PasswordDifferentRule(IPropertyInfo primaryProperty, IPropertyInfo newPasswordProperty)
                : base(primaryProperty)
            {
                NewPasswordProperty = newPasswordProperty;
                InputProperties = new System.Collections.Generic.List<IPropertyInfo> { primaryProperty, newPasswordProperty };
            }

            protected override void Execute(IRuleContext context)
            {
                var oldPassword = (string)context.InputPropertyValues[PrimaryProperty];
                var newPassword = (string)context.InputPropertyValues[NewPasswordProperty];

                if (!string.IsNullOrEmpty(oldPassword) && !string.IsNullOrEmpty(newPassword))
                {
                    if (oldPassword == newPassword)
                    {
                        context.AddErrorResult("New password must be different from the old password");
                    }
                }
            }
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a new PasswordEdit for the specified user.
        /// </summary>
        public static async Task<PasswordEdit> NewPasswordEditAsync(int userId, string email)
        {
            return await DataPortal.CreateAsync<PasswordEdit>(new PasswordEditCriteria
            {
                UserId = userId,
                Email = email
            });
        }

        /// <summary>
        /// Changes the password for the user.
        /// This method validates old password and updates to new password.
        /// </summary>
        public async Task<PasswordChangeResult> ChangePasswordAsync(NetworkConnectivityService connectivityService, SyncService syncService)
        {
            // Validate business rules
            if (!IsValid)
            {
                return PasswordChangeResult.Failure("Please correct validation errors");
            }

            try
            {
                // Use HybridUserDal for password change
                var hybridDal = new HybridUserDal(connectivityService, syncService);
                var result = await hybridDal.ChangePasswordAsync(Email, OldPassword, NewPassword);

                return result;
            }
            catch (Exception ex)
            {
                return PasswordChangeResult.Failure(ex.Message);
            }
        }

        #endregion

        #region Data Access

        [Serializable]
        private class PasswordEditCriteria
        {
            public int UserId { get; set; }
            public string Email { get; set; }
        }

        [Create]
        private void Create(PasswordEditCriteria criteria)
        {
            UserId = criteria.UserId;
            Email = criteria.Email;
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            BusinessRules.CheckRules();
        }

        #endregion
    }
}
