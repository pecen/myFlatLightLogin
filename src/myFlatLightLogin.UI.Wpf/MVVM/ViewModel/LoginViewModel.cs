using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.UI.Wpf.MVVM.View;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the Login view.
    /// Handles user authentication with offline/online support using HybridUserDal.
    /// </summary>
    public class LoginViewModel : ViewModelBase, IAuthenticateUser
    {
        private static readonly ILogger _logger = Log.ForContext<LoginViewModel>();
        private readonly HybridUserDal _hybridDal;
        private readonly NetworkConnectivityService _connectivityService;
        private readonly SyncService _syncService;

        #region Properties

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set
            {
                SetProperty(ref _email, value);
                ((AsyncRelayCommand)LoginCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                PwdIsEmpty = string.IsNullOrEmpty(value);
                ((AsyncRelayCommand)LoginCommand)?.RaiseCanExecuteChanged();
            }
        }

        private bool _pwdIsEmpty = true;
        public bool PwdIsEmpty
        {
            get => _pwdIsEmpty;
            set => SetProperty(ref _pwdIsEmpty, value);
        }

        private bool _isPasswordVisible;
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                ((AsyncRelayCommand)LoginCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set
            {
                SetProperty(ref _isOnline, value);
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }

        public string ConnectionStatus => IsOnline ? "🟢 Online" : "🔴 Offline";

        public bool IsAuthenticated { get; private set; }

        #endregion

        #region Commands

        public RelayCommand NavigateToRegisterUserCommand { get; set; }
        public AsyncRelayCommand LoginCommand { get; set; }
        public RelayCommand TogglePasswordVisibilityCommand { get; set; }

        #endregion

        #region Constructor

        public LoginViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;

            // Initialize network connectivity service
            _connectivityService = new NetworkConnectivityService();
            IsOnline = _connectivityService.IsOnline;

            // Listen for connectivity changes
            _connectivityService.ConnectivityChanged += OnConnectivityChanged;

            // Initialize sync service
            _syncService = new SyncService(_connectivityService);

            // Initialize Hybrid DAL (manages Firebase and SQLite)
            _hybridDal = new HybridUserDal(_connectivityService, _syncService);

            // Initialize commands
            NavigateToRegisterUserCommand = new RelayCommand(
                o =>
                {
                    ClearForm();
                    Navigation.NavigateTo<RegisterUserViewModel>();
                },
                o => true);

            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);

            TogglePasswordVisibilityCommand = new RelayCommand(
                o => IsPasswordVisible = !IsPasswordVisible,
                o => true);

            // Clear form when view loads (in case returning from another view)
            ClearForm();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Authenticates user with Firebase (online) or SQLite (offline).
        /// </summary>
        private async Task LoginAsync()
        {
            var window = (MetroWindow)Application.Current.MainWindow;

            try
            {
                IsLoading = true;

                _logger.Information("========== LOGIN ATTEMPT STARTED ==========");
                _logger.Information("Email: {Email}", Email);

                // Check FRESH connectivity status (don't trust cached value)
                bool wasOnlineAtStart = _connectivityService.CheckConnectivity();
                _logger.Information("Fresh connectivity check: {IsOnline}", wasOnlineAtStart);
                _logger.Debug("Cached IsOnline property: {CachedIsOnline}", _connectivityService.IsOnline);

                if (wasOnlineAtStart)
                {
                    StatusMessage = "Signing in with Firebase...";
                }
                else
                {
                    StatusMessage = "Signing in offline...";
                }

                // Authenticate using HybridDAL (tries Firebase first, falls back to SQLite)
                var user = await _hybridDal.SignInAsync(Email, Password);

                _logger.Information("Authentication result: {Result}", user != null ? "SUCCESS" : "FAILED");

                if (user != null)
                {
                    _logger.Information("User authenticated - Email: {Email}, Name: {Name}, Username: {Username}, Role: {Role}",
                        user.Email, user.Name, user.Username, user.Role);

                    IsAuthenticated = true;

                    // Set the current user in the application-wide service
                    CurrentUserService.Instance.SetCurrentUser(user);
                    _logger.Information("Current user set in CurrentUserService with role: {Role}", user.Role);

                    // Check connectivity AGAIN with fresh check (don't trust cached value)
                    bool isCurrentlyOnline = _connectivityService.CheckConnectivity();
                    _logger.Information("Fresh connectivity check after auth: {IsOnline}", isCurrentlyOnline);

                    string loginMode = isCurrentlyOnline ? "online" : "offline";
                    string displayName = user.Name ?? user.Email ?? "Unknown User";
                    StatusMessage = $"Welcome back, {displayName}! (Logged in {loginMode})";

                    _logger.Information("Display name: {DisplayName}, Login mode: {LoginMode}", displayName, loginMode);

                    // Show success message
                    //MessageBox.Show(
                    //    $"Successfully logged in as {user.Email ?? "Unknown"}\n\nMode: {loginMode.ToUpper()}",
                    //    "Login Successful",
                    //    MessageBoxButton.OK,
                    //    MessageBoxImage.Information);

                    await window.ShowMessageAsync("Login Successful",
                        $"Successfully logged in as {user.Email ?? "Unknown"}\n\nMode: {loginMode.ToUpper()}",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "Continue",
                            AnimateShow = true,
                            AnimateHide = true
                        });

                    _logger.Information("========== LOGIN ATTEMPT COMPLETED SUCCESSFULLY ==========");

                    // Check for pending password changes (offline password change that needs sync)
                    if (user.PendingPasswordChange && _connectivityService.IsOnline)
                    {
                        _logger.Information("Pending password change detected for user: {Email}. Showing password sync dialog.", user.Email);
                        await ShowPasswordSyncDialogAsync(user, window);
                    }

                    // Navigate to Home view after successful login
                    Navigation.NavigateTo<HomeViewModel>();
                }
                else
                {
                    IsAuthenticated = false;
                    StatusMessage = "Login failed. Please check your credentials.";

                    string message = wasOnlineAtStart
                        ? "Invalid email or password."
                        : "Invalid email or password, or user not found in offline cache.";

                    //MessageBox.Show(
                    //    message,
                    //    "Login Failed",
                    //    MessageBoxButton.OK,
                    //    MessageBoxImage.Warning);

                    await window.ShowMessageAsync("Login failed",
                        message,
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "Continue",
                            AnimateShow = true,
                            AnimateHide = true
                        });

                    _logger.Warning("========== LOGIN ATTEMPT FAILED ==========");
                }
            }
            catch (Exception ex)
            {
                IsAuthenticated = false;

#if DEBUG
                // DEBUG MODE: Show detailed error for development
                StatusMessage = $"Error: {ex.Message}";
                _logger.Error(ex, "Login failed with exception");

                //MessageBox.Show(
                //    $"Login Error: {ex.Message}",
                //    "Login Error",
                //    MessageBoxButton.OK,
                //    MessageBoxImage.Error);

                await window.ShowMessageAsync("Login Error",
                    $"Login Error: {ex.Message}",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "Continue",
                        AnimateShow = true,
                        AnimateHide = true
                    });
#else
                // RELEASE MODE: Generic error (no technical details or passwords)
                StatusMessage = "Login error occurred";
                _logger.Error("Login failed with exception: {ErrorType}", ex.GetType().Name);

                //MessageBox.Show(
                //    "An error occurred during login. Please check your credentials and try again.",
                //    "Login Error",
                //    MessageBoxButton.OK,
                //    MessageBoxImage.Error);

                await window.ShowMessageAsync("Login Error",
                    "An error occurred during login. Please check your credentials and try again.",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "Continue",
                        AnimateShow = true,
                        AnimateHide = true
                    });
#endif

                _logger.Error("========== LOGIN ATTEMPT FAILED WITH EXCEPTION ==========");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Determines whether the login command can execute.
        /// </summary>
        private bool CanLogin()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Password.Length >= 6;
        }

        /// <summary>
        /// Clears the login form (email and password fields).
        /// </summary>
        public void ClearForm()
        {
            Email = string.Empty;
            Password = string.Empty;
            IsPasswordVisible = false;
            StatusMessage = string.Empty;
        }

        /// <summary>
        /// Handles connectivity changes.
        /// </summary>
        private void OnConnectivityChanged(object sender, bool isOnline)
        {
            IsOnline = isOnline;

            if (isOnline)
            {
                StatusMessage = "Connection restored! You can now sign in with Firebase.";
            }
            else
            {
                StatusMessage = "Offline mode. You can still sign in with cached credentials.";
            }
        }

        /// <summary>
        /// Shows the password sync dialog to sync an offline password change to Firebase.
        /// </summary>
        private async Task ShowPasswordSyncDialogAsync(UserDto user, MetroWindow window)
        {
            try
            {
                _logger.Information("Showing password sync dialog for user: {Email}", user.Email);

                // Create the dialog view and ViewModel
                var dialogView = new myFlatLightLogin.UI.Wpf.MVVM.View.PasswordSyncDialog();
                var dialogViewModel = new PasswordSyncDialogViewModel(
                    _syncService,
                    user,
                    window);

                dialogView.DataContext = dialogViewModel;

                // Create a task completion source to wait for dialog closure
                var tcs = new TaskCompletionSource<bool>();
                dialogViewModel.OnDialogClosed += (sender, e) => tcs.TrySetResult(dialogViewModel.DialogResult);

                // Show the dialog as a Metro dialog
                await window.ShowMetroDialogAsync(new MahApps.Metro.Controls.Dialogs.CustomDialog
                {
                    Content = dialogView
                });

                // Wait for the dialog to close
                bool result = await tcs.Task;

                // Hide the dialog
                await window.HideMetroDialogAsync(await window.GetCurrentDialogAsync<MahApps.Metro.Controls.Dialogs.BaseMetroDialog>());

                _logger.Information("Password sync dialog closed. Result: {Result}", result);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error showing password sync dialog");
            }
        }

        #endregion
    }
}
