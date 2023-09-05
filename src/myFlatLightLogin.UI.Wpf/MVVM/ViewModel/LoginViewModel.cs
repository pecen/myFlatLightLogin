using myFlatLightLogin.UI.Wpf.Core;
using myFlatLightLogin.UI.Wpf.Services;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        // Enable if EventTrigger is to be used
        //public RelayCommand<object> SetPwdStatusCommand { get; set; }

        public RelayCommand NavigateToRegisterUserCommand { get; set; }

        private bool _pwdIsEmpty = true;
        public bool PwdIsEmpty
        {
            get { return _pwdIsEmpty; }
            set { SetProperty(ref _pwdIsEmpty, value); }
        }

        public LoginViewModel(INavigationService navigationService)
        {
            // Enable if EventTrigger is to be used
            //SetPwdStatusCommand = new RelayCommand<object>(SetStatus);

            Navigation = navigationService;

            NavigateToRegisterUserCommand = new RelayCommand(o => { Navigation.NavigateTo<RegisterUserViewModel>(); }, o => true);
        }

        // Used if EventTrigger is used
        //private void SetStatus(object pwdBox)
        //{
        //    if (pwdBox is PasswordBox pwd)
        //    {
        //        if (pwd.SecurePassword.Length > 0)
        //        {
        //            PwdIsEmpty = false;
        //        }
        //        else
        //        {
        //            PwdIsEmpty = true;
        //        }
        //    }
        //}
    }
}
