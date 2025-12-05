using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using myFlatLightLogin.UI.Common.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.UI.Common.Services;
using myFlatLightLogin.Library.Security;
using myFlatLightLogin.UI.Wpf.MVVM.View;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the Login view.
    /// Handles user authentication with offline/online support using BLL's UserPrincipal.
    /// </summary>
    public class LoginViewModel : ViewModelBase, IAuthenticateUser
    {
        private static readonly ILogger _logger = Log.ForContext<LoginViewModel>();
        private readonly NetworkConnectivityService _connectivityService;
        private readonly SyncService _syncService;
        private readonly IDialogService _dialogService;
        private UserPrincipal? _currentPrincipal;

        #region Properties

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
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

        #endregion

        #region Constructor

        public LoginViewModel(INavigationService navigationService, IDialogService dialogService)
        {
            Navigation = navigationService;
            _dialogService = dialogService;

            // Initialize network connectivity service
            _connectivityService = new NetworkConnectivityService();
            IsOnline = _connectivityService.IsOnline;

            // Listen for connectivity changes
            _connectivityService.ConnectivityChanged += OnConnectivityChanged;

            // Initialize sync service
            _syncService = new SyncService(_connectivityService);

            // Initialize commands
            NavigateToRegisterUserCommand = new RelayCommand(
                o =>
                {
                    ClearForm();
                    Navigation.NavigateTo<RegisterUserViewModel>();
                },
                o => true);

            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);

            // Clear form when view loads (in case returning from another view)
            ClearForm();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Authenticates user with Firebase (online) or SQLite (offline) using BLL.
        /// </summary>
        private async Task LoginAsync()
        {
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
                    StatusMessage = "Signing in with the Cloud storage...";
                }
                else
                {
                    StatusMessage = "Signing in offline...";
                }

                // Authenticate using BLL UserPrincipal (handles Firebase and SQLite through HybridUserDal)
                _currentPrincipal = await UserPrincipal.LoginAsync(Email, Password, _connectivityService, _syncService);

                _logger.Information("Authentication result: {Result}",
                    _currentPrincipal?.Identity?.IsAuthenticated == true ? "SUCCESS" : "FAILED");

                if (_currentPrincipal?.Identity?.IsAuthenticated == true)
                {
                    var identity = _currentPrincipal.Identity;

                    _logger.Information("User authenticated - Email: {Email}, Name: {Name}, Role: {Role}",
                        identity.Email, identity.FirstName, identity.Role);

                    IsAuthenticated = true;

                    // Set the current user info in the application-wide service
                    var currentUserInfo = new CurrentUserInfo
                    {
                        UserId = identity.UserId,
                        FirstName = identity.FirstName,
                        Email = identity.Email,
                        Role = identity.Role,
                        IsOnline = identity.IsOnline,
                        FirebaseAuthToken = identity.FirebaseAuthToken
                    };
                    CurrentUserService.Instance.SetCurrentUserInfo(currentUserInfo);
                    _logger.Information("Current user info set in CurrentUserService with role: {Role}", identity.Role);

                    // Check connectivity AGAIN with fresh check (don't trust cached value)
                    bool isCurrentlyOnline = _connectivityService.CheckConnectivity();
                    _logger.Information("Fresh connectivity check after auth: {IsOnline}", isCurrentlyOnline);

                    string loginMode = identity.IsOnline ? "online" : "offline";
                    string displayName = identity.FirstName ?? identity.Email ?? "Unknown User";
                    StatusMessage = $"Welcome back, {displayName}! (Logged in {loginMode})";

                    _logger.Information("Display name: {DisplayName}, Login mode: {LoginMode}", displayName, loginMode);

                    await _dialogService.ShowMessageAsync("Login Successful",
                        $"Successfully logged in as {identity.Email ?? "Unknown"}\n\nMode: {loginMode.ToUpper()}",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "Continue",
                            AnimateShow = true,
                            AnimateHide = true
                        });

                    _logger.Information("========== LOGIN ATTEMPT COMPLETED SUCCESSFULLY ==========");

                    // Check for pending password changes (offline password change that needs sync)
                    // Note: This functionality will need to be refactored to work with BLL
                    // For now, we'll skip it as it requires PasswordSyncDialogViewModel refactoring

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

                    await _dialogService.ShowMessageAsync("Login failed",
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
            catch (System.Security.SecurityException secEx)
            {
                IsAuthenticated = false;
                StatusMessage = "Login failed. Please check your credentials.";
                _logger.Warning(secEx, "Authentication failed - invalid credentials");

                await _dialogService.ShowMessageAsync("Login failed",
                    "Invalid email or password.",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "Continue",
                        AnimateShow = true,
                        AnimateHide = true
                    });
            }
            catch (Exception ex)
            {
                IsAuthenticated = false;

                // Extract user-friendly error message from CSLA DataPortalException
                string errorMessage = ex.Message;

                // Strip CSLA's "DataPortal.Fetch failed" wrapper to get the actual error
                if (errorMessage.StartsWith("DataPortal.Fetch failed (") && errorMessage.EndsWith(")"))
                {
                    // Extract the inner message: "DataPortal.Fetch failed (Invalid email or password)" -> "Invalid email or password"
                    errorMessage = errorMessage.Substring("DataPortal.Fetch failed (".Length);
                    errorMessage = errorMessage.Substring(0, errorMessage.Length - 1);
                }

#if DEBUG
                // DEBUG MODE: Show detailed error for development
                StatusMessage = $"Error: {errorMessage}";
                _logger.Error(ex, "Login failed with exception");

                await _dialogService.ShowMessageAsync("Login Error",
                    errorMessage,
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "OK",
                        AnimateShow = true,
                        AnimateHide = true
                    });
#else
                // RELEASE MODE: Show user-friendly error or generic fallback
                StatusMessage = errorMessage;
                _logger.Error("Login failed with exception: {ErrorType}", ex.GetType().Name);

                // Check if it's an authentication error (invalid credentials)
                if (errorMessage.Contains("Invalid email or password", StringComparison.OrdinalIgnoreCase) ||
                    errorMessage.Contains("invalid credentials", StringComparison.OrdinalIgnoreCase))
                {
                    await _dialogService.ShowMessageAsync("Login Failed",
                        "Invalid email or password. Please check your credentials and try again.",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK",
                            AnimateShow = true,
                            AnimateHide = true
                        });
                }
                else
                {
                    // Generic error for other failures
                    await _dialogService.ShowMessageAsync("Login Error",
                        "An error occurred during login. Please try again.",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK",
                            AnimateShow = true,
                            AnimateHide = true
                        });
                }
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
            StatusMessage = string.Empty;
            // Note: Password visibility is managed by TogglePwdBox control and auto-resets when cleared
        }

        /// <summary>
        /// Handles connectivity changes.
        /// </summary>
        private void OnConnectivityChanged(object sender, bool isOnline)
        {
            IsOnline = isOnline;

            if (isOnline)
            {
                StatusMessage = "Connection restored! You can now sign in with the Cloud storage.";
            }
            else
            {
                StatusMessage = "Offline mode. You can still sign in with cached credentials.";
            }
        }

        // TODO: Refactor password sync dialog to work with BLL
        // This method has been temporarily removed and will be reimplemented
        // to work with the BLL's UserIdentity and PasswordEdit classes

        #endregion
    }
}
