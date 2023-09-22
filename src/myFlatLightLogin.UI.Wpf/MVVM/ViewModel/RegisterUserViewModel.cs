using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using System;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public class RegisterUserViewModel : ViewModelBase, IAuthenticateConfirmPwd
    {
        public RelayCommand NavigateToLoginCommand { get; set; }
        public RelayCommand RegisterUserCommand { get; set; }

        public string UserId { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        public string IsEnabled
        {
            get 
            { 
                var color = CanRegister(null) ? "#ffffff" : "#808080"; 
                OnPropertyChanged(color);
                return color;
            }
        }


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

        bool IAuthenticateUser.IsAuthenticated { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public RegisterUserViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;

            NavigateToLoginCommand = new RelayCommand(o => { Navigation.NavigateTo<LoginViewModel>(o => { var test = o; }); }, o => true);

            RegisterUserCommand = new RelayCommand(RegisterUser, CanRegister);
        }

        private bool CanRegister(object arg)
        {
            return (!string.IsNullOrEmpty(ConfirmPassword) && ConfirmPassword == Password);
        }

        private void Navigate(object obj)
        {
            Navigation.NavigateTo<LoginViewModel>();
        }

        private void RegisterUser(object obj)
        {
            //if(ConfirmPassword != Password)
            //{
            //    MessageBox.Show("Passwords do not match!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning); 
            //    return;
            //}

            //MessageBox.Show("User registered", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
