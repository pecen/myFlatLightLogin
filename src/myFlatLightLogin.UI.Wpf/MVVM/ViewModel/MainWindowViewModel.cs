using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Core.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public RelayCommand OpenLogsFolderCommand { get; set; }
        public RelayCommand ViewCurrentLogCommand { get; set; }

        /// <summary>
        /// Gets whether the current user is a Windows administrator
        /// </summary>
        public bool IsUserAdministrator => SecurityHelper.IsUserAdministrator();

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
            OpenLogsFolderCommand = new RelayCommand(o =>
            {
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
                        MessageBox.Show("Logs folder does not exist yet. No logs have been created.",
                            "Logs Folder", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open logs folder: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, o => IsUserAdministrator);

            ViewCurrentLogCommand = new RelayCommand(o =>
            {
                try
                {
                    var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    if (!Directory.Exists(logsPath))
                    {
                        MessageBox.Show("Logs folder does not exist yet. No logs have been created.",
                            "View Logs", MessageBoxButton.OK, MessageBoxImage.Information);
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
                        MessageBox.Show("No log files found.",
                            "View Logs", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open log file: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, o => IsUserAdministrator);
        }
    }
}
