using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.DalFirebase;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the Register User view.
    /// Handles user registration using Firebase.
    /// </summary>
    public class RegisterUserViewModel : ViewModelBase, IAuthenticateConfirmUser
    {
        private readonly UserDal _userDal;

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

            // Initialize Firebase UserDal
            _userDal = new UserDal();

            // Initialize commands
            NavigateToLoginCommand = new RelayCommand(
                o => Navigation.NavigateTo<LoginViewModel>(),
                o => true);

            RegisterUserCommand = new AsyncRelayCommand(RegisterUserAsync, CanRegister);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Registers a new user with Firebase.
        /// </summary>
        private async Task RegisterUserAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Creating account...";

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

                // Create user DTO
                var newUser = new UserDto
                {
                    Name = Name,
                    Lastname = Lastname,
                    Email = Email,
                    Username = Email,
                    Password = Password
                };

                // Register with Firebase
                bool success = await Task.Run(() => _userDal.Insert(newUser));

                if (success)
                {
                    IsAuthenticated = true;
                    StatusMessage = "Account created successfully!";

                    MessageBox.Show(
                        $"Account created successfully!\n\nEmail: {Email}\n\nYou can now sign in.",
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

        #endregion
    }
}
