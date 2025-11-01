using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Dal;
using myFlatLightLogin.DalFirebase;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the Login view.
    /// Handles user authentication using Firebase.
    /// </summary>
    public class LoginViewModel : ViewModelBase, IAuthenticateUser
    {
        private readonly UserDal _userDal;

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

            // Initialize Firebase UserDal
            _userDal = new UserDal();

            // Initialize commands
            NavigateToRegisterUserCommand = new RelayCommand(
                o => Navigation.NavigateTo<RegisterUserViewModel>(),
                o => true);

            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Authenticates user with Firebase.
        /// </summary>
        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Signing in...";

                // Authenticate with Firebase
                var user = await _userDal.SignInAsync(Email, Password);

                if (user != null)
                {
                    IsAuthenticated = true;
                    StatusMessage = $"Welcome back, {user.Name ?? Email}!";

                    // Show success message
                    MessageBox.Show(
                        $"Successfully logged in as {user.Email}",
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

                    MessageBox.Show(
                        "Invalid email or password.",
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

        #endregion
    }
}
