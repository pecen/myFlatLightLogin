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

        // Enable if EventTrigger is to be used
        //public RelayCommand<object> SetPwdStatusCommand { get; set; }

        private bool _pwdIsEmpty = true;
        public bool PwdIsEmpty
        {
            get { return _pwdIsEmpty; }
            set { SetProperty(ref _pwdIsEmpty, value); }
        }
        public string Password { get; set; }
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

            // Enable if EventTrigger is to be used
            //SetPwdStatusCommand = new RelayCommand<object>(SetStatus);
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
