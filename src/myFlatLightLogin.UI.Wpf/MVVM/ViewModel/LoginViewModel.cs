using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Dal;
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
        private readonly HybridUserDal _hybridDal;
        private readonly NetworkConnectivityService _connectivityService;

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

        public LoginViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;

            // Initialize network connectivity service
            _connectivityService = new NetworkConnectivityService();
            IsOnline = _connectivityService.IsOnline;

            // Listen for connectivity changes
            _connectivityService.ConnectivityChanged += OnConnectivityChanged;

            // Initialize sync service
            var syncService = new SyncService(_connectivityService);

            // Initialize Hybrid DAL (manages Firebase and SQLite)
            _hybridDal = new HybridUserDal(_connectivityService, syncService);

            // Initialize commands
            NavigateToRegisterUserCommand = new RelayCommand(
                o => Navigation.NavigateTo<RegisterUserViewModel>(),
                o => true);

            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Authenticates user with Firebase (online) or SQLite (offline).
        /// </summary>
        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true;

                // Check current connectivity status
                bool wasOnlineAtStart = _connectivityService.IsOnline;

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

                if (user != null)
                {
                    IsAuthenticated = true;

                    // Check connectivity AGAIN after authentication to get accurate status
                    bool isCurrentlyOnline = _connectivityService.IsOnline;
                    string loginMode = isCurrentlyOnline ? "online" : "offline";
                    StatusMessage = $"Welcome back, {user.Name ?? user.Email}! (Logged in {loginMode})";

                    // Show success message
                    MessageBox.Show(
                        $"Successfully logged in as {user.Email}\n\nMode: {loginMode.ToUpper()}",
                        "Login Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Navigate to main application or close login window
                    // TODO: Navigate to main window or implement your app's logic here
                }
                else
                {
                    IsAuthenticated = false;
                    StatusMessage = "Login failed. Please check your credentials.";

                    string message = wasOnlineAtStart
                        ? "Invalid email or password."
                        : "Invalid email or password, or user not found in offline cache.";

                    MessageBox.Show(
                        message,
                        "Login Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                IsAuthenticated = false;
                StatusMessage = $"Error: {ex.Message}";

                MessageBox.Show(
                    ex.Message,
                    "Login Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

        #endregion
    }
}
