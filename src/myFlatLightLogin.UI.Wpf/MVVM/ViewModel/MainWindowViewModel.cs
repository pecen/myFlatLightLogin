using myFlatLightLogin.UI.Wpf.Core;
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
        public RelayCommand ShowLoginView {  get; set; }

        // The ViewModels
        public LoginViewModel LoginVM { get; set; }

        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set { SetProperty(ref _currentView, value); }
        }

        public MainWindowViewModel()
        {
            LoginVM = new LoginViewModel();

            _currentView =  LoginVM;

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

            ShowLoginView = new RelayCommand(o => CurrentView = LoginVM); 
        }
    }
}
