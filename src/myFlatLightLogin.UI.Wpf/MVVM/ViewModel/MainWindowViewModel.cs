using myFlatLightLogin.UI.Wpf.Core;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        public RelayCommand ShutdownWindowCommand { get; set; }
        public RelayCommand MoveWindowCommand { get; set; }
        public RelayCommand ResizeWindowCommand { get; set; }

        public MainWindowViewModel()
        {
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
        }
    }
}
