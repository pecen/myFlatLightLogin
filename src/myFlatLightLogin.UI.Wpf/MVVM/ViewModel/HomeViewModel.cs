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

        public HomeViewModel(INavigationService navigationService)
        {
            WelcomeText = $"Welcome, {CurrentUserService.Instance.CurrentUser?.Name}!";
            Navigation = navigationService;
        }
    }
}
