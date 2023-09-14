using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using System;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public class LoginViewModel : ViewModelBase, IAuthenticateUser
    {
        // Enable if EventTrigger is to be used
        //public RelayCommand<object> SetPwdStatusCommand { get; set; }

        public RelayCommand NavigateToRegisterUserCommand { get; set; }
        public RelayCommand LoginCommand { get; set; }

        public string UserId { get; set; }
        public string Password { get; set; }

        private bool _pwdIsEmpty = true;
        public bool PwdIsEmpty
        {
            get { return _pwdIsEmpty; }
            set { SetProperty(ref _pwdIsEmpty, value); }
        }

        public bool IsAuthenticated => throw new NotImplementedException();

        public LoginViewModel(INavigationService navigationService)
        {
            // Enable if EventTrigger is to be used
            //SetPwdStatusCommand = new RelayCommand<object>(SetStatus);

            Navigation = navigationService;

            NavigateToRegisterUserCommand = new RelayCommand(o => { Navigation.NavigateTo<RegisterUserViewModel>(o => { var test = o; }); }, o => true);
            LoginCommand = new RelayCommand(Login);
        }

        private void Login(object obj)
        {

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
