using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public class ChangePasswordViewModel : ViewModelBase
    {
        private readonly HybridUserDal _hybridUserDal;
        private readonly NetworkConnectivityService _connectivityService;

        public AsyncRelayCommand ChangePasswordCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

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

        public bool IsOnline => _connectivityService.IsOnline;
        public string ConnectionStatus => IsOnline ? "🟢 Online" : "🔴 Offline";

        public ChangePasswordViewModel(HybridUserDal hybridUserDal, NetworkConnectivityService connectivityService)
        {
            _hybridUserDal = hybridUserDal;
            _connectivityService = connectivityService;

            ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync, CanChangePassword);
            CancelCommand = new RelayCommand(o => Navigation.NavigateTo<HomeViewModel>());

            // Subscribe to connectivity changes to update UI
            _connectivityService.ConnectivityChanged += (sender, isOnline) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(IsOnline));
                    OnPropertyChanged(nameof(ConnectionStatus));
                });
            };
        }

        private bool CanChangePassword()
        {
            return !string.IsNullOrWhiteSpace(CurrentPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        private async Task ChangePasswordAsync()
        {
            var window = (MetroWindow)Application.Current.MainWindow;

            // Validate inputs
            if (!ValidateInputs(window))
                return;

            // Show offline warning if applicable
            if (!IsOnline)
            {
                var offlineWarning = await ShowOfflineWarningAsync(window);
                if (!offlineWarning)
                {
                    // User cancelled
                    return;
                }
            }

            // Get current user ID
            var currentUser = CurrentUserService.Instance.CurrentUser;
            if (currentUser == null)
            {
                await window.ShowMessageAsync("Error", "No user is currently logged in.",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings { AnimateShow = true, AnimateHide = true });
                return;
            }

            try
            {
                // Change password
                var result = await _hybridUserDal.ChangePasswordAsync(
                    currentUser.Id,
                    CurrentPassword,
                    NewPassword);

                if (result.Success)
                {
                    await window.ShowMessageAsync("Success", result.Message,
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings { AnimateShow = true, AnimateHide = true });

                    // Clear form and navigate back
                    ClearForm();
                    Navigation.NavigateTo<HomeViewModel>();
                }
                else
                {
                    await window.ShowMessageAsync("Error", result.Message,
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings { AnimateShow = true, AnimateHide = true });
                }
            }
            catch (Exception ex)
            {
                await window.ShowMessageAsync("Error", $"Failed to change password: {ex.Message}",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings { AnimateShow = true, AnimateHide = true });
            }
        }

        private bool ValidateInputs(MetroWindow window)
        {
            if (string.IsNullOrWhiteSpace(CurrentPassword))
            {
                window.ShowMessageAsync("Validation Error", "Please enter your current password.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                window.ShowMessageAsync("Validation Error", "Please enter a new password.");
                return false;
            }

            if (NewPassword.Length < 6)
            {
                window.ShowMessageAsync("Validation Error", "New password must be at least 6 characters long.");
                return false;
            }

            if (NewPassword != ConfirmPassword)
            {
                window.ShowMessageAsync("Validation Error", "New password and confirm password do not match.");
                return false;
            }

            if (CurrentPassword == NewPassword)
            {
                window.ShowMessageAsync("Validation Error", "New password must be different from current password.");
                return false;
            }

            return true;
        }

        private async Task<bool> ShowOfflineWarningAsync(MetroWindow window)
        {
            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Change Password Anyway",
                NegativeButtonText = "Cancel",
                AnimateShow = true,
                AnimateHide = true,
                DefaultButtonFocus = MessageDialogResult.Negative
            };

            var result = await window.ShowMessageAsync(
                "Offline Password Change",
                "You are currently offline.\n\n" +
                "If you change your password now, you will need to remember your OLD password " +
                "when you come back online to sync with Firebase.\n\n" +
                "Recommendation: Wait until you're online to change your password.\n\n" +
                "Do you want to proceed?",
                MessageDialogStyle.AffirmativeAndNegative,
                settings);

            return result == MessageDialogResult.Affirmative;
        }

        public void ClearForm()
        {
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
    }
}
