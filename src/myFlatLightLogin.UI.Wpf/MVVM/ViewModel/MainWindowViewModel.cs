using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using System.Windows;

// Enable if EventTrigger is to be used
//using System.Windows.Controls;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        public RelayCommand ShutdownWindowCommand { get; set; }
        public RelayCommand MoveWindowCommand { get; set; }
        public RelayCommand ResizeWindowCommand { get; set; }
        public RelayCommand NavigateToLoginCommand { get; set; }
        public RelayCommand NavigateToRegisterUserCommand { get; set; }

        public MainWindowViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;
            Navigation.NavigateTo<LoginViewModel>();

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

            NavigateToLoginCommand = new RelayCommand(o => { Navigation.NavigateTo<LoginViewModel>(); }, o => true);
        }
    }
}
