using Microsoft.Extensions.DependencyInjection;
using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
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

        public App()
        {
            // Configure Serilog
            ConfigureSerilog();

            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(provider => new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            });

            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<LoginViewModel>();
            services.AddSingleton<RegisterUserViewModel>();
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<RoleManagementViewModel>();

            services.AddSingleton<INavigationService, NavigationService>();

            services.AddSingleton<Func<Type, ViewModelBase>>(serviceProvider =>
                viewModelType => (ViewModelBase)serviceProvider.GetRequiredService(viewModelType));

            _serviceProvider = services.BuildServiceProvider();
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

                    // OPTIONAL: Uncomment to migrate existing Firebase users to add Role field
                    // This only needs to be run once for existing users created before Role system
                    // After running once, you can comment it out again
                    /*
                    Log.Information("Running Firebase user migration...");
                    var migrationUtility = new FirebaseMigrationUtility();
                    int usersUpdated = await migrationUtility.MigrateUsersWithRoleFieldAsync();
                    Log.Information($"Migration complete. {usersUpdated} users updated with Role field.");
                    */
                }
                catch (Exception ex)
                {
                    // Log error but don't prevent app startup
                    // Role initialization may fail if Firebase is not configured
                    Log.Warning($"Failed to initialize Firebase Roles: {ex.Message}");
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

            // Ensure all logs are flushed before exit
            Log.CloseAndFlush();

            base.OnExit(e);
        }
    }
}
