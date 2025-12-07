using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using myFlatLightLogin.UI.Common.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.UI.Common.Services;
using myFlatLightLogin.Library;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the Change Password view.
    /// Handles password changes using BLL's PasswordEdit.
    /// </summary>
    public class ChangePasswordViewModel : ViewModelBase
    {
        private static readonly ILogger _logger = Log.ForContext<ChangePasswordViewModel>();
        private readonly IDialogService _dialogService;

        #region Commands

        public AsyncRelayCommand ChangePasswordCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

        #endregion

        #region Properties

        private string _currentPassword;
        public string CurrentPassword
        {
            get => _currentPassword;
            set => SetProperty(ref _currentPassword, value);
        }

        private string _newPassword;
        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        // Note: IsOnline removed - BLL handles online/offline logic internally

        #endregion

        public ChangePasswordViewModel(INavigationService navigationService, IDialogService dialogService)
        {
            Navigation = navigationService;
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync, CanChangePassword);
            CancelCommand = new RelayCommand(o => Navigation.NavigateTo<HomeViewModel>());
        }

        private bool CanChangePassword()
        {
            return !string.IsNullOrWhiteSpace(CurrentPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        private async Task ChangePasswordAsync()
        {
            _logger.Information("========== PASSWORD CHANGE ATTEMPT STARTED ==========");

            // Get current user
            var userId = CurrentUserService.Instance.GetUserId();
            var userEmail = CurrentUserService.Instance.GetUserEmail();

            if (userId == 0 || string.IsNullOrEmpty(userEmail))
            {
                _logger.Warning("Password change attempted with no logged-in user");                                                                                                                                                                                                            
                await _dialogService.ShowMessageAsync("Error", "No user is currently logged in.",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings { AnimateShow = true, AnimateHide = true });
                return;
            }

            try
            {
                // Create PasswordEdit business object for validation and password change
                var passwordEdit = await PasswordEdit.NewPasswordEditAsync(userId, userEmail);

                // Set properties from form
                passwordEdit.OldPassword = CurrentPassword;
                passwordEdit.NewPassword = NewPassword;
                passwordEdit.ConfirmPassword = ConfirmPassword;

                // Validate using BLL business rules
                if (!passwordEdit.IsValid)
                {
                    var validationErrors = string.Join("\n", passwordEdit.BrokenRulesCollection);
                    _logger.Warning("Password change validation failed: {Errors}", validationErrors);

                    await _dialogService.ShowMessageAsync("Validation Error",
                       $"Please correct the following errors:\n\n{validationErrors}",
                       MessageDialogStyle.Affirmative,
                       new MetroDialogSettings { AnimateShow = true, AnimateHide = true });

                    return;
                }

                // Change password using BLL
                // BLL handles online/offline logic internally - UI doesn't need to know!
                var result = await passwordEdit.ChangePasswordAsync();

                if (result.Success)
                {
                    _logger.Information("Password changed successfully for user: {Email}", userEmail);

                    await _dialogService.ShowMessageAsync("Success", result.Message,
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings { AnimateShow = true, AnimateHide = true });

                    // Clear form and navigate back
                    ClearForm();
                    Navigation.NavigateTo<HomeViewModel>();
                }
                else
                {
                    _logger.Warning("Password change failed: {Message}", result.Message);

                    await _dialogService.ShowMessageAsync("Error", result.Message,
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings { AnimateShow = true, AnimateHide = true });
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Password change failed with exception");

                await _dialogService.ShowMessageAsync("Error", $"Failed to change password: {ex.Message}",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings { AnimateShow = true, AnimateHide = true });
            }
            finally
            {
                _logger.Information("========== PASSWORD CHANGE ATTEMPT COMPLETED ==========");
            }
        }

        public void ClearForm()
        {
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            // Note: Visibility state is managed by TogglePwdBox control and auto-resets when password is cleared
        }
    }
}
