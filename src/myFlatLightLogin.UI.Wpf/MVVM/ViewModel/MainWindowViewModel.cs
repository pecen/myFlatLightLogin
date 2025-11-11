using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Core.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public AsyncRelayCommand OpenLogsFolderCommand { get; set; }
        public AsyncRelayCommand ViewCurrentLogCommand { get; set; }

        /// <summary>
        /// Gets whether the current logged-in user has Admin role in the application.
        /// This is checked against application roles, not Windows administrator privileges.
        /// </summary>
        public bool IsUserAdministrator => CurrentUserService.Instance.IsAdmin;

        public MainWindowViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;
            Navigation.NavigateTo<LoginViewModel>();

            MoveWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.DragMove(); });

            // By using Application.Current.MainWindow.Close() instead of Application.Current.ShutDown()
            // we can utilize the extra MahApp popup that shows and asks if we really want to quit. 
            ShutdownWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.Close(); });

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

            // Admin-only commands for log access
            OpenLogsFolderCommand = new AsyncRelayCommand(OpenLogsFolderAsync, o => IsUserAdministrator);
            ViewCurrentLogCommand = new AsyncRelayCommand(ViewCurrentLogAsync, o => IsUserAdministrator);

            // Subscribe to user changes to update admin-only features visibility
            CurrentUserService.Instance.OnUserChanged += (sender, user) =>
            {
                // Notify UI that IsUserAdministrator may have changed
                OnPropertyChanged(nameof(IsUserAdministrator));

                // Update CanExecute for admin commands
                OpenLogsFolderCommand?.RaiseCanExecuteChanged();
                ViewCurrentLogCommand?.RaiseCanExecuteChanged();
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Opens the logs folder in Windows Explorer (admin only).
        /// </summary>
        private async Task OpenLogsFolderAsync()
        {
            var window = (MetroWindow)Application.Current.MainWindow;

            try
            {
                var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (Directory.Exists(logsPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = logsPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else
                {
                    await window.ShowMessageAsync("Logs Folder",
                        "Logs folder does not exist yet. No logs have been created.",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK",
                            AnimateShow = true,
                            AnimateHide = true
                        });
                }
            }
            catch (Exception ex)
            {
                await window.ShowMessageAsync("Error",
                    $"Failed to open logs folder: {ex.Message}",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "OK",
                        AnimateShow = true,
                        AnimateHide = true
                    });
            }
        }

        /// <summary>
        /// Opens the most recent log file in the default text editor (admin only).
        /// </summary>
        private async Task ViewCurrentLogAsync()
        {
            var window = (MetroWindow)Application.Current.MainWindow;

            try
            {
                var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logsPath))
                {
                    await window.ShowMessageAsync("View Logs",
                        "Logs folder does not exist yet. No logs have been created.",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK",
                            AnimateShow = true,
                            AnimateHide = true
                        });
                    return;
                }

                // Get the most recent log file
                var logFiles = Directory.GetFiles(logsPath, "myFlatLightLogin-*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                if (logFiles.Any())
                {
                    var latestLog = logFiles.First();
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = latestLog,
                        UseShellExecute = true
                    });
                }
                else
                {
                    await window.ShowMessageAsync("View Logs",
                        "No log files found.",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK",
                            AnimateShow = true,
                            AnimateHide = true
                        });
                }
            }
            catch (Exception ex)
            {
                await window.ShowMessageAsync("Error",
                    $"Failed to open log file: {ex.Message}",
                    MessageDialogStyle.Affirmative,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "OK",
                        AnimateShow = true,
                        AnimateHide = true
                    });
            }
        }

        #endregion
    }
}
