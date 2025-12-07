using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using myFlatLightLogin.UI.Common.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.UI.Common.Services;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the password sync dialog.
    /// Prompts user for old and new passwords to sync offline password change to Firebase.
    /// </summary>
    public class PasswordSyncDialogViewModel : ViewModelBase
    {
        public event EventHandler? OnDialogClosed;

        private readonly SyncService _syncService;
        private readonly UserDto _user;
        private readonly IDialogService _dialogService;
        private bool _dialogResult;

        #region Properties

        private string _oldPassword = string.Empty;
        public string OldPassword
        {
            get => _oldPassword;
            set => SetProperty(ref _oldPassword, value);
        }

        private string _newPassword = string.Empty;
        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string? UserEmail { get; }
        public bool DialogResult => _dialogResult;

        public AsyncRelayCommand SyncPasswordCommand { get; }
        public RelayCommand SkipCommand { get; }

        #endregion

        public PasswordSyncDialogViewModel(SyncService syncService, UserDto user, IDialogService dialogService)
        {
            _syncService = syncService;
            _user = user;
            _dialogService = dialogService;
            UserEmail = user.Email;

            SyncPasswordCommand = new AsyncRelayCommand(SyncPasswordAsync, CanSyncPassword);
            SkipCommand = new RelayCommand(o => SkipSync());
        }

        private bool CanSyncPassword()
        {
            return !string.IsNullOrWhiteSpace(OldPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   !IsProcessing;
        }

        private async Task SyncPasswordAsync()
        {
            // Validation
            if (NewPassword != ConfirmPassword)
            {
                await _dialogService.ShowMessageAsync("Validation Error", "New password and confirm password do not match.",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings { AnimateShow = true, AnimateHide = true });

                return;
            }

            IsProcessing = true;
            SyncPasswordCommand.RaiseCanExecuteChanged();

            try
            {
                // Verify old password hash matches
                var oldPasswordHash = myFlatLightLogin.Core.Utilities.SecurityHelper.HashPassword(OldPassword);
                if (oldPasswordHash != _user.OldPasswordHash)
                {
                    await _dialogService.ShowMessageAsync("Error",
                        "Old password is incorrect. Please enter the password you used BEFORE the change.",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings { AnimateShow = true, AnimateHide = true });
                    return;
                }

                // Verify new password hash matches what's stored in SQLite
                var newPasswordHash = myFlatLightLogin.Core.Utilities.SecurityHelper.HashPassword(NewPassword);
                var sqliteDal = new myFlatLightLogin.DalSQLite.UserDal();
                var currentUser = sqliteDal.Fetch(_user.Id);

                if (currentUser == null || currentUser.Password != newPasswordHash)
                {
                    await _dialogService.ShowMessageAsync("Error",
                        "New password is incorrect. Please enter the password you changed to while offline.",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings { AnimateShow = true, AnimateHide = true });
                    return;
                }

                // Sync password to Firebase
                bool success = await _syncService.SyncPasswordChangeAsync(_user, OldPassword, NewPassword);

                if (success)
                {
                    await _dialogService.ShowMessageAsync("Success", "Password synced to the Cloud successfully!",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings { AnimateShow = true, AnimateHide = true });

                    _dialogResult = true;
                    CloseDialog();
                }
                else
                {
                    await _dialogService.ShowMessageAsync("Error",
                        "Failed to sync password to Firebase. Please check your internet connection and try again.",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings { AnimateShow = true, AnimateHide = true });
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Error", $"Failed to sync password: {ex.Message}",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings { AnimateShow = true, AnimateHide = true });
            }
            finally
            {
                IsProcessing = false;
                SyncPasswordCommand.RaiseCanExecuteChanged();
            }
        }

        private void SkipSync()
        {
            _dialogResult = false;
            CloseDialog();
        }

        private void CloseDialog()
        {
            // Signal that dialog should close
            OnDialogClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
