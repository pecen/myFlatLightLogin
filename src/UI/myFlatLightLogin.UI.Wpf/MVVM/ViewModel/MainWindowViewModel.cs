using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using myFlatLightLogin.UI.Common.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.UI.Common.Services;
using myFlatLightLogin.Core.Utilities;
using Serilog;
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
        private static readonly ILogger _logger = Log.ForContext<MainWindowViewModel>();
        private readonly LoginViewModel _loginViewModel;
        private readonly HybridUserDal _hybridUserDal;
        private readonly SyncService _syncService;
        private readonly NetworkConnectivityService _connectivityService;

        public RelayCommand ShutdownWindowCommand { get; set; }
        public RelayCommand MoveWindowCommand { get; set; }
        public RelayCommand ResizeWindowCommand { get; set; }
        public RelayCommand NavigateToLoginCommand { get; set; }
        public RelayCommand LogoutCommand { get; set; }
        public RelayCommand LoginLogoutCommand { get; set; }
        public RelayCommand NavigateToRegisterUserCommand { get; set; }
        public AsyncRelayCommand OpenLogsFolderCommand { get; set; }
        public AsyncRelayCommand ViewCurrentLogCommand { get; set; }
        public AsyncRelayCommand SyncNowCommand { get; set; }

        /// <summary>
        /// Gets whether the current logged-in user has Admin role in the application.
        /// This is checked against application roles, not Windows administrator privileges.
        /// </summary>
        public bool IsUserAdministrator => CurrentUserService.Instance.IsAdmin;

        /// <summary>
        /// Gets whether a user is currently logged in.
        /// </summary>
        public bool IsUserLoggedIn => CurrentUserService.Instance.IsLoggedIn;

        private int _pendingSyncCount;
        /// <summary>
        /// Gets the number of users pending sync to Firebase.
        /// </summary>
        public int PendingSyncCount
        {
            get => _pendingSyncCount;
            set => SetProperty(ref _pendingSyncCount, value);
        }

        private string _syncStatusMessage;
        /// <summary>
        /// Gets the current sync status message.
        /// </summary>
        public string SyncStatusMessage
        {
            get => _syncStatusMessage;
            set => SetProperty(ref _syncStatusMessage, value);
        }

        private bool _isSyncing;
        /// <summary>
        /// Gets whether a sync operation is currently in progress.
        /// </summary>
        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        /// <summary>
        /// Gets whether the system is currently online.
        /// </summary>
        public bool IsOnline => _connectivityService?.IsOnline ?? false;

        /// <summary>
        /// Gets the connection status text for display.
        /// </summary>
        public string ConnectionStatusText => IsOnline ? "Online" : "Offline";

        /// <summary>
        /// Gets the text for the Login/Logout button.
        /// </summary>
        public string LoginLogoutButtonText => IsUserLoggedIn ? "Logout" : "Login";

        public MainWindowViewModel(INavigationService navigationService, LoginViewModel loginViewModel,
            HybridUserDal hybridUserDal, SyncService syncService, NetworkConnectivityService connectivityService)
        {
            Navigation = navigationService;
            _loginViewModel = loginViewModel;
            _hybridUserDal = hybridUserDal;
            _syncService = syncService;
            _connectivityService = connectivityService;
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

            NavigateToLoginCommand = new RelayCommand(o =>
            {
                _loginViewModel.ClearForm();
                Navigation.NavigateTo<LoginViewModel>();
            }, o => true);

            LogoutCommand = new RelayCommand(o => Logout());

            // Combined Login/Logout command that switches behavior based on login state
            LoginLogoutCommand = new RelayCommand(o =>
            {
                if (IsUserLoggedIn)
                {
                    Logout();
                }
                else
                {
                    _loginViewModel.ClearForm();
                    Navigation.NavigateTo<LoginViewModel>();
                }
            });

            // Admin-only commands for log access
            OpenLogsFolderCommand = new AsyncRelayCommand(OpenLogsFolderAsync, () => IsUserAdministrator);
            ViewCurrentLogCommand = new AsyncRelayCommand(ViewCurrentLogAsync, () => IsUserAdministrator);
            SyncNowCommand = new AsyncRelayCommand(SyncNowAsync, () => IsUserAdministrator && !IsSyncing);

            // Subscribe to sync events
            _syncService.SyncStarted += OnSyncStarted;
            _syncService.SyncCompleted += OnSyncCompleted;
            _syncService.SyncProgress += OnSyncProgress;

            // Subscribe to user changes to update admin-only features visibility
            CurrentUserService.Instance.OnUserInfoChanged += (sender, userInfo) =>
            {
                // Notify UI that IsUserAdministrator and IsUserLoggedIn may have changed
                OnPropertyChanged(nameof(IsUserAdministrator));
                OnPropertyChanged(nameof(IsUserLoggedIn));
                OnPropertyChanged(nameof(LoginLogoutButtonText));

                // Update CanExecute for admin commands
                OpenLogsFolderCommand?.RaiseCanExecuteChanged();
                ViewCurrentLogCommand?.RaiseCanExecuteChanged();
                SyncNowCommand?.RaiseCanExecuteChanged();

                // Refresh sync status when user logs in/out
                RefreshSyncStatus();
            };

            // Subscribe to connectivity changes to update UI
            _connectivityService.ConnectivityChanged += (sender, isOnline) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(IsOnline));
                    OnPropertyChanged(nameof(ConnectionStatusText));
                });
            };

            // Initialize sync status
            RefreshSyncStatus();
        }

        /// <summary>
        /// Logs out the current user and navigates to the login screen.
        /// </summary>
        private void Logout()
        {
            var currentUser = CurrentUserService.Instance.CurrentUser;
            _logger.Information("User logging out: {Email}", currentUser?.Email ?? "Unknown");

            // Sign out from Firebase if online
            _hybridUserDal.SignOut();

            // Clear current user
            CurrentUserService.Instance.ClearCurrentUser();

            _logger.Information("User logged out successfully");

            // Clear login form and navigate to login screen
            _loginViewModel.ClearForm();
            Navigation.NavigateTo<LoginViewModel>();
        }

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

        /// <summary>
        /// Manually triggers a sync operation (admin only).
        /// </summary>
        private async Task SyncNowAsync()
        {
            var window = (MetroWindow)Application.Current.MainWindow;

            try
            {
                SyncStatusMessage = "Starting sync...";
                var result = await _syncService.SyncAsync();

                if (result.Success)
                {
                    await window.ShowMessageAsync("Sync Complete",
                        $"Sync completed successfully!\n\nUsers uploaded: {result.UsersUploaded}\nDuration: {result.Duration.TotalSeconds:F1}s",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK",
                            AnimateShow = true,
                            AnimateHide = true
                        });
                }
                else
                {
                    await window.ShowMessageAsync("Sync Failed",
                        $"Sync failed: {result.ErrorMessage}",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings
                        {
                            AffirmativeButtonText = "OK",
                            AnimateShow = true,
                            AnimateHide = true
                        });
                }

                RefreshSyncStatus();
            }
            catch (Exception ex)
            {
                await window.ShowMessageAsync("Error",
                    $"Sync error: {ex.Message}",
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
        /// Refreshes the sync status display.
        /// </summary>
        private void RefreshSyncStatus()
        {
            try
            {
                PendingSyncCount = _hybridUserDal.GetPendingSyncCount();

                if (PendingSyncCount > 0)
                {
                    SyncStatusMessage = $"{PendingSyncCount} user(s) pending sync";
                }
                else
                {
                    SyncStatusMessage = "All synced";
                }
            }
            catch (Exception ex)
            {
                SyncStatusMessage = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Event handler for sync started.
        /// </summary>
        private void OnSyncStarted(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsSyncing = true;
                SyncStatusMessage = "Syncing...";
                SyncNowCommand?.RaiseCanExecuteChanged();
            });
        }

        /// <summary>
        /// Event handler for sync completed.
        /// </summary>
        private void OnSyncCompleted(object sender, SyncCompletedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsSyncing = false;
                SyncNowCommand?.RaiseCanExecuteChanged();
                RefreshSyncStatus();
            });
        }

        /// <summary>
        /// Event handler for sync progress updates.
        /// </summary>
        private void OnSyncProgress(object sender, SyncProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SyncStatusMessage = e.Message;
            });
        }

        #endregion
    }
}
