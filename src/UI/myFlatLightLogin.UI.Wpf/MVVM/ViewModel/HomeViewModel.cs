using myFlatLightLogin.UI.Common.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Dal.Dto;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for the Home view.
    /// Displayed after successful login.
    /// </summary>
    public class HomeViewModel : ViewModelBase
    {
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

        public HomeViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;

            // Initialize commands
            NavigateToRoleManagementCommand = new RelayCommand(
                o => Navigation.NavigateTo<RoleManagementViewModel>(),
                o => IsUserAdmin);

            NavigateToChangePasswordCommand = new RelayCommand(
                o => Navigation.NavigateTo<ChangePasswordViewModel>());

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
    }
}
