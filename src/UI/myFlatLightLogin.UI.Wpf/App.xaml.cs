using Microsoft.Extensions.DependencyInjection;
using myFlatLightLogin.UI.Common.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.UI.Common.Services;
using myFlatLightLogin.DalFirebase;
using myFlatLightLogin.UI.Wpf.MVVM.View;
using myFlatLightLogin.UI.Wpf.MVVM.ViewModel;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FlatLightLogin
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;
        private NetworkConnectivityService _connectivityService;
        private SyncService _syncService;
        private HybridUserDal _hybridUserDal;

        /// <summary>
        /// Public access to the service provider for manual dependency resolution.
        /// </summary>
        public ServiceProvider ServiceProvider => _serviceProvider;

        public App()
        {
            // Configure Serilog
            ConfigureSerilog();

            IServiceCollection services = new ServiceCollection();

            // Register core services as singletons
            services.AddSingleton<NetworkConnectivityService>();
            services.AddSingleton<SyncService>();
            services.AddSingleton<HybridUserDal>();

            services.AddSingleton(provider => new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            });

            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<LoginViewModel>();
            services.AddSingleton<RegisterUserViewModel>();
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<RoleManagementViewModel>();
            services.AddSingleton<ChangePasswordViewModel>();

            services.AddSingleton<IDialogService, MahAppsDialogService>();
            services.AddSingleton<INavigationService, NavigationService>();

            services.AddSingleton<Func<Type, ViewModelBase>>(serviceProvider =>
                viewModelType => (ViewModelBase)serviceProvider.GetRequiredService(viewModelType));

            _serviceProvider = services.BuildServiceProvider();

            // Get service instances
            _connectivityService = _serviceProvider.GetRequiredService<NetworkConnectivityService>();
            _syncService = _serviceProvider.GetRequiredService<SyncService>();
            _hybridUserDal = _serviceProvider.GetRequiredService<HybridUserDal>();

            // Subscribe to connectivity changes for automatic sync
            _connectivityService.ConnectivityChanged += OnConnectivityChanged;
        }

        private void ConfigureSerilog()
        {
            // Create logs directory if it doesn't exist
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logsPath);

            // Configure Serilog
            // SECURITY NOTE: Exception details can contain sensitive data (passwords, tokens, etc.)
            // Always log ex.Message only, never pass full exception objects to logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logsPath, "myFlatLightLogin-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 30)
                .CreateLogger();

            Log.Information("========================================");
            Log.Information("Application Starting");
            Log.Information("========================================");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Information("OnStartup called");

            // Initialize Firebase Roles asynchronously (seeds default roles if they don't exist)
            // This runs in the background to avoid blocking the UI thread
            _ = Task.Run(async () =>
            {
                try
                {
                    Log.Information("Initializing Firebase Roles...");
                    var roleDal = new RoleDal();
                    await roleDal.InitializeAsync();
                    Log.Information("Firebase Roles initialized successfully");
                }
                catch (Exception ex)
                {
                    // Log error but don't prevent app startup
                    // Role initialization may fail if Firebase is not configured
                    Log.Warning($"Failed to initialize Firebase Roles: {ex.Message}");
                }
            });

            // Perform automatic sync on startup if online
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_connectivityService.IsOnline)
                    {
                        Log.Information("App startup - checking for pending sync...");
                        var pendingCount = _hybridUserDal.GetPendingSyncCount();

                        if (pendingCount > 0)
                        {
                            Log.Information("Found {PendingCount} users pending sync, starting sync...", pendingCount);
                            var syncResult = await _syncService.SyncAsync();

                            if (syncResult.Success)
                            {
                                Log.Information("Startup sync completed successfully. Uploaded: {UsersUploaded}", syncResult.UsersUploaded);
                            }
                            else
                            {
                                Log.Warning("Startup sync failed: {ErrorMessage}", syncResult.ErrorMessage);
                            }
                        }
                        else
                        {
                            Log.Information("No users pending sync on startup");
                        }
                    }
                    else
                    {
                        Log.Information("App started offline - sync will occur when connection is restored");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error during startup sync");
                }
            });

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("========================================");
            Log.Information("Application Shutting Down");
            Log.Information("========================================");

            // Cleanup connectivity service
            _connectivityService?.Dispose();

            // Ensure all logs are flushed before exit
            Log.CloseAndFlush();

            base.OnExit(e);
        }

        /// <summary>
        /// Handles connectivity changes and automatically triggers sync when connection is restored.
        /// </summary>
        private async void OnConnectivityChanged(object? sender, bool isOnline)
        {
            Log.Information("Connectivity changed: IsOnline = {IsOnline}", isOnline);

            if (isOnline)
            {
                // Connection restored - check for pending sync
                try
                {
                    var pendingCount = _hybridUserDal.GetPendingSyncCount();

                    if (pendingCount > 0)
                    {
                        Log.Information("Connection restored - found {PendingCount} users pending sync, starting automatic sync...", pendingCount);

                        var syncResult = await _syncService.SyncAsync();

                        if (syncResult.Success)
                        {
                            Log.Information("Automatic sync completed successfully. Uploaded: {UsersUploaded}", syncResult.UsersUploaded);
                        }
                        else
                        {
                            Log.Warning("Automatic sync failed: {ErrorMessage}", syncResult.ErrorMessage);
                        }
                    }
                    else
                    {
                        Log.Information("Connection restored - no users pending sync");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error during automatic sync on connectivity restore");
                }
            }
        }
    }
}
