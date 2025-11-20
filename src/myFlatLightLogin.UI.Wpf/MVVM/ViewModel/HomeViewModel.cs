using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Dal.Dto;
using Serilog;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the Home view.
    /// Displayed after successful login.
    /// </summary>
    public class HomeViewModel : ViewModelBase
    {
        private static readonly ILogger _logger = Log.ForContext<HomeViewModel>();
        private readonly HybridUserDal _hybridUserDal;

        private string welcomeText = string.Empty;
        public string WelcomeText
        {
            get => welcomeText;
            private set => SetProperty(ref welcomeText, value);
        }

        private bool _isUserAdmin;
        public bool IsUserAdmin
        {
            get => _isUserAdmin;
            private set => SetProperty(ref _isUserAdmin, value);
        }

        public RelayCommand NavigateToRoleManagementCommand { get; }
        public RelayCommand NavigateToChangePasswordCommand { get; }
        public RelayCommand LogoutCommand { get; }

        public HomeViewModel(INavigationService navigationService, HybridUserDal hybridUserDal)
        {
            Navigation = navigationService;
            _hybridUserDal = hybridUserDal;

            // Initialize commands
            NavigateToRoleManagementCommand = new RelayCommand(
                o => Navigation.NavigateTo<RoleManagementViewModel>(),
                o => IsUserAdmin);

            NavigateToChangePasswordCommand = new RelayCommand(
                o => Navigation.NavigateTo<ChangePasswordViewModel>());

            LogoutCommand = new RelayCommand(o => Logout());

            // Subscribe to user changes to update welcome text
            CurrentUserService.Instance.OnUserChanged += OnUserChanged;

            // Set initial welcome text and role check
            UpdateWelcomeText();
            UpdateUserRole();
        }

        /// <summary>
        /// Updates the welcome text when the current user changes.
        /// </summary>
        private void OnUserChanged(object? sender, Dal.Dto.UserDto? user)
        {
            UpdateWelcomeText();
            UpdateUserRole();
        }

        /// <summary>
        /// Updates the welcome text with the current user's name.
        /// </summary>
        private void UpdateWelcomeText()
        {
            var userName = CurrentUserService.Instance.GetDisplayName();
            WelcomeText = $"Welcome, {userName}!";
        }

        /// <summary>
        /// Updates the user role status (checks if user is admin).
        /// </summary>
        private void UpdateUserRole()
        {
            var currentUser = CurrentUserService.Instance.CurrentUser;
            IsUserAdmin = currentUser?.Role == UserRole.Admin;
        }

        /// <summary>
        /// Logs out the current user and navigates to the login screen.
        /// </summary>
        private void Logout()
        {
            var currentUser = CurrentUserService.Instance.CurrentUser;
            _logger.Information("User logging out: {Email}", currentUser?.Email ?? "Unknown");

            // Sign out from Firebase if online
            _hybridUserDal.SignOut();

            // Clear current user
            CurrentUserService.Instance.ClearCurrentUser();

            _logger.Information("User logged out successfully");

            // Navigate to login screen
            Navigation.NavigateTo<LoginViewModel>();
        }
    }
}
