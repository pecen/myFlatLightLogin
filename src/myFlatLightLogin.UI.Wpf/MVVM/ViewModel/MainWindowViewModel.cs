using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using System;
using System.Windows;

// Enable if EventTrigger is to be used
//using System.Windows.Controls;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public class MainWindowViewModel : ViewModelBase, IAuthenticateUser
    {
        #region Commands

        public RelayCommand ShutdownWindowCommand { get; set; }
        public RelayCommand MoveWindowCommand { get; set; }
        public RelayCommand ResizeWindowCommand { get; set; }
        public RelayCommand NavigateToLoginCommand { get; set; }

        #endregion

        #region Properties

        private bool _isAuthenticated = false;
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set
            {
                SetProperty(ref _isAuthenticated, value);
            }
        }

        public bool PwdIsEmpty { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string UserId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Password { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion

        public MainWindowViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;
            Navigation.NavigateTo<LoginViewModel>(o => { var test = o; });

            MoveWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.DragMove(); });
            ShutdownWindowCommand = new RelayCommand(o => { Application.Current.Shutdown(); });
            ResizeWindowCommand = new RelayCommand(o =>
            {
                if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
                {
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                }
                else
                {
                    Application.Current.MainWindow.WindowState = WindowState.Maximized;
                }
            });

            NavigateToLoginCommand = new RelayCommand(Navigate, o => true);
        }

        private void Navigate(object obj)
        {
            Navigation.NavigateTo<LoginViewModel>(o =>
            {
                var test = o;
            });
        }
    }
}
