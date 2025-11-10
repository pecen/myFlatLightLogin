using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Dal.Dto;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the Register User view.
    /// Handles user registration with offline/online support using HybridUserDal.
    /// </summary>
    public class RegisterUserViewModel : ViewModelBase, IAuthenticateConfirmUser
    {
        private readonly HybridUserDal _hybridDal;
        private readonly NetworkConnectivityService _connectivityService;

        #region Properties

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _lastname = string.Empty;
        public string Lastname
        {
            get => _lastname;
            set => SetProperty(ref _lastname, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set
            {
                SetProperty(ref _email, value);
                ((AsyncRelayCommand)RegisterUserCommand)?.RaiseCanExecuteChanged();
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
                ((AsyncRelayCommand)RegisterUserCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                SetProperty(ref _confirmPassword, value);
                ConfirmPwdIsEmpty = string.IsNullOrEmpty(value);
                ((AsyncRelayCommand)RegisterUserCommand)?.RaiseCanExecuteChanged();
            }
        }

        private bool _pwdIsEmpty = true;
        public bool PwdIsEmpty
        {
            get => _pwdIsEmpty;
            set => SetProperty(ref _pwdIsEmpty, value);
        }

        private bool _confirmPwdIsEmpty = true;
        public bool ConfirmPwdIsEmpty
        {
            get => _confirmPwdIsEmpty;
            set => SetProperty(ref _confirmPwdIsEmpty, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                ((AsyncRelayCommand)RegisterUserCommand)?.RaiseCanExecuteChanged();
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

        public RelayCommand NavigateToLoginCommand { get; set; }
        public AsyncRelayCommand RegisterUserCommand { get; set; }

        #endregion

        #region Constructor

        public RegisterUserViewModel(INavigationService navigationService)
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
            NavigateToLoginCommand = new RelayCommand(
                o => Navigation.NavigateTo<LoginViewModel>(),
                o => true);

            RegisterUserCommand = new AsyncRelayCommand(RegisterUserAsync, CanRegister);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Registers a new user with Firebase (online) or SQLite (offline).
        /// </summary>
        private async Task RegisterUserAsync()
        {
            try
            {
                IsLoading = true;

                // Validate passwords match
                if (Password != ConfirmPassword)
                {
                    MessageBox.Show(
                        "Passwords do not match. Please try again.",
                        "Registration Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (IsOnline)
                {
                    StatusMessage = "Creating account with Firebase...";
                }
                else
                {
                    StatusMessage = "Creating account offline (will sync when connected)...";
                }

                // Create user DTO
                var newUser = new UserDto
                {
                    Name = Name,
                    Lastname = Lastname,
                    Email = Email,
                    Username = Email,
                    Password = Password
                };

                // Register using HybridDAL (tries Firebase first, falls back to SQLite)
                bool success = await _hybridDal.RegisterAsync(newUser);

                if (success)
                {
                    IsAuthenticated = true;

                    string registrationMode = IsOnline ? "online" : "offline";
                    StatusMessage = $"Account created successfully! (Registered {registrationMode})";

                    string message = IsOnline
                        ? $"Account created successfully!\n\nEmail: {Email}\n\nYou can now sign in."
                        : $"Account created offline!\n\nEmail: {Email}\n\nYour account will be synced to Firebase when you're back online.\nYou can sign in now using offline mode.";

                    MessageBox.Show(
                        message,
                        "Registration Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Navigate back to login
                    Navigation.NavigateTo<LoginViewModel>();
                }
                else
                {
                    IsAuthenticated = false;
                    StatusMessage = "Registration failed. Please try again.";

                    MessageBox.Show(
                        "Registration failed. Please try again.",
                        "Registration Failed",
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
                    "Registration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Determines whether the register command can execute.
        /// </summary>
        private bool CanRegister()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
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
                StatusMessage = "Connection restored! You can now register with Firebase.";
            }
            else
            {
                StatusMessage = "Offline mode. You can still register (will sync later).";
            }
        }

        #endregion
    }
}
