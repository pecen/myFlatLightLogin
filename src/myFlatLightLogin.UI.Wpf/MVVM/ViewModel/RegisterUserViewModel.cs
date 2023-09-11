using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public class RegisterUserViewModel : ViewModelBase, IAuthenticateConfirmUser
    {
        public RelayCommand NavigateToLoginCommand { get; set; }
        public RelayCommand RegisterUserCommand { get; set; }

        private bool _pwdIsEmpty = true;
        public bool PwdIsEmpty
        {
            get { return _pwdIsEmpty; }
            set { SetProperty(ref _pwdIsEmpty, value); }
        }

        private bool _confirmPwdIsEmpty = true;
        public bool ConfirmPwdIsEmpty
        {
            get { return _confirmPwdIsEmpty; }
            set { SetProperty(ref _confirmPwdIsEmpty, value); }
        }

        public bool IsAuthenticated => throw new System.NotImplementedException();

        public RegisterUserViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;

            NavigateToLoginCommand = new RelayCommand(o => { Navigation.NavigateTo<LoginViewModel>(); }, o => true);

            RegisterUserCommand = new RelayCommand(RegisterUser);
        }

        private void RegisterUser(object obj)
        {

        }
    }
}
