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
        public HomeViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;
        }
    }
}
