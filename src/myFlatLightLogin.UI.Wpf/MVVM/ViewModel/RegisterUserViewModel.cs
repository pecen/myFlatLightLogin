using myFlatLightLogin.UI.Wpf.Core;
using myFlatLightLogin.UI.Wpf.Services;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public class RegisterUserViewModel : ViewModelBase
    {
        public RelayCommand NavigateToLoginCommand { get; set; }

        private bool _pwdIsEmpty = true;
        public bool PwdIsEmpty
        {
            get { return _pwdIsEmpty; }
            set { SetProperty(ref _pwdIsEmpty, value); }
        }

        public RegisterUserViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;

            NavigateToLoginCommand = new RelayCommand(o => { Navigation.NavigateTo<LoginViewModel>(); }, o => true);
        }
    }
}
