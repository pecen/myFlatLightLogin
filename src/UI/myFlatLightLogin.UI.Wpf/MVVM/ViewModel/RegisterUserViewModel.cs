using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using myFlatLightLogin.UI.Common.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.UI.Common.Services;
using myFlatLightLogin.Library;
using myFlatLightLogin.Dal;
using myFlatLightLogin.Dal.Dto;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the Register User view.
    /// Handles user registration with offline/online support using HybridUserDal.
    /// Uses BLL's UserEdit for validation only.
    /// </summary>
    public class RegisterUserViewModel : ViewModelBase, IAuthenticateConfirmUser
    {
        private static readonly ILogger _logger = Log.ForContext<RegisterUserViewModel>();
        private readonly NetworkConnectivityService _connectivityService;
        private readonly IDialogService _dialogService;
        private readonly HybridUserDal _hybridUserDal;

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
            set => SetProperty(ref _email, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
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

        public RegisterUserViewModel(INavigationService navigationService, IDialogService dialogService, NetworkConnectivityService connectivityService, HybridUserDal hybridUserDal)
        {
            Navigation = navigationService;
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Inject singleton services from DI container
            _connectivityService = connectivityService ?? throw new ArgumentNullException(nameof(connectivityService));
            _hybridUserDal = hybridUserDal ?? throw new ArgumentNullException(nameof(hybridUserDal));

            IsOnline = _connectivityService.IsOnline;

            // Listen for connectivity changes
            _connectivityService.ConnectivityChanged += OnConnectivityChanged;

            // Initialize commands
            NavigateToLoginCommand = new RelayCommand(
                o =>
                {
                    ClearForm();
                    Navigation.NavigateTo<LoginViewModel>();
                },
                o => true);

            RegisterUserCommand = new AsyncRelayCommand(RegisterUserAsync, CanRegister);

            // Clear form when view loads (in case returning from another view)
            ClearForm();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Registers a new user using HybridUserDal (with UserEdit validation).
        /// Handles both online (Firebase) and offline (SQLite) registration.
        /// </summary>
        private async Task RegisterUserAsync()
        {
            try
            {
                IsLoading = true;

                _logger.Information("========== REGISTRATION ATTEMPT STARTED ==========");
                _logger.Information("Email: {Email}", Email);

                if (IsOnline)
                {
                    StatusMessage = "Creating account with the Cloud storage...";
                }
                else
                {
                    StatusMessage = "Creating account offline (will sync when connected)...";
                }

                // Create a new UserEdit business object for validation
                var userEdit = await UserEdit.NewUserAsync();

                // Set properties from form
                userEdit.Name = Name;
                userEdit.LastName = Lastname;
                userEdit.UserName = Email;
                userEdit.Email = Email;
                userEdit.Password = Password;
                userEdit.ConfirmPassword = ConfirmPassword;

                // Validate using BLL business rules
                if (!userEdit.IsValid)
                {
                    var validationErrors = string.Join("\n", userEdit.BrokenRulesCollection);
                    _logger.Warning("Registration validation failed: {Errors}", validationErrors);

                    await _dialogService.ShowMessageAsync("Registration Error",
                       $"Please correct the following errors:\n\n{validationErrors}",
                       MessageDialogStyle.Affirmative,
                       new MetroDialogSettings
                       {
                           AffirmativeButtonText = "Continue",
                           AnimateShow = true,
                           AnimateHide = true
                       });

                    return;
                }

                // Create UserDto from validated data
                var userDto = new UserDto
                {
                    Name = Name,
                    Lastname = Lastname,
                    Email = Email,
                    Username = Email,
                    Password = Password,
                    Role = UserRole.User // Will be set to Admin if first user by HybridUserDal
                };

                // Register using HybridUserDal (handles both online and offline scenarios)
                var registrationResult = await _hybridUserDal.RegisterAsync(userDto);

                if (!registrationResult.Success)
                {
                    _logger.Warning("Registration failed: {Message}", registrationResult.Message);

                    await _dialogService.ShowMessageAsync("Registration Error",
                       registrationResult.Message,
                       MessageDialogStyle.Affirmative,
                       new MetroDialogSettings
                       {
                           AffirmativeButtonText = "Continue",
                           AnimateShow = true,
                           AnimateHide = true
                       });

                    return;
                }

                _logger.Information("Registration successful for: {Email}, Mode: {Mode}", Email, registrationResult.Mode);

                IsAuthenticated = true;
                StatusMessage = "Registration successful!";

                var mode = registrationResult.Mode;
                string title = mode == RegistrationMode.Firebase
                    ? "Registration Successful"
                    : "Registration Successful (Offline)";

                string message = $"Account created successfully!\n\nEmail: {Email}\n\nMode: {mode.ToString().ToUpper()}\n\nYou can now sign in with your credentials.";

                if (mode == RegistrationMode.SQLiteOffline)
                {
                    message += "\n\nNote: Your account will be synced to the cloud when connection is restored.";
                }

                await _dialogService.ShowMessageAsync(title,
                   message,
                   MessageDialogStyle.Affirmative,
                   new MetroDialogSettings
                   {
                       AffirmativeButtonText = "Continue",
                       AnimateShow = true,
                       AnimateHide = true
                   });

                // Clear the form before navigating back to login
                ClearForm();
                Navigation.NavigateTo<LoginViewModel>();
            }
            catch (Exception ex)
            {
                IsAuthenticated = false;
                _logger.Error(ex, "Registration failed with exception");
                StatusMessage = $"Error: {ex.Message}";

                await _dialogService.ShowMessageAsync("Registration Error",
                   ex.Message,
                   MessageDialogStyle.Affirmative,
                   new MetroDialogSettings
                   {
                       AffirmativeButtonText = "Continue",
                       AnimateShow = true,
                       AnimateHide = true
                   });
            }
            finally
            {
                IsLoading = false;
                _logger.Information("========== REGISTRATION ATTEMPT COMPLETED ==========");
            }
        }

        /// <summary>
        /// Determines whether the register command can execute.
        /// </summary>
        private bool CanRegister()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(Lastname) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   Password.Length >= 6;
        }

        /// <summary>
        /// Clears the registration form (all input fields).
        /// </summary>
        private void ClearForm()
        {
            Name = string.Empty;
            Lastname = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            StatusMessage = string.Empty;
            // Note: Password visibility is managed by TogglePwdBox control and auto-resets when cleared
        }

        /// <summary>
        /// Handles connectivity changes.
        /// </summary>
        private void OnConnectivityChanged(object? sender, bool isOnline)
        {
            IsOnline = isOnline;

            if (isOnline)
            {
                StatusMessage = "Connection restored! You can now register with the Cloud storage.";
            }
            else
            {
                StatusMessage = "Offline mode. You can still register (will sync later).";
            }
        }

        #endregion
    }
}
