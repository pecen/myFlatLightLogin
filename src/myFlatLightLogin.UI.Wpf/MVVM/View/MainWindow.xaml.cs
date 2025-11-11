using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace myFlatLightLogin.UI.Wpf.MVVM.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private bool _shutdown;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (e.Cancel)
            {
                return;
            }

            if (_shutdown == false)
            {
                e.Cancel = true;

                // We have to delay the execution through BeginInvoke to prevent potential re-entrancy
                Dispatcher.BeginInvoke(new Action(async () => await ConfirmShutdown()));
            }
        }

        private async Task ConfirmShutdown()
        {
            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Quit",
                NegativeButtonText = "Cancel",
                AnimateShow = true,
                AnimateHide = false
            };

            var result = await this.ShowMessageAsync("Quit application?",
                                                     "Sure you want to quit application?",
                                                     MessageDialogStyle.AffirmativeAndNegative,
                                                     mySettings);

            _shutdown = result == MessageDialogResult.Affirmative;

            if (_shutdown)
            {
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Closes the window without showing the shutdown confirmation dialog.
        /// Use this when closing after a successful operation like login.
        /// </summary>
        public void CloseWithoutConfirmation()
        {
            _shutdown = true;
            Close();
        }
    }
}
