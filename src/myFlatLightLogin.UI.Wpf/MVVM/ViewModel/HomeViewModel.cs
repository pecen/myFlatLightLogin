using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;

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

        public RelayCommand NavigateToRoleManagementCommand { get; }

        public HomeViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;

            // Initialize commands
            NavigateToRoleManagementCommand = new RelayCommand(
                o => Navigation.NavigateTo<RoleManagementViewModel>(),
                o => true);

            // Subscribe to user changes to update welcome text
            CurrentUserService.Instance.OnUserChanged += OnUserChanged;

            // Set initial welcome text
            UpdateWelcomeText();
        }

        /// <summary>
        /// Updates the welcome text when the current user changes.
        /// </summary>
        private void OnUserChanged(object? sender, Dal.Dto.UserDto? user)
        {
            UpdateWelcomeText();
        }

        /// <summary>
        /// Updates the welcome text with the current user's name.
        /// </summary>
        private void UpdateWelcomeText()
        {
            var userName = CurrentUserService.Instance.GetDisplayName();
            WelcomeText = $"Welcome, {userName}!";
        }
    }
}
